using MediatR;
using VisitorManagementSystem.Api.Application.Services.FileUploadService;

namespace VisitorManagementSystem.Api.Application.Commands.Users;

/// <summary>
/// Handler for uploading user profile photo
/// </summary>
public class UploadProfilePhotoCommandHandler : IRequestHandler<UploadProfilePhotoCommand, string>
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<UploadProfilePhotoCommandHandler> _logger;

    public UploadProfilePhotoCommandHandler(
        IFileUploadService fileUploadService,
        ILogger<UploadProfilePhotoCommandHandler> logger)
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    public async Task<string> Handle(UploadProfilePhotoCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing upload profile photo command for user: {UserId}", request.UserId);

            if (request.File == null || request.File.Length == 0)
            {
                throw new ArgumentException("No file provided");
            }

            if (!_fileUploadService.IsValidImageFile(request.File))
            {
                throw new ArgumentException("Invalid image file. Please upload a valid image (JPG, PNG, GIF) under 5MB.");
            }

            var filePath = await _fileUploadService.UploadProfilePhotoAsync(request.UserId, request.File);

            _logger.LogInformation("Profile photo uploaded successfully for user: {UserId}", request.UserId);

            return _fileUploadService.GetProfilePhotoUrl(filePath) ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile photo for user: {UserId}", request.UserId);
            throw;
        }
    }
}
