using System.Collections.Concurrent;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Services.Cameras;

/// <summary>
/// Starts configured camera stream workers on application startup and stops them on shutdown.
/// </summary>
public sealed class CameraStreamHostedService : BackgroundService
{
    private const int TickIntervalSeconds = 5;
    private const int DefaultHealthCheckIntervalSeconds = 30;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICameraStreamRuntimeService _streamRuntime;
    private readonly ILogger<CameraStreamHostedService> _logger;
    private readonly ConcurrentDictionary<int, DateTime> _nextCheckDue = new();

    public CameraStreamHostedService(
        IServiceScopeFactory scopeFactory,
        ICameraStreamRuntimeService streamRuntime,
        ILogger<CameraStreamHostedService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _streamRuntime = streamRuntime ?? throw new ArgumentNullException(nameof(streamRuntime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Camera stream hosted service started. Auto-start reconciliation will run after startup delay.");
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await EnsureAutoStartCamerasRunningAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Camera stream auto-start cycle failed; the hosted service will retry");
                }

                await Task.Delay(TimeSpan.FromSeconds(TickIntervalSeconds), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Camera stream hosted service failed during startup");
        }
    }

    private async Task EnsureAutoStartCamerasRunningAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var cameras = await unitOfWork.Cameras.GetAsync(
            camera => camera.IsActive && !camera.IsDeleted,
            stoppingToken);

        var autoStartCameras = cameras
            .Where(camera =>
            {
                var config = camera.GetConfiguration();
                return config.Enabled &&
                       config.AutoStart &&
                       camera.CameraType != CameraType.USB;
            })
            .OrderBy(camera => camera.Priority)
            .ToList();

        _logger.LogInformation(
            "Camera auto-start scan found {CameraCount} active camera(s), {AutoStartCount} backend auto-start candidate(s)",
            cameras.Count(),
            autoStartCameras.Count);

        foreach (var camera in cameras.OrderBy(camera => camera.Priority))
        {
            var config = camera.GetConfiguration();
            if (!config.AutoStart)
            {
                if (camera.CameraType != CameraType.USB)
                {
                    _logger.LogInformation(
                        "Camera {CameraId} ({CameraName}) skipped by backend auto-start. Type={CameraType}, RuntimeEnabled={RuntimeEnabled}, AutoStart={AutoStart}",
                        camera.Id,
                        camera.Name,
                        camera.CameraType,
                        config.Enabled,
                        config.AutoStart);
                }

                continue;
            }

            if (!config.Enabled)
            {
                _logger.LogWarning(
                    "Camera {CameraId} ({CameraName}) has auto-start enabled but runtime configuration is disabled",
                    camera.Id,
                    camera.Name);
            }
            else if (camera.CameraType == CameraType.USB)
            {
                _logger.LogInformation(
                    "Camera {CameraId} ({CameraName}) has auto-start enabled but is USB; browser client runtime owns this source",
                    camera.Id,
                    camera.Name);
            }
        }

        var now = DateTime.UtcNow;
        foreach (var camera in autoStartCameras)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var config = camera.GetConfiguration();
            var intervalSeconds = Math.Max(TickIntervalSeconds, config.HealthCheckIntervalSeconds > 0
                ? config.HealthCheckIntervalSeconds
                : DefaultHealthCheckIntervalSeconds);

            if (_nextCheckDue.TryGetValue(camera.Id, out var nextDue) && now < nextDue)
            {
                continue;
            }

            _nextCheckDue[camera.Id] = now.AddSeconds(intervalSeconds);

            try
            {
                if (_streamRuntime.IsStreaming(camera.Id))
                {
                    continue;
                }

                if (_streamRuntime.IsAutoStartPaused(camera.Id))
                {
                    _logger.LogDebug(
                        "Auto-start is paused for camera {CameraId} ({CameraName}); skipping ensure-running check",
                        camera.Id,
                        camera.Name);
                    continue;
                }

                var started = await _streamRuntime.StartStreamAsync(camera.Id, stoppingToken);
                _logger.LogInformation(
                    "Auto-start camera stream ensure-running check for {CameraId} ({CameraName}) result: {Started}",
                    camera.Id,
                    camera.Name,
                    started);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Auto-start ensure-running check failed for camera {CameraId} ({CameraName})",
                    camera.Id,
                    camera.Name);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _streamRuntime.StopAllAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
