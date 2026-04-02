using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VisitorManagementSystem.Api.Application.DTOs.Backup;
using VisitorManagementSystem.Api.Application.Services.Backup;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Controllers.Admin;

/// <summary>
/// Backup and storage management endpoints.
/// All routes require Administrator role + explicit backup permission.
/// </summary>
[ApiController]
[Route("api/system/backup")]
[Authorize(Roles = "Administrator")]
public class BackupController : ControllerBase
{
    private readonly IDatabaseBackupService _backupService;
    private readonly IDatabaseStorageMonitor _dbMonitor;
    private readonly IDiskStorageMonitor _diskMonitor;
    private readonly IDataPurgeService _purgeService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackupController> _logger;

    public BackupController(
        IDatabaseBackupService backupService,
        IDatabaseStorageMonitor dbMonitor,
        IDiskStorageMonitor diskMonitor,
        IDataPurgeService purgeService,
        IServiceScopeFactory scopeFactory,
        ILogger<BackupController> logger)
    {
        _backupService = backupService;
        _dbMonitor = dbMonitor;
        _diskMonitor = diskMonitor;
        _purgeService = purgeService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/system/backup/health
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the full backup health dashboard payload:
    /// DB storage metrics, drive health, job state, last backup, settings.
    /// Polled by the frontend every 60 seconds (10 seconds when a job is running).
    /// </summary>
    [HttpGet("health")]
    [Authorize(Policy = Permissions.Backup.View)]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _backupService.GetSettingsAsync(cancellationToken);

            // DB metrics
            DatabaseStorageDto dbMetrics;
            try { dbMetrics = await _dbMonitor.GetStorageMetricsAsync(cancellationToken); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DB storage query failed in health endpoint");
                dbMetrics = new DatabaseStorageDto
                {
                    SqlEdition = "Unknown (query failed)",
                    AlertLevel = StorageAlertLevel.None
                };
            }

            // Disk metrics
            List<DriveHealthDto> drives;
            try
            {
                var dbPaths = dbMetrics.Files.Select(f => f.PhysicalPath).Where(p => !string.IsNullOrEmpty(p));
                drives = await _diskMonitor.GetDriveHealthAsync(
                    dbPaths, settings.DestinationPath,
                    settings.DiskWarnFreePercent, settings.DiskCriticalFreePercent,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Disk health query failed in health endpoint");
                drives = new List<DriveHealthDto>();
            }

            var lastSuccess = await _backupService.GetLastSuccessAsync(cancellationToken);
            var lastFailure = await _backupService.GetLastFailureAsync(cancellationToken);

            // Parse NextRunAt
            DateTime? nextRun = null;
            if (!string.IsNullOrEmpty(settings.DestinationPath)) // re-read raw NextRunAt below
            {
                // We embed it in settings indirectly — read it fresh from backup service
                // (GetSettingsAsync doesn't include it, so we keep it simple here)
            }

            // Active warnings
            var warnings = new List<string>();
            if (dbMetrics.AlertLevel == StorageAlertLevel.Critical)
                warnings.Add($"Database data file is at {dbMetrics.UsagePercent:F1}% of the 10 GB Express limit — imminent write failures.");
            else if (dbMetrics.AlertLevel == StorageAlertLevel.High)
                warnings.Add($"Database data file is at {dbMetrics.UsagePercent:F1}% of the 10 GB Express limit — action required.");
            else if (dbMetrics.AlertLevel == StorageAlertLevel.Warning)
                warnings.Add($"Database data file is at {dbMetrics.UsagePercent:F1}% of the 10 GB Express limit.");

            foreach (var drive in drives.Where(d => d.AlertLevel != StorageAlertLevel.None))
                warnings.Add($"Drive {drive.DriveLetter} has {drive.FreePercent:F1}% free space ({drive.AlertLevel}).");

            if (lastSuccess == null)
                warnings.Add("No successful backup on record. Configure and run a backup immediately.");
            else if (lastSuccess.CompletedAt.HasValue && (DateTime.UtcNow - lastSuccess.CompletedAt.Value).TotalHours > 48)
                warnings.Add($"Last successful backup was {(int)(DateTime.UtcNow - lastSuccess.CompletedAt.Value).TotalHours} hours ago.");

            string overallStatus = warnings.Any(w => w.Contains("imminent") || w.Contains("Critical") || w.Contains("CRITICAL"))
                ? OverallHealthStatus.Critical
                : warnings.Any() ? OverallHealthStatus.Warning
                : OverallHealthStatus.Healthy;

            var health = new BackupHealthDto
            {
                Database = dbMetrics,
                Drives = drives,
                JobState = new BackupJobStateDto(
                    IsRunning: _backupService.IsBackupRunning,
                    CurrentTriggerType: null,
                    StartedAt: null),
                LastSuccess = lastSuccess,
                LastFailure = lastFailure,
                NextScheduledAt = nextRun,
                Settings = settings,
                ActiveWarnings = warnings,
                OverallStatus = overallStatus
            };

            return Ok(new { success = true, data = health });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving backup health");
            return StatusCode(500, new { success = false, message = "Failed to retrieve backup health data." });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/system/backup/history
    // ─────────────────────────────────────────────────────────────────────

    [HttpGet("history")]
    [Authorize(Policy = Permissions.Backup.View)]
    public async Task<IActionResult> GetHistory([FromQuery] int count = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await _backupService.GetHistoryAsync(Math.Clamp(count, 1, 100), cancellationToken);
            return Ok(new { success = true, data = history });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving backup history");
            return StatusCode(500, new { success = false, message = "Failed to retrieve backup history." });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // POST /api/system/backup/execute
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Triggers a manual backup. Returns 202 Accepted immediately;
    /// the backup runs asynchronously. Poll /health for completion.
    /// Returns 409 Conflict if a backup is already running.
    /// </summary>
    [HttpPost("execute")]
    [Authorize(Policy = Permissions.Backup.Execute)]
    public async Task<IActionResult> ExecuteBackup(CancellationToken cancellationToken)
    {
        if (_backupService.IsBackupRunning)
            return Conflict(new { success = false, message = "A backup is already in progress." });

        var userId = GetCurrentUserId();
        var userDisplay = GetCurrentUserDisplay();

        _logger.LogInformation("Manual backup requested by user {UserId} ({Display})", userId, userDisplay);

        // Fire-and-forget with a dedicated scope — the request scope will be disposed
        // before this Task executes, so we must create a fresh DI scope here.
        var scopeFactory = _scopeFactory;
        _ = Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();
            var backupService = scope.ServiceProvider.GetRequiredService<IDatabaseBackupService>();
            try
            {
                await backupService.ExecuteBackupAsync(
                    BackupTriggerType.Manual, userId, userDisplay ?? "Administrator", CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Manual backup threw unhandled exception");
            }
        }, CancellationToken.None);

        return Accepted(new { success = true, message = "Backup started. Poll /health for status." });
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/system/backup/settings
    // ─────────────────────────────────────────────────────────────────────

    [HttpGet("settings")]
    [Authorize(Policy = Permissions.Backup.View)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _backupService.GetSettingsAsync(cancellationToken);
            return Ok(new { success = true, data = settings });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving backup settings");
            return StatusCode(500, new { success = false, message = "Failed to retrieve backup settings." });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // PUT /api/system/backup/settings
    // ─────────────────────────────────────────────────────────────────────

    [HttpPut("settings")]
    [Authorize(Policy = Permissions.Backup.Configure)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateBackupSettingsRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Request body is required." });

        try
        {
            var (ok, error) = await _backupService.UpdateSettingsAsync(request, cancellationToken);
            if (!ok)
                return BadRequest(new { success = false, message = error });

            _logger.LogInformation("Backup settings updated by user {UserId}", GetCurrentUserId());
            return Ok(new { success = true, message = "Backup settings saved." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating backup settings");
            return StatusCode(500, new { success = false, message = "Failed to save backup settings." });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // POST /api/system/backup/test-path
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tests whether the provided path is writable.
    /// Used by the "Test Backup Path" button in the UI before saving settings.
    /// </summary>
    [HttpPost("test-path")]
    [Authorize(Policy = Permissions.Backup.Configure)]
    public async Task<IActionResult> TestPath([FromBody] TestPathRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Path))
            return BadRequest(new { success = false, message = "Path is required." });

        var (writable, error) = await _diskMonitor.TestPathWritableAsync(request.Path, cancellationToken);
        return Ok(new { success = writable, message = writable ? "Path is accessible and writable." : error });
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/system/backup/purge/settings
    // PUT /api/system/backup/purge/settings
    // GET /api/system/backup/purge/preview
    // POST /api/system/backup/purge
    // ─────────────────────────────────────────────────────────────────────

    [HttpGet("purge/settings")]
    [Authorize(Policy = Permissions.Backup.View)]
    public async Task<IActionResult> GetPurgeSettings(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _purgeService.GetRetentionSettingsAsync(cancellationToken);
            return Ok(new { success = true, data = settings });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purge settings");
            return StatusCode(500, new { success = false, message = "Failed to retrieve purge settings." });
        }
    }

    [HttpPut("purge/settings")]
    [Authorize(Policy = Permissions.Backup.Configure)]
    public async Task<IActionResult> UpdatePurgeSettings(
        [FromBody] UpdateRetentionSettingsRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Request body is required." });

        try
        {
            var (ok, error) = await _purgeService.UpdateRetentionSettingsAsync(request, cancellationToken);
            if (!ok) return BadRequest(new { success = false, message = error });

            _logger.LogInformation("Purge/retention settings updated by {User}", GetCurrentUserDisplay());
            return Ok(new { success = true, message = "Retention settings saved." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating purge settings");
            return StatusCode(500, new { success = false, message = "Failed to save retention settings." });
        }
    }

    /// <summary>
    /// Dry-run: returns how many rows would be deleted without deleting anything.
    /// </summary>
    [HttpGet("purge/preview")]
    [Authorize(Policy = Permissions.Backup.Purge)]
    public async Task<IActionResult> GetPurgePreview(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _purgeService.GetRetentionSettingsAsync(cancellationToken);
            var preview = await _purgeService.GetPurgePreviewAsync(settings, cancellationToken);
            return Ok(new { success = true, data = preview });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating purge preview");
            return StatusCode(500, new { success = false, message = "Failed to generate purge preview." });
        }
    }

    /// <summary>
    /// Executes the data retention purge using current configured settings.
    /// Deletes terminal invitations, notification alerts, audit logs, and occupancy logs
    /// older than the configured retention period. Optionally runs DBCC SHRINKDATABASE.
    /// NOTE: Take a backup before running this. Deleted rows cannot be recovered without a restore.
    /// </summary>
    [HttpPost("purge")]
    [Authorize(Policy = Permissions.Backup.Purge)]
    public async Task<IActionResult> ExecutePurge(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _purgeService.GetRetentionSettingsAsync(cancellationToken);
            _logger.LogWarning("Data purge initiated by {User} (retentionDays={Days})",
                GetCurrentUserDisplay(), settings.RetentionDays);

            var result = await _purgeService.ExecutePurgeAsync(settings, cancellationToken);

            if (!result.Success)
                return StatusCode(500, new { success = false, message = result.ErrorMessage, data = result });

            return Ok(new { success = true, message = $"{result.TotalPurged} records purged.", data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing purge");
            return StatusCode(500, new { success = false, message = "Purge failed." });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value
                 ?? User.FindFirst("userId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private string? GetCurrentUserDisplay()
        => User.FindFirst(ClaimTypes.Name)?.Value
        ?? User.FindFirst("username")?.Value
        ?? User.FindFirst(ClaimTypes.Email)?.Value;
}

public record TestPathRequest(string Path);
