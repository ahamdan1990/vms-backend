using VisitorManagementSystem.Api.Application.DTOs.Cameras;

namespace VisitorManagementSystem.Api.Application.Services.Cameras;

/// <summary>
/// Owns long-running camera stream workers and exposes runtime frame/inference status.
/// </summary>
public interface ICameraStreamRuntimeService
{
    Task<bool> StartStreamAsync(int cameraId, CancellationToken cancellationToken = default);

    Task<bool> StopStreamAsync(
        int cameraId,
        bool graceful = true,
        CancellationToken cancellationToken = default,
        bool pauseAutoStart = true);

    Task StopAllAsync(CancellationToken cancellationToken = default);

    bool IsStreaming(int cameraId);

    bool IsAutoStartPaused(int cameraId);

    void PauseAutoStart(int cameraId);

    void ResumeAutoStart(int cameraId);

    CameraStreamInfo? GetStreamInfo(int cameraId);

    void RecordFrameCapture(CameraFrameCaptureResult result);

    void RecordInferenceResult(CameraFrameRecognitionResultDto result);

    bool IsFacialRecognitionEnabled(int cameraId);

    void SetFacialRecognitionEnabled(int cameraId, bool enabled);

    /// <summary>
    /// Returns a copy of the most recently grabbed frame bytes for the given camera,
    /// or null if no frame has been captured yet. Intended for debug inspection only.
    /// </summary>
    (byte[]? Bytes, DateTime? CapturedAt) GetLastFrameSnapshot(int cameraId);
}
