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

    /// <summary>
    /// Removes in-memory cooldown entries for a person so a new face event can fire immediately.
    /// Call after a face event's status is changed away from Pending (dismiss / review / match).
    /// </summary>
    void ClearPersonCooldown(int cameraId, bool isKnown, string personType, int? personId, string? subjectId);

    /// <summary>
    /// Marks all pending face events for a person as Reviewed and clears their cooldown cache.
    /// Call after a manual attendance action (check-in, temp-return) performed outside the face event modal.
    /// </summary>
    Task AutoReviewPendingEventsForPersonAsync(
        string personType,
        int personId,
        CancellationToken cancellationToken = default);
}
