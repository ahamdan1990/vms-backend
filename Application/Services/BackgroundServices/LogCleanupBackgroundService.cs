using VisitorManagementSystem.Api.Application.Services.Backup;

namespace VisitorManagementSystem.Api.Application.Services.BackgroundServices;

/// <summary>
/// Runs log-file cleanup on a configured daily schedule.
/// Each cycle creates a new DI scope to avoid scoped-service lifetime issues.
/// </summary>
public class LogCleanupBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LogCleanupBackgroundService> _logger;

    public LogCleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<LogCleanupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LogCleanupBackgroundService started");
        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCycleAsync(stoppingToken);
            await Task.Delay(CheckInterval, stoppingToken);
        }

        _logger.LogInformation("LogCleanupBackgroundService stopped");
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<ILogFileService>();

            var settings = await logService.GetLogSettingsAsync(ct);
            if (!settings.ScheduleEnabled || settings.NextRunAt == null) return;
            if (settings.NextRunAt.Value > DateTime.UtcNow) return;

            _logger.LogInformation("Scheduled log cleanup starting (retention: {Days} days)", settings.RetentionDays);

            var result = await logService.PurgeOldLogsAsync(settings.RetentionDays, ct);

            if (result.Success)
                _logger.LogInformation(
                    "Scheduled log cleanup completed: {Count} files deleted, {Size} freed",
                    result.FilesDeleted, result.BytesFreedDisplay);
            else
                _logger.LogError("Scheduled log cleanup failed: {Error}", result.ErrorMessage);

            // Advance NextRunAt to tomorrow at same time
            await logService.UpdateLogSettingsAsync(new DTOs.Backup.UpdateLogSettingsRequest(
                settings.RetentionDays,
                settings.AutoPurgeEnabled,
                settings.ScheduleEnabled,
                settings.ScheduleTime,
                settings.MaxFolderSizeMb), ct);
        }
        catch (OperationCanceledException) { /* shutting down */ }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in LogCleanupBackgroundService");
        }
    }
}
