using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.Services.Visitors;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Handler for uploading visitor profile photo
/// </summary>
public class UploadVisitorProfilePhotoCommandHandler : IRequestHandler<UploadVisitorProfilePhotoCommand, PhotoUploadResult>
{
    private readonly IVisitorService _visitorService;
    private readonly ILogger<UploadVisitorProfilePhotoCommandHandler> _logger;

    public UploadVisitorProfilePhotoCommandHandler(
        IVisitorService visitorService,
        ILogger<UploadVisitorProfilePhotoCommandHandler> logger)
    {
        _visitorService = visitorService;
        _logger = logger;
    }

    public async Task<PhotoUploadResult> Handle(UploadVisitorProfilePhotoCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing upload profile photo command for visitor: {VisitorId}", request.VisitorId);

            if (request.File == null || request.File.Length == 0)
            {
                throw new ArgumentException("No file provided");
            }

            // forceUpdateProfilePhoto: true — this endpoint is an explicit admin action,
            // so the profile photo must always be replaced regardless of whether one exists.
            var result = await _visitorService.UploadVisitorPhotoAsync(
                request.VisitorId,
                request.File,
                forceUpdateProfilePhoto: true,
                cancellationToken);

            _logger.LogInformation("Profile photo uploaded successfully for visitor: {VisitorId}, FaceDetected: {FaceDetected}",
                request.VisitorId, result.FaceDetected);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile photo for visitor: {VisitorId}", request.VisitorId);
            throw;
        }
    }
}
