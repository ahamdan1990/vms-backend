using MediatR;
using VisitorManagementSystem.Api.Application.Services.FileUploadService;

namespace VisitorManagementSystem.Api.Application.Commands.Users;

/// <summary>
/// Handler for removing user profile photo
/// </summary>
public class RemoveProfilePhotoCommandHandler : IRequestHandler<RemoveProfilePhotoCommand>
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<RemoveProfilePhotoCommandHandler> _logger;

    public RemoveProfilePhotoCommandHandler(
        IFileUploadService fileUploadService,
        ILogger<RemoveProfilePhotoCommandHandler> logger)
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    public async Task Handle(RemoveProfilePhotoCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing remove profile photo command for user: {UserId}", request.UserId);

            await _fileUploadService.RemoveProfilePhotoAsync(request.UserId);

            _logger.LogInformation("Profile photo removed successfully for user: {UserId}", request.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing profile photo for user: {UserId}", request.UserId);
            throw;
        }
    }
}
