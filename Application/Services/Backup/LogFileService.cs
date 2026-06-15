using Microsoft.AspNetCore.Hosting;
using VisitorManagementSystem.Api.Application.DTOs.Backup;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Infrastructure.Configuration;

namespace VisitorManagementSystem.Api.Application.Services.Backup;

/// <summary>
/// Manages the backend log files written by Serilog to the logs/ folder.
/// Provides health metrics (total size, file count) and purges files older than the retention period.
/// </summary>
public class LogFileService : ILogFileService
{
    private static readonly string[] LogExtensions = [".txt", ".log"];

    private readonly IWebHostEnvironment _env;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LogFileService> _logger;

    public LogFileService(
        IWebHostEnvironment env,
        IUnitOfWork unitOfWork,
        ILogger<LogFileService> logger)
    {
        _env = env;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public string GetLogFolderPath()
        => VmsRuntimePaths.GetLogsRoot(_env);

    // ─────────────────────────────────────────────────────────────────────
    // Health
    // ─────────────────────────────────────────────────────────────────────

    public Task<LogFolderHealthDto> GetLogFolderHealthAsync(CancellationToken ct = default)
    {
        var folderPath = GetLogFolderPath();

        if (!Directory.Exists(folderPath))
        {
            return Task.FromResult(new LogFolderHealthDto(
                folderPath, false, 0, "0 B", 0, null, null,
                new List<LogFileInfoDto>(), StorageAlertLevel.None));
        }

        var fileInfos = Directory.GetFiles(folderPath)
            .Where(f => LogExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .ToList();

        var totalBytes = fileInfos.Sum(f => f.Length);
        var oldestFile = fileInfos.Count > 0 ? fileInfos.Min(f => f.LastWriteTimeUtc) : (DateTime?)null;
        var newestFile = fileInfos.Count > 0 ? fileInfos.Max(f => f.LastWriteTimeUtc) : (DateTime?)null;

        var logFiles = fileInfos
            .Select(f => new LogFileInfoDto(f.Name, f.Length, FormatBytes(f.Length), f.LastWriteTimeUtc))
            .ToList();

        var totalMb = totalBytes / (1024.0 * 1024.0);
        var alertLevel = totalMb >= 1000 ? StorageAlertLevel.Critical
                       : totalMb >= 500  ? StorageAlertLevel.Warning
                       : StorageAlertLevel.None;

        return Task.FromResult(new LogFolderHealthDto(
            folderPath, true, totalBytes, FormatBytes(totalBytes),
            fileInfos.Count, oldestFile, newestFile, logFiles, alertLevel));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Purge
    // ─────────────────────────────────────────────────────────────────────

    public Task<LogPurgeResultDto> PurgeOldLogsAsync(int retentionDays, CancellationToken ct = default)
    {
        var folderPath = GetLogFolderPath();

        if (!Directory.Exists(folderPath))
            return Task.FromResult(new LogPurgeResultDto(true, 0, 0, "0 B", null));

        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

        var candidates = Directory.GetFiles(folderPath)
            .Where(f => LogExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Select(f => new FileInfo(f))
            .Where(f => f.LastWriteTimeUtc < cutoff)
            .ToList();

        int deleted = 0;
        long freed = 0;

        foreach (var file in candidates)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                freed += file.Length;
                file.Delete();
                deleted++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete log file: {File}", file.Name);
            }
        }

        _logger.LogInformation("Log purge: deleted {Count} files, freed {Size}", deleted, FormatBytes(freed));
        return Task.FromResult(new LogPurgeResultDto(true, deleted, freed, FormatBytes(freed), null));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Settings
    // ─────────────────────────────────────────────────────────────────────

    public async Task<LogSettingsDto> GetLogSettingsAsync(CancellationToken ct = default)
    {
        async Task<string> S(string key, string def) =>
            (await _unitOfWork.SystemConfigurations.GetByCategoryAndKeyAsync("Logs", key, ct))?.Value ?? def;
        async Task<bool> B(string key, bool def) =>
            bool.TryParse(await S(key, def.ToString()), out var v) ? v : def;
        async Task<int> I(string key, int def) =>
            int.TryParse(await S(key, def.ToString()), out var v) ? v : def;

        var nextRunRaw = await S("NextRunAt", "");
        DateTime? nextRun = DateTime.TryParse(nextRunRaw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var nr) ? nr : null;

        return new LogSettingsDto(
            RetentionDays:    await I("RetentionDays", 30),
            AutoPurgeEnabled: await B("AutoPurgeEnabled", false),
            ScheduleEnabled:  await B("ScheduleEnabled", false),
            ScheduleTime:     await S("ScheduleTime", "04:00"),
            NextRunAt:        nextRun,
            MaxFolderSizeMb:  await I("MaxFolderSizeMb", 500)
        );
    }

    public async Task<(bool Success, string? Error)> UpdateLogSettingsAsync(
        UpdateLogSettingsRequest request, CancellationToken ct = default)
    {
        if (request.RetentionDays < 1 || request.RetentionDays > 365)
            return (false, "RetentionDays must be between 1 and 365.");

        if (!TimeSpan.TryParse(request.ScheduleTime, out _))
            return (false, "ScheduleTime must be in HH:mm format.");

        if (request.MaxFolderSizeMb < 50 || request.MaxFolderSizeMb > 10240)
            return (false, "MaxFolderSizeMb must be between 50 and 10240.");

        await Set("RetentionDays",    request.RetentionDays.ToString(), ct);
        await Set("AutoPurgeEnabled", request.AutoPurgeEnabled.ToString(), ct);
        await Set("ScheduleEnabled",  request.ScheduleEnabled.ToString(), ct);
        await Set("ScheduleTime",     request.ScheduleTime, ct);
        await Set("MaxFolderSizeMb",  request.MaxFolderSizeMb.ToString(), ct);

        if (request.ScheduleEnabled)
        {
            var next = DataPurgeService.CalculateNextRunAt(request.ScheduleTime, "Daily");
            await Set("NextRunAt", next.ToString("O"), ct);
        }
        else
        {
            await Set("NextRunAt", string.Empty, ct);
        }

        return (true, null);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────

    private async Task Set(string key, string value, CancellationToken ct)
    {
        var config = await _unitOfWork.SystemConfigurations.GetByCategoryAndKeyAsync("Logs", key, ct);
        if (config == null)
        {
            var dataType = bool.TryParse(value, out _) ? "boolean"
                         : int.TryParse(value, out _) ? "integer"
                         : "string";
            await _unitOfWork.Repository<SystemConfiguration>().AddAsync(new SystemConfiguration
            {
                Category = "Logs", Key = key, Value = value, DataType = dataType, Environment = "All"
            }, ct);
        }
        else
        {
            config.Value = value;
            config.UpdateModifiedOn();
            _unitOfWork.SystemConfigurations.Update(config);
        }
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}
