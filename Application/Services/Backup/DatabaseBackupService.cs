using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Backup;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Application.Services.Backup;

public class DatabaseBackupService : IDatabaseBackupService
{
    // One backup at a time, application-wide.
    private static readonly SemaphoreSlim _lock = new(1, 1);
    private static volatile bool _isRunning;

    private const int BackupTimeoutSeconds = 1800; // 30 minutes max
    private const double ExpressLimitMb = 10240.0;

    // Forbidden destination prefixes (Windows system directories)
    private static readonly string[] ForbiddenRoots =
    [
        @"C:\Windows",
        @"C:\Program Files",
        @"C:\Program Files (x86)",
        Environment.GetFolderPath(Environment.SpecialFolder.System),
        Environment.GetFolderPath(Environment.SpecialFolder.Windows),
    ];

    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDiskStorageMonitor _diskMonitor;
    private readonly ILogger<DatabaseBackupService> _logger;

    public DatabaseBackupService(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IDiskStorageMonitor diskMonitor,
        ILogger<DatabaseBackupService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _diskMonitor = diskMonitor;
        _logger = logger;
    }

    public bool IsBackupRunning => _isRunning;

    // ─────────────────────────────────────────────────────────────────────
    // Core backup execution
    // ─────────────────────────────────────────────────────────────────────

    public async Task<BackupResult> ExecuteBackupAsync(
        string triggerType,
        int? triggeredByUserId,
        string triggeredByDisplay,
        CancellationToken cancellationToken = default)
    {
        // Reject if already running
        if (!await _lock.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
            return new BackupResult(false, null, null, 0, "A backup is already in progress. Try again shortly.");

        _isRunning = true;
        BackupRecord? record = null;

        try
        {
            // --- Read settings ---
            var settings = await GetSettingsAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(settings.DestinationPath))
                return Fail("Backup destination path is not configured.");

            // --- Validate destination path ---
            var (pathOk, pathErr) = ValidatePath(settings.DestinationPath);
            if (!pathOk) return Fail(pathErr!);

            var (writable, writeErr) = await _diskMonitor.TestPathWritableAsync(settings.DestinationPath, cancellationToken);
            if (!writable) return Fail($"Backup destination is not writable: {writeErr}");

            // --- Pre-flight disk space check ---
            var dataFileSizeMb = await GetDataFileSizeMbAsync(cancellationToken);
            var estimatedBytes = _diskMonitor.EstimateBackupSizeBytes(dataFileSizeMb);
            var destinationRoot = Path.GetPathRoot(settings.DestinationPath)!;

            try
            {
                var drive = new DriveInfo(destinationRoot);
                if (drive.AvailableFreeSpace < estimatedBytes)
                {
                    return Fail($"Insufficient disk space on {destinationRoot}. " +
                        $"Need ~{estimatedBytes / (1024 * 1024)} MB, have {drive.AvailableFreeSpace / (1024 * 1024)} MB free.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not check disk space for {Root} — proceeding with backup", destinationRoot);
            }

            // --- Build file name and path ---
            var dbName = GetDatabaseName();
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var trigger = triggerType.ToLowerInvariant();
            var fileName = $"{dbName}_Full_{timestamp}_{trigger}.bak";
            var filePath = Path.Combine(settings.DestinationPath, fileName);

            // --- Create BackupRecord (Running) ---
            record = BackupRecord.Start(triggerType, triggeredByUserId, triggeredByDisplay, dbName, dataFileSizeMb);
            await _unitOfWork.Repository<BackupRecord>().AddAsync(record, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Starting {TriggerType} backup: {FileName}", triggerType, fileName);

            // --- Execute BACKUP DATABASE ---
            // COMPRESSION is not supported on SQL Server Express; CHECKSUM and STATS are fine on all editions.
            var backupSql = $@"BACKUP DATABASE [{dbName}] TO DISK = N'{EscapeSqlPath(filePath)}' WITH CHECKSUM, STATS = 10, NAME = N'{dbName} {triggerType} Backup {timestamp}'";

            using var backupCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            backupCts.CancelAfter(TimeSpan.FromSeconds(BackupTimeoutSeconds));

            _context.Database.SetCommandTimeout(BackupTimeoutSeconds);
            await _context.Database.ExecuteSqlRawAsync(backupSql, backupCts.Token);

            // --- Record success ---
            var fileInfo = new FileInfo(filePath);
            record.Complete(filePath, fileName, fileInfo.Exists ? fileInfo.Length : 0);
            _unitOfWork.Repository<BackupRecord>().Update(record);

            // Update LastSuccessAt in config
            await UpdateConfigAsync("Backup", "LastSuccessAt", DateTime.UtcNow.ToString("O"), cancellationToken);
            await UpdateConfigAsync("Backup", "LastFailureMessage", string.Empty, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Backup completed successfully: {FileName} ({SizeMb:F1} MB) in {Seconds}s",
                fileName, fileInfo.Length / (1024.0 * 1024), record.DurationSeconds);

            // --- Retention cleanup ---
            await RunRetentionCleanupAsync(settings.DestinationPath, settings.RetentionDays, dbName, cancellationToken);

            return new BackupResult(true, filePath, fileName, fileInfo.Length, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var msg = "Backup was cancelled.";
            if (record != null) { record.Fail(msg); _unitOfWork.Repository<BackupRecord>().Update(record); await _unitOfWork.SaveChangesAsync(CancellationToken.None); }
            return new BackupResult(false, null, null, 0, msg);
        }
        catch (Exception ex)
        {
            var msg = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            _logger.LogError(ex, "Backup failed ({TriggerType})", triggerType);

            if (record != null) { record.Fail(msg); _unitOfWork.Repository<BackupRecord>().Update(record); await _unitOfWork.SaveChangesAsync(CancellationToken.None); }

            await UpdateConfigAsync("Backup", "LastFailureMessage", msg, CancellationToken.None);
            try { await _unitOfWork.SaveChangesAsync(CancellationToken.None); } catch { /* best effort */ }

            return new BackupResult(false, null, null, 0, msg);
        }
        finally
        {
            _isRunning = false;
            _lock.Release();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // History queries
    // ─────────────────────────────────────────────────────────────────────

    public async Task<List<BackupRecordDto>> GetHistoryAsync(int count = 20, CancellationToken cancellationToken = default)
    {
        var records = await _context.BackupRecords
            .OrderByDescending(r => r.StartedAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return records.Select(MapToDto).ToList();
    }

    public async Task<BackupRecordDto?> GetLastSuccessAsync(CancellationToken cancellationToken = default)
    {
        var record = await _context.BackupRecords
            .Where(r => r.Status == BackupStatus.Completed)
            .OrderByDescending(r => r.CompletedAt)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return record == null ? null : MapToDto(record);
    }

    public async Task<BackupRecordDto?> GetLastFailureAsync(CancellationToken cancellationToken = default)
    {
        var record = await _context.BackupRecords
            .Where(r => r.Status == BackupStatus.Failed)
            .OrderByDescending(r => r.StartedAt)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return record == null ? null : MapToDto(record);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Settings
    // ─────────────────────────────────────────────────────────────────────

    public async Task<BackupSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        async Task<string> S(string cat, string key, string def) =>
            (await _unitOfWork.SystemConfigurations.GetByCategoryAndKeyAsync(cat, key, cancellationToken))?.Value ?? def;

        async Task<bool> B(string cat, string key, bool def) =>
            bool.TryParse(await S(cat, key, def.ToString()), out var v) ? v : def;

        async Task<int> I(string cat, string key, int def) =>
            int.TryParse(await S(cat, key, def.ToString()), out var v) ? v : def;

        return new BackupSettingsDto(
            Enabled: await B("Backup", "Enabled", false),
            ScheduleTime: await S("Backup", "ScheduleTime", "02:00"),
            DestinationPath: await S("Backup", "DestinationPath", @"C:\VMS_Backups"),
            RetentionDays: await I("Backup", "RetentionDays", 14),
            AutoBackupOnAlert: await B("Backup", "AutoBackupOnAlert", false),
            AutoBackupOnCritical: await B("Backup", "AutoBackupOnCritical", true),
            AlertEnabled: await B("Storage", "AlertEnabled", true),
            DbWarnThresholdPercent: await I("Storage", "DbWarnThresholdPercent", 70),
            DbAlertThresholdPercent: await I("Storage", "DbAlertThresholdPercent", 85),
            DbCriticalThresholdPercent: await I("Storage", "DbCriticalThresholdPercent", 95),
            DiskWarnFreePercent: await I("Storage", "DiskWarnFreePercent", 20),
            DiskCriticalFreePercent: await I("Storage", "DiskCriticalFreePercent", 10)
        );
    }

    public async Task<(bool Success, string? Error)> UpdateSettingsAsync(
        UpdateBackupSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate destination path before saving
        var (pathOk, pathErr) = ValidatePath(request.DestinationPath);
        if (!pathOk) return (false, pathErr);

        if (!TimeSpan.TryParse(request.ScheduleTime, out _))
            return (false, "ScheduleTime must be in HH:mm format.");

        if (request.RetentionDays < 1 || request.RetentionDays > 365)
            return (false, "RetentionDays must be between 1 and 365.");

        await SetConfigAsync("Backup", "Enabled", request.Enabled.ToString(), cancellationToken);
        await SetConfigAsync("Backup", "ScheduleTime", request.ScheduleTime, cancellationToken);
        await SetConfigAsync("Backup", "DestinationPath", request.DestinationPath, cancellationToken);
        await SetConfigAsync("Backup", "RetentionDays", request.RetentionDays.ToString(), cancellationToken);
        await SetConfigAsync("Backup", "AutoBackupOnAlert", request.AutoBackupOnAlert.ToString(), cancellationToken);
        await SetConfigAsync("Backup", "AutoBackupOnCritical", request.AutoBackupOnCritical.ToString(), cancellationToken);
        await SetConfigAsync("Storage", "AlertEnabled", request.AlertEnabled.ToString(), cancellationToken);
        await SetConfigAsync("Storage", "DbWarnThresholdPercent", request.DbWarnThresholdPercent.ToString(), cancellationToken);
        await SetConfigAsync("Storage", "DbAlertThresholdPercent", request.DbAlertThresholdPercent.ToString(), cancellationToken);
        await SetConfigAsync("Storage", "DbCriticalThresholdPercent", request.DbCriticalThresholdPercent.ToString(), cancellationToken);
        await SetConfigAsync("Storage", "DiskWarnFreePercent", request.DiskWarnFreePercent.ToString(), cancellationToken);
        await SetConfigAsync("Storage", "DiskCriticalFreePercent", request.DiskCriticalFreePercent.ToString(), cancellationToken);

        // Recalculate NextRunAt if schedule or enabled state changed
        if (request.Enabled)
        {
            var nextRun = CalculateNextRunAt(request.ScheduleTime);
            await SetConfigAsync("Backup", "NextRunAt", nextRun.ToString("O"), cancellationToken);
        }
        else
        {
            await SetConfigAsync("Backup", "NextRunAt", string.Empty, cancellationToken);
        }

        return (true, null);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Retention cleanup
    // ─────────────────────────────────────────────────────────────────────

    private async Task RunRetentionCleanupAsync(string destinationPath, int retentionDays, string dbName, CancellationToken cancellationToken)
    {
        try
        {
            if (!Directory.Exists(destinationPath)) return;

            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            var pattern = $"{dbName}_Full_*.bak";
            var files = Directory.GetFiles(destinationPath, pattern);

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var fi = new FileInfo(file);
                    if (fi.CreationTimeUtc < cutoff)
                    {
                        fi.Delete();
                        _logger.LogInformation("Retention cleanup: deleted old backup {File}", fi.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete backup file during retention cleanup: {File}", file);
                }
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Retention cleanup failed for {Path}", destinationPath);
        }

        await Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────

    private string GetDatabaseName()
    {
        var connStr = _unitOfWork.GetConnectionString();
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connStr);
        return string.IsNullOrWhiteSpace(builder.InitialCatalog) ? "VMS" : builder.InitialCatalog;
    }

    private async Task<double> GetDataFileSizeMbAsync(CancellationToken ct)
    {
        try
        {
            var connection = _context.Database.GetDbConnection();
            bool opened = connection.State != System.Data.ConnectionState.Open;
            if (opened) await connection.OpenAsync(ct);
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT CAST(SUM(size * 8.0 / 1024.0) AS float) FROM sys.database_files WHERE type = 0;";
                var result = await cmd.ExecuteScalarAsync(ct);
                return result == DBNull.Value || result == null ? 0 : Convert.ToDouble(result);
            }
            finally { if (opened) await connection.CloseAsync(); }
        }
        catch { return 0; }
    }

    private static (bool Ok, string? Error) ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return (false, "Backup destination path cannot be empty.");

        if (!Path.IsPathRooted(path))
            return (false, "Backup destination must be an absolute path (e.g. C:\\VMS_Backups).");

        if (path.Contains(".."))
            return (false, "Backup destination path must not contain '..' path traversal sequences.");

        var normalized = path.TrimEnd('\\', '/');
        foreach (var forbidden in ForbiddenRoots)
        {
            if (!string.IsNullOrEmpty(forbidden) &&
                normalized.StartsWith(forbidden.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase))
                return (false, $"Backup destination cannot be inside a system directory ({forbidden}).");
        }

        return (true, null);
    }

    private static string EscapeSqlPath(string path)
        => path.Replace("'", "''");

    private static DateTime CalculateNextRunAt(string scheduleTime)
    {
        if (!TimeSpan.TryParse(scheduleTime, out var time))
            time = TimeSpan.FromHours(2);

        var candidate = DateTime.UtcNow.Date.Add(time);
        if (candidate <= DateTime.UtcNow)
            candidate = candidate.AddDays(1);
        return candidate;
    }

    private async Task UpdateConfigAsync(string category, string key, string value, CancellationToken ct)
    {
        var config = await _unitOfWork.SystemConfigurations.GetByCategoryAndKeyAsync(category, key, ct);
        if (config == null)
        {
            // Key has never been seeded — create it on first use
            var dataType = bool.TryParse(value, out _) ? "boolean"
                         : int.TryParse(value, out _) ? "integer"
                         : "string";
            var newConfig = new SystemConfiguration
            {
                Category = category,
                Key = key,
                Value = value,
                DataType = dataType,
                Environment = "All"
            };
            await _unitOfWork.Repository<SystemConfiguration>().AddAsync(newConfig, ct);
        }
        else
        {
            config.Value = value;
            config.UpdateModifiedOn();
            _unitOfWork.SystemConfigurations.Update(config);
        }
    }

    private async Task SetConfigAsync(string category, string key, string value, CancellationToken ct)
    {
        await UpdateConfigAsync(category, key, value, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static BackupRecordDto MapToDto(BackupRecord r) => new(
        Id: r.Id,
        TriggerType: r.TriggerType,
        TriggeredByDisplay: r.TriggeredByDisplay,
        StartedAt: r.StartedAt,
        CompletedAt: r.CompletedAt,
        Status: r.Status,
        FileName: r.FileName,
        FilePath: r.FilePath,
        FileSizeDisplay: r.FileSizeDisplay,
        DurationDisplay: r.DurationDisplay,
        ErrorMessage: r.ErrorMessage
    );

    private static BackupResult Fail(string error) => new(false, null, null, 0, error);
}
