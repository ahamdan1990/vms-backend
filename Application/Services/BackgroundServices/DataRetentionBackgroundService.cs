using VisitorManagementSystem.Api.Application.Services.Backup;

namespace VisitorManagementSystem.Api.Application.Services.BackgroundServices;

/// <summary>
/// Runs data-retention purges on a configured schedule (daily at a set time, or on a chosen day of the week).
/// Each cycle creates a new DI scope to avoid scoped-service lifetime issues.
/// </summary>
public class DataRetentionBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataRetentionBackgroundService> _logger;

    public DataRetentionBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DataRetentionBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DataRetentionBackgroundService started");
        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCycleAsync(stoppingToken);
            await Task.Delay(CheckInterval, stoppingToken);
        }

        _logger.LogInformation("DataRetentionBackgroundService stopped");
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var purgeService = scope.ServiceProvider.GetRequiredService<IDataPurgeService>();

            var settings = await purgeService.GetRetentionSettingsAsync(ct);
            if (!settings.ScheduleEnabled || settings.NextRunAt == null) return;
            if (settings.NextRunAt.Value > DateTime.UtcNow) return;

            _logger.LogInformation("Scheduled data retention purge starting (cutoff: {Days} days)", settings.RetentionDays);

            var result = await purgeService.ExecutePurgeAsync(settings, ct);

            if (result.Success)
                _logger.LogInformation(
                    "Scheduled purge completed: {Total} rows removed (invitations={Inv}, notifications={Notif}, auditLogs={Audit}, occupancy={Occ})",
                    result.TotalPurged, result.InvitationsPurged, result.NotificationsPurged,
                    result.AuditLogsPurged, result.OccupancyLogsPurged);
            else
                _logger.LogError("Scheduled purge failed: {Error}", result.ErrorMessage);

            // Advance to next scheduled occurrence
            var next = DataPurgeService.CalculateNextRunAt(settings.ScheduleTime, settings.ScheduleDayOfWeek);
            await purgeService.UpdateRetentionSettingsAsync(new DTOs.Backup.UpdateRetentionSettingsRequest(
                settings.AutoPurgeAfterBackup,
                settings.ScheduleEnabled,
                settings.ScheduleTime,
                settings.ScheduleDayOfWeek,
                settings.RetentionDays,
                settings.PurgeInvitations,
                settings.PurgeNotifications,
                settings.PurgeAuditLogs,
                settings.PurgeOccupancyLogs,
                settings.ShrinkDbAfterPurge), ct);

            _ = next; // NextRunAt is recalculated inside UpdateRetentionSettingsAsync
        }
        catch (OperationCanceledException) { /* shutting down */ }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in DataRetentionBackgroundService");
        }
    }
}
