using VisitorManagementSystem.Api.Application.DTOs.FaceDetection;

namespace VisitorManagementSystem.Api.Application.Services.FaceDetection;

public interface IFaceTemplateEnrollmentService
{
    Task<FaceTemplateEnrollmentBatchResultDto> EnrollExistingProfilePhotosAsync(
        bool includeVisitors,
        bool includeUsers,
        bool force,
        CancellationToken cancellationToken = default);
}
