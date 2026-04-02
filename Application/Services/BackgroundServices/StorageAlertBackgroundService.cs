using VisitorManagementSystem.Api.Application.DTOs.Backup;
using VisitorManagementSystem.Api.Application.Services.Backup;
using VisitorManagementSystem.Api.Application.Services.Notifications;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Services.BackgroundServices;

/// <summary>
/// Checks database storage and disk space every 5 minutes.
/// Fires threshold-crossing notifications to administrators via INotificationService
/// (which routes Critical/Emergency alerts automatically to email via NotificationDispatcherService).
/// Optionally triggers emergency backups when storage thresholds are breached.
///
/// Deduplication: alerts fire once per threshold crossing.
/// A threshold re-arms only after usage drops back below its hysteresis band.
/// An additional 4-hour cooldown prevents alert storms at boundary conditions.
/// </summary>
public class StorageAlertBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
    private const double EventBackupCooldownHours = 6.0;
    private const double AlertCooldownHours = 4.0;

    // Hysteresis: re-arm lower threshold only when usage drops below these levels
    private const double DbWarnRearmPercent = 65.0;
    private const double DbHighRearmPercent = 80.0;
    private const double DbCriticalRearmPercent = 90.0;
    private const double DiskWarnRearmPercent = 25.0;  // disk free % above which Warning re-arms
    private const double DiskCriticalRearmPercent = 15.0;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StorageAlertBackgroundService> _logger;

    public StorageAlertBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<StorageAlertBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StorageAlertBackgroundService started");

        // Allow application to fully start up before first check
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCheckCycleAsync(stoppingToken);
            await Task.Delay(CheckInterval, stoppingToken);
        }

        _logger.LogInformation("StorageAlertBackgroundService stopped");
    }

    private async Task RunCheckCycleAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var backupService = scope.ServiceProvider.GetRequiredService<IDatabaseBackupService>();
            var dbMonitor = scope.ServiceProvider.GetRequiredService<IDatabaseStorageMonitor>();
            var diskMonitor = scope.ServiceProvider.GetRequiredService<IDiskStorageMonitor>();
            var alertState = scope.ServiceProvider.GetRequiredService<IStorageAlertStateStore>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var settings = await backupService.GetSettingsAsync(stoppingToken);
            if (!settings.AlertEnabled) return;

            // ── Database size check ──────────────────────────────────────────
            DatabaseStorageDto? dbMetrics = null;
            try { dbMetrics = await dbMonitor.GetStorageMetricsAsync(stoppingToken); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Storage monitor: DB size query failed, skipping cycle");
                return;
            }

            if (dbMetrics.IsExpressEdition)
            {
                await CheckDbThresholdAsync(
                    dbMetrics, settings, alertState, backupService, notificationService, stoppingToken);
            }

            // ── Disk space check ─────────────────────────────────────────────
            var dbPaths = dbMetrics.Files.Select(f => f.PhysicalPath).Where(p => !string.IsNullOrEmpty(p));
            List<DriveHealthDto>? drives = null;
            try
            {
                drives = await diskMonitor.GetDriveHealthAsync(
                    dbPaths, settings.DestinationPath,
                    settings.DiskWarnFreePercent, settings.DiskCriticalFreePercent,
                    stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Storage monitor: disk check failed, skipping disk alerts");
            }

            if (drives != null)
            {
                await CheckDiskThresholdsAsync(drives, alertState, notificationService, stoppingToken);
            }
        }
        catch (OperationCanceledException) { /* shutting down */ }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in StorageAlertBackgroundService check cycle");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Database threshold logic
    // ─────────────────────────────────────────────────────────────────────

    private async Task CheckDbThresholdAsync(
        DatabaseStorageDto metrics,
        BackupSettingsDto settings,
        IStorageAlertStateStore alertState,
        IDatabaseBackupService backupService,
        INotificationService notificationService,
        CancellationToken ct)
    {
        var usedPercent = metrics.UsagePercent;
        var lastLevel = await alertState.GetLastDbAlertLevelAsync(ct);
        var lastFiredAt = await alertState.GetLastAlertFiredAtAsync(ct);

        // Determine current tier
        string currentTier;
        if (usedPercent >= settings.DbCriticalThresholdPercent)
            currentTier = StorageAlertLevel.Critical;
        else if (usedPercent >= settings.DbAlertThresholdPercent)
            currentTier = StorageAlertLevel.High;
        else if (usedPercent >= settings.DbWarnThresholdPercent)
            currentTier = StorageAlertLevel.Warning;
        else
            currentTier = StorageAlertLevel.None;

        // Handle recovery — re-arm lower thresholds when usage drops below hysteresis band
        if (currentTier == StorageAlertLevel.None && lastLevel != StorageAlertLevel.None)
        {
            // Fully recovered
            await alertState.SetLastDbAlertLevelAsync(StorageAlertLevel.None, ct);
            _logger.LogInformation("DB storage recovered to {Percent:F1}% — alert state reset", usedPercent);
            return;
        }

        bool partialRecovery =
            (lastLevel == StorageAlertLevel.Critical && usedPercent < DbCriticalRearmPercent) ||
            (lastLevel == StorageAlertLevel.High && usedPercent < DbHighRearmPercent) ||
            (lastLevel == StorageAlertLevel.Warning && usedPercent < DbWarnRearmPercent);

        if (partialRecovery)
        {
            var newLevel = usedPercent >= settings.DbAlertThresholdPercent ? StorageAlertLevel.High
                         : usedPercent >= settings.DbWarnThresholdPercent ? StorageAlertLevel.Warning
                         : StorageAlertLevel.None;
            await alertState.SetLastDbAlertLevelAsync(newLevel, ct);
        }

        // Determine if we should fire
        bool shouldFire = IsHigherTier(currentTier, lastLevel) &&
                          !IsWithinCooldown(lastFiredAt, AlertCooldownHours);

        if (!shouldFire) return;

        // Determine if we should trigger a backup
        bool triggerBackup = false;
        string backupBlockReason = string.Empty;

        bool backupWanted = currentTier == StorageAlertLevel.Critical
            ? settings.AutoBackupOnCritical
            : settings.AutoBackupOnAlert;

        if (backupWanted && !backupService.IsBackupRunning)
        {
            var lastEventBackup = await alertState.GetLastEventBackupAtAsync(ct);
            if (!IsWithinCooldown(lastEventBackup, EventBackupCooldownHours))
                triggerBackup = true;
            else
                backupBlockReason = "event backup cooldown active";
        }

        // Map tier to alert type and priority
        var (alertType, priority) = currentTier switch
        {
            StorageAlertLevel.Critical => (NotificationAlertType.DatabaseStorageEmergency, AlertPriority.Critical),
            StorageAlertLevel.High => (NotificationAlertType.DatabaseStorageCritical, AlertPriority.High),
            _ => (NotificationAlertType.DatabaseStorageWarning, AlertPriority.Medium)
        };

        await notificationService.NotifyStorageAlertAsync(
            alertType, currentTier,
            usedPercent, metrics.TotalDataAllocatedMb, metrics.ExpressLimitMb,
            "Database", triggerBackup, priority, ct);

        // Update dedup state
        await alertState.SetLastDbAlertLevelAsync(currentTier, ct);
        await alertState.SetLastAlertFiredAtAsync(DateTime.UtcNow, ct);

        _logger.LogWarning("DB storage alert fired: {Tier} at {Percent:F1}% (triggerBackup={Trigger})",
            currentTier, usedPercent, triggerBackup);

        // Execute backup if warranted.
        // Must use a fresh scope — the outer scope will be disposed before this Task executes.
        if (triggerBackup)
        {
            await alertState.SetLastEventBackupAtAsync(DateTime.UtcNow, ct);
            var scopeFactory = _scopeFactory;
            var logger = _logger;
            _ = Task.Run(async () =>
            {
                using var backupScope = scopeFactory.CreateScope();
                var svc = backupScope.ServiceProvider.GetRequiredService<IDatabaseBackupService>();
                try
                {
                    var result = await svc.ExecuteBackupAsync(
                        BackupTriggerType.StorageAlert, null, "System — Storage Alert", CancellationToken.None);
                    if (!result.Success)
                        logger.LogError("Event-triggered backup failed: {Error}", result.ErrorMessage);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Event-triggered backup threw an exception");
                }
            }, CancellationToken.None);
        }
        else if (!string.IsNullOrEmpty(backupBlockReason))
        {
            _logger.LogInformation("Event backup skipped: {Reason}", backupBlockReason);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Disk threshold logic
    // ─────────────────────────────────────────────────────────────────────

    private async Task CheckDiskThresholdsAsync(
        List<DriveHealthDto> drives,
        IStorageAlertStateStore alertState,
        INotificationService notificationService,
        CancellationToken ct)
    {
        // Use the worst drive for the dedup state (simplest approach for single-machine deployments)
        var worstDrive = drives
            .OrderBy(d => d.FreePercent)
            .FirstOrDefault();

        if (worstDrive == null) return;

        string currentTier;
        if (worstDrive.FreePercent <= drives.Min(d => d.FreePercent) &&
            worstDrive.AlertLevel == StorageAlertLevel.Critical)
            currentTier = StorageAlertLevel.Critical;
        else if (worstDrive.AlertLevel == StorageAlertLevel.Warning)
            currentTier = StorageAlertLevel.Warning;
        else
            currentTier = StorageAlertLevel.None;

        var lastLevel = await alertState.GetLastDiskAlertLevelAsync(ct);
        var lastFiredAt = await alertState.GetLastAlertFiredAtAsync(ct);

        // Recovery handling
        if (currentTier == StorageAlertLevel.None)
        {
            if (lastLevel != StorageAlertLevel.None)
                await alertState.SetLastDiskAlertLevelAsync(StorageAlertLevel.None, ct);
            return;
        }

        bool partialRecovery =
            (lastLevel == StorageAlertLevel.Critical && worstDrive.FreePercent > DiskCriticalRearmPercent) ||
            (lastLevel == StorageAlertLevel.Warning && worstDrive.FreePercent > DiskWarnRearmPercent);

        if (partialRecovery)
        {
            var newLevel = currentTier;
            await alertState.SetLastDiskAlertLevelAsync(newLevel, ct);
        }

        bool shouldFire = IsHigherTier(currentTier, lastLevel) &&
                          !IsWithinCooldown(lastFiredAt, AlertCooldownHours);

        if (!shouldFire) return;

        var (alertType, priority) = currentTier == StorageAlertLevel.Critical
            ? (NotificationAlertType.DiskSpaceCritical, AlertPriority.Critical)
            : (NotificationAlertType.DiskSpaceWarning, AlertPriority.Medium);

        await notificationService.NotifyStorageAlertAsync(
            alertType, currentTier,
            worstDrive.FreePercent, 0, 0,
            worstDrive.DriveLetter, false, priority, ct);

        await alertState.SetLastDiskAlertLevelAsync(currentTier, ct);
        await alertState.SetLastAlertFiredAtAsync(DateTime.UtcNow, ct);

        _logger.LogWarning("Disk alert fired: {Tier} on {Drive} ({FreePercent:F1}% free)",
            currentTier, worstDrive.DriveLetter, worstDrive.FreePercent);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────

    private static readonly string[] TierOrder = [StorageAlertLevel.None, StorageAlertLevel.Warning, StorageAlertLevel.High, StorageAlertLevel.Critical];

    private static bool IsHigherTier(string current, string last)
        => Array.IndexOf(TierOrder, current) > Array.IndexOf(TierOrder, last);

    private static bool IsWithinCooldown(DateTime? lastFiredAt, double cooldownHours)
        => lastFiredAt.HasValue && (DateTime.UtcNow - lastFiredAt.Value).TotalHours < cooldownHours;
}
