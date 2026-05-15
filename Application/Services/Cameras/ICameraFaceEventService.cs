using VisitorManagementSystem.Api.Application.DTOs.Cameras;

namespace VisitorManagementSystem.Api.Application.Services.Cameras;

/// <summary>
/// Publishes camera face recognition events after cooldown and duplicate checks.
/// </summary>
public interface ICameraFaceEventService
{
    Task<IReadOnlyList<CameraFrameFaceEventDto>> PublishFrameEventsAsync(
        CameraFrameRecognitionResultDto recognitionResult,
        int? processedByUserId = null,
        CancellationToken cancellationToken = default);
}
