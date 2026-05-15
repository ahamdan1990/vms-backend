using MediatR;
using VisitorManagementSystem.Api.Application.Services.FaceDetection;
using VisitorManagementSystem.Api.Application.Services.FileUploadService;

namespace VisitorManagementSystem.Api.Application.Commands.Users;

/// <summary>
/// Handler for uploading user profile photo
/// </summary>
public class UploadProfilePhotoCommandHandler : IRequestHandler<UploadProfilePhotoCommand, string>
{
    private readonly IFileUploadService _fileUploadService;
    private readonly IFaceDetectionService _faceDetectionService;
    private readonly ILogger<UploadProfilePhotoCommandHandler> _logger;

    public UploadProfilePhotoCommandHandler(
        IFileUploadService fileUploadService,
        IFaceDetectionService faceDetectionService,
        ILogger<UploadProfilePhotoCommandHandler> logger)
    {
        _fileUploadService = fileUploadService;
        _faceDetectionService = faceDetectionService;
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

            byte[] imageBytes;
            await using (var memoryStream = new MemoryStream())
            {
                await request.File.CopyToAsync(memoryStream, cancellationToken);
                imageBytes = memoryStream.ToArray();
            }

            var filePath = await _fileUploadService.UploadProfilePhotoAsync(request.UserId, request.File);
            await TryEnrollUserFaceAsync(request.UserId, imageBytes, cancellationToken);

            _logger.LogInformation("Profile photo uploaded successfully for user: {UserId}", request.UserId);

            return _fileUploadService.GetProfilePhotoUrl(filePath) ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile photo for user: {UserId}", request.UserId);
            throw;
        }
    }

    private async Task TryEnrollUserFaceAsync(
        int userId,
        byte[] imageBytes,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await _faceDetectionService.IsServiceAvailableAsync())
            {
                _logger.LogWarning("Profile photo saved for user {UserId}, but no face engine is available for enrollment.", userId);
                return;
            }

            var subjectId = $"user:{userId}";
            var result = await _faceDetectionService.AddFaceToCollectionAsync(
                imageBytes,
                subjectId,
                cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Profile photo saved for user {UserId}, but face enrollment failed: {Error}",
                    userId,
                    result.ErrorMessage);
                return;
            }

            await _faceDetectionService.TrimFacesToMaxAsync(subjectId, 6, cancellationToken);
            _logger.LogInformation(
                "Face enrolled for staff/user {UserId}, template/image id {ImageId}",
                userId,
                result.ImageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Profile photo saved for user {UserId}, but face enrollment failed.", userId);
        }
    }
}
