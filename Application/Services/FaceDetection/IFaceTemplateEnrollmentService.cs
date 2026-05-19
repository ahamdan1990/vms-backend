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
}
