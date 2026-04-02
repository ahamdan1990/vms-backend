using VisitorManagementSystem.Api.Application.Services.Backup;

namespace VisitorManagementSystem.Api.Application.Services.BackgroundServices;

/// <summary>
/// Checks every 60 seconds whether a scheduled backup is due and executes it.
/// On startup, if a scheduled run was missed while the app was stopped,
/// an immediate catch-up backup is executed.
/// </summary>
public class DatabaseBackupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseBackupBackgroundService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(60);

    public DatabaseBackupBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseBackupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DatabaseBackupBackgroundService started");

        // Brief startup delay to allow DI and DB to be ready
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        // On startup: check for missed schedule
        await CheckAndRunAsync(stoppingToken, startupCheck: true);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);
            await CheckAndRunAsync(stoppingToken, startupCheck: false);
        }

        _logger.LogInformation("DatabaseBackupBackgroundService stopped");
    }

    private async Task CheckAndRunAsync(CancellationToken stoppingToken, bool startupCheck)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var backupService = scope.ServiceProvider.GetRequiredService<IDatabaseBackupService>();

            var settings = await backupService.GetSettingsAsync(stoppingToken);

            if (!settings.Enabled) return;
            if (backupService.IsBackupRunning) return;

            // Parse NextRunAt
            DateTime? nextRun = null;
            if (!string.IsNullOrWhiteSpace(
                (await GetRawConfigAsync(scope, "Backup", "NextRunAt", stoppingToken))))
            {
                var raw = await GetRawConfigAsync(scope, "Backup", "NextRunAt", stoppingToken);
                if (DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                    nextRun = parsed;
            }

            bool isDue = nextRun.HasValue && nextRun.Value <= DateTime.UtcNow;
            bool isMissed = startupCheck && nextRun.HasValue && nextRun.Value < DateTime.UtcNow.AddMinutes(-5);

            if (!isDue && !isMissed) return;

            if (isMissed)
                _logger.LogWarning("Scheduled backup was missed (NextRunAt: {NextRun}). Running catch-up backup.", nextRun);

            _logger.LogInformation("Triggering {Type} backup", startupCheck && isMissed ? "catch-up" : "scheduled");

            var result = await backupService.ExecuteBackupAsync(
                Domain.Entities.BackupTriggerType.Scheduled,
                null,
                "System",
                stoppingToken);

            if (!result.Success)
                _logger.LogError("Scheduled backup failed: {Error}", result.ErrorMessage);

            // Advance NextRunAt by 24 hours from the target time
            await AdvanceNextRunAtAsync(scope, settings.ScheduleTime, nextRun, stoppingToken);
        }
        catch (OperationCanceledException) { /* shutting down */ }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in DatabaseBackupBackgroundService");
        }
    }

    private static async Task<string> GetRawConfigAsync(IServiceScope scope, string category, string key, CancellationToken ct)
    {
        var unitOfWork = scope.ServiceProvider.GetRequiredService<Domain.Interfaces.Repositories.IUnitOfWork>();
        var config = await unitOfWork.SystemConfigurations.GetByCategoryAndKeyAsync(category, key, ct);
        return config?.Value ?? string.Empty;
    }

    private static async Task AdvanceNextRunAtAsync(IServiceScope scope, string scheduleTime, DateTime? currentNext, CancellationToken ct)
    {
        var unitOfWork = scope.ServiceProvider.GetRequiredService<Domain.Interfaces.Repositories.IUnitOfWork>();

        if (!TimeSpan.TryParse(scheduleTime, out var time))
            time = TimeSpan.FromHours(2);

        // Next occurrence: start from currentNext (or now) and find next future slot
        var base_ = currentNext ?? DateTime.UtcNow;
        var next = base_.Date.Add(time).AddDays(1);
        while (next <= DateTime.UtcNow)
            next = next.AddDays(1);

        var config = await unitOfWork.SystemConfigurations.GetByCategoryAndKeyAsync("Backup", "NextRunAt", ct);
        if (config != null)
        {
            config.Value = next.ToString("O");
            config.UpdateModifiedOn();
            unitOfWork.SystemConfigurations.Update(config);
            await unitOfWork.SaveChangesAsync(ct);
        }
    }
}
