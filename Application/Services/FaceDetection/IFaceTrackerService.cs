namespace VisitorManagementSystem.Api.Application.Services.FaceDetection;

/// <summary>
/// Luxand tracker API abstraction for per-camera stable face IDs.
/// </summary>
public interface IFaceTrackerService
{
    bool IsAvailable { get; }

    bool HasCameraTracker(int cameraId);

    bool CreateCameraTracker(int cameraId, int maxFaces = 64, int trackTimeoutMs = 0, int reIdTimeoutMs = 0);

    void DeleteCameraTracker(int cameraId);

    Task<IReadOnlyList<TrackedFace>> FeedFrameAsync(
        int cameraId,
        byte[] jpegBytes,
        CancellationToken cancellationToken = default);

    float MatchTemplates(byte[] probe, byte[] stored);
}
