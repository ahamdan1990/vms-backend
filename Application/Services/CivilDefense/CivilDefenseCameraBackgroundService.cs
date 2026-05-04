using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.Commands.CivilDefense;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Hubs;
using VisitorManagementSystem.Api.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Application.Services.CivilDefense;

/// <summary>
/// Background service that samples frames from Civil Defense cameras at configured intervals,
/// sends them to CompreFace for recognition, and pushes results via SignalR.
/// No video stream is delivered to the frontend — only recognition event payloads.
/// </summary>
public class CivilDefenseCameraBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<CivilDefenseHub> _hub;
    private readonly ILogger<CivilDefenseCameraBackgroundService> _logger;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    public CivilDefenseCameraBackgroundService(
        IServiceScopeFactory scopeFactory,
        IHubContext<CivilDefenseHub> hub,
        ILogger<CivilDefenseCameraBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _hub          = hub;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CivilDefense camera background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SampleAllCamerasAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CivilDefense camera sampling loop.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }

        _logger.LogInformation("CivilDefense camera background service stopped.");
    }

    private async Task SampleAllCamerasAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db      = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var now = DateTime.UtcNow;

        // Load cameras that have a Civil Defense role and are currently active
        var cameras = await db.Cameras
            .AsNoTracking()
            .Where(c => c.CameraRole != CameraRole.None && c.Status == CameraStatus.Active && !c.IsDeleted)
            .ToListAsync(ct);

        if (cameras.Count == 0)
            return;

        var tasks = cameras.Select(camera => SampleCameraAsync(camera.Id, camera.CameraRole,
            camera.FrameSamplingIntervalSeconds, mediator, ct));

        await Task.WhenAll(tasks);
    }

    private async Task SampleCameraAsync(int cameraId, CameraRole role, int intervalSecs,
        IMediator mediator, CancellationToken ct)
    {
        // Fetch a single JPEG frame from the camera via the existing camera streaming infrastructure.
        // CaptureFrameAsync is a placeholder — replace with actual RTSP frame grab.
        byte[]? frame;
        try
        {
            frame = await CaptureFrameFromCameraAsync(cameraId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Frame capture failed for camera {CameraId}", cameraId);
            return;
        }

        if (frame == null || frame.Length == 0)
            return;

        var threshold = 0.80; // Default; could be loaded from SystemConfigurations

        var result = await mediator.Send(new CdProcessFaceFrameCommand
        {
            FrameBytes          = frame,
            CameraId            = cameraId,
            CameraRole          = role,
            ConfidenceThreshold = threshold,
        }, ct);

        if (!result.FaceDetected)
            return;

        if (result.Matched)
        {
            // Broadcast to both receptionist and manager groups
            await _hub.Clients.Group("cd-receptionist")
                .SendAsync("FaceRecognized", result, ct);
            await _hub.Clients.Group("cd-manager")
                .SendAsync("FaceRecognized", result, ct);

            _logger.LogDebug("CD: face matched — visitor {VisitorId} at camera {CameraId}",
                result.VisitorId, cameraId);
        }
        else
        {
            // Unknown face — alert receptionist only
            await _hub.Clients.Group("cd-receptionist")
                .SendAsync("UnknownFaceDetected", new
                {
                    cameraId   = cameraId,
                    cameraRole = role.ToString(),
                    similarity = result.Similarity,
                }, ct);
        }
    }

    /// <summary>
    /// Placeholder: capture one JPEG frame from an RTSP/IP camera.
    /// Implement using OpenCvSharp, FFmpeg.AutoGen, or a streaming proxy.
    /// </summary>
    private static Task<byte[]?> CaptureFrameFromCameraAsync(int cameraId, CancellationToken ct)
    {
        // Return null — no frame captured (service is wired but not streaming yet).
        // Replace with real frame capture logic when camera hardware is available.
        return Task.FromResult<byte[]?>(null);
    }
}
