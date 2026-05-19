using VisitorManagementSystem.Api.Application.DTOs.FaceDetection;

namespace VisitorManagementSystem.Api.Application.Services.FaceDetection;

public interface IFaceTemplateEnrollmentService
{
    Task<FaceTemplateEnrollmentBatchResultDto> EnrollExistingProfilePhotosAsync(
        bool includeVisitors,
        bool includeUsers,
        bool force,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an uploaded image as the visitor's profile photo and immediately enrolls the face.
    /// </summary>
    Task<FaceTemplateEnrollmentItemResultDto> EnrollVisitorPhotoAsync(
        int visitorId,
        byte[] imageBytes,
        string originalFileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enrolls an image as an additional face template for an existing person WITHOUT
    /// changing their profile photo. Up to MaxAdditionalTemplatesPerIdentity secondary
    /// templates are kept; oldest non-primary is evicted when the limit is reached.
    /// Returns SuggestPromoteToPrimary=true when the new template quality exceeds the current primary.
    /// </summary>
    Task<FaceTemplateEnrollmentItemResultDto> EnrollAdditionalFaceAsync(
        string personType,
        int personId,
        byte[] imageBytes,
        string? sourcePath = null,
        string source = "Detection",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the snapshot attached to a known face event and enrolls it as an additional
    /// template for the matched person. The profile photo is never changed.
    /// </summary>
    Task<FaceTemplateEnrollmentItemResultDto> EnrollFromFaceEventSnapshotAsync(
        int faceEventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an uploaded image as the user's/staff member's profile photo and immediately
    /// enrolls the face as their primary face template.
    /// </summary>
    Task<FaceTemplateEnrollmentItemResultDto> EnrollUserPhotoAsync(
        int userId,
        byte[] imageBytes,
        string originalFileName,
        CancellationToken cancellationToken = default);
}
