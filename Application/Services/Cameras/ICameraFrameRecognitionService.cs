using VisitorManagementSystem.Api.Application.DTOs.Cameras;

namespace VisitorManagementSystem.Api.Application.Services.Cameras;

/// <summary>
/// Processes client or server camera frames through the currently available face recognition path.
/// </summary>
public interface ICameraFrameRecognitionService
{
    Task<CameraFrameRecognitionResultDto> ProcessFrameAsync(
        int cameraId,
        CameraFrameProcessingRequestDto request,
        int? processedByUserId = null,
        string? clientIpAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs raw detection on the provided frame bytes and returns annotated face results
    /// showing which faces passed or were filtered out, for live debug inspection.
    /// </summary>
    Task<CameraDebugFrameDto> InspectFrameAsync(
        int cameraId,
        byte[] frameBytes,
        CancellationToken cancellationToken = default);
}
