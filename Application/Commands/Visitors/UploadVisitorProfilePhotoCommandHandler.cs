using MediatR;
using VisitorManagementSystem.Api.Application.Services.Visitors;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Handler for uploading visitor profile photo
/// </summary>
public class UploadVisitorProfilePhotoCommandHandler : IRequestHandler<UploadVisitorProfilePhotoCommand, string>
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

    public async Task<string> Handle(UploadVisitorProfilePhotoCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing upload profile photo command for visitor: {VisitorId}", request.VisitorId);

            if (request.File == null || request.File.Length == 0)
            {
                throw new ArgumentException("No file provided");
            }

            // VisitorService will handle validation and saving
            var photoUrl = await _visitorService.UploadVisitorPhotoAsync(
                request.VisitorId,
                request.File,
                cancellationToken);

            _logger.LogInformation("Profile photo uploaded successfully for visitor: {VisitorId}", request.VisitorId);

            return photoUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile photo for visitor: {VisitorId}", request.VisitorId);
            throw;
        }
    }
}
