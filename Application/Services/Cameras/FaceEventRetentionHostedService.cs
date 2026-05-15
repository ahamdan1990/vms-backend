using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Services.Cameras;

/// <summary>
/// Runs once per hour. Hard-deletes expired unknown face events and their snapshot files.
/// Known events (ExpiresAt = null) and FIFO candidates are kept until manually reviewed.
/// </summary>
public sealed class FaceEventRetentionHostedService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFaceSnapshotService _snapshotService;
    private readonly ILogger<FaceEventRetentionHostedService> _logger;

    public FaceEventRetentionHostedService(
        IServiceScopeFactory scopeFactory,
        IFaceSnapshotService snapshotService,
        ILogger<FaceEventRetentionHostedService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunRetentionPassAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Face event retention pass failed; will retry next hour");
            }

            await Task.Delay(TickInterval, stoppingToken);
        }
    }

    private async Task RunRetentionPassAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var cutoff = DateTime.UtcNow;
        var totalDeleted = 0;

        while (true)
        {
            var batch = await unitOfWork.FaceEvents.GetExpiredEventsAsync(cutoff, batchSize: 200, cancellationToken);
            if (batch.Count == 0)
                break;

            // Delete snapshot files first so we never orphan a DB record.
            foreach (var ev in batch)
            {
                if (!string.IsNullOrEmpty(ev.SnapshotPath))
                    _snapshotService.DeleteSnapshot(ev.SnapshotPath);
            }

            var ids = batch.Select(e => e.Id);
            await unitOfWork.FaceEvents.HardDeleteBatchAsync(ids, cancellationToken);
            totalDeleted += batch.Count;

            if (batch.Count < 200)
                break;
        }

        if (totalDeleted > 0)
        {
            _logger.LogInformation("Face event retention: deleted {Count} expired event(s)", totalDeleted);
        }
    }
}
