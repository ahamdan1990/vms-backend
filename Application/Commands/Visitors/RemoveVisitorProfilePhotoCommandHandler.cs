using MediatR;
using VisitorManagementSystem.Api.Application.Services.FaceDetection;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Handler for removing visitor profile photo
/// </summary>
public class RemoveVisitorProfilePhotoCommandHandler : IRequestHandler<RemoveVisitorProfilePhotoCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;
    private readonly IFaceDetectionService _faceDetectionService;
    private readonly ILogger<RemoveVisitorProfilePhotoCommandHandler> _logger;

    public RemoveVisitorProfilePhotoCommandHandler(
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment,
        IFaceDetectionService faceDetectionService,
        ILogger<RemoveVisitorProfilePhotoCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _environment = environment;
        _faceDetectionService = faceDetectionService;
        _logger = logger;
    }

    public async Task Handle(RemoveVisitorProfilePhotoCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing remove profile photo command for visitor: {VisitorId}", request.VisitorId);

            // Get visitor
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(request.VisitorId, cancellationToken);
            if (visitor == null)
            {
                throw new InvalidOperationException($"Visitor with ID {request.VisitorId} not found");
            }

            // Create subject ID using visitor's email for CompreFace (guaranteed unique)
            // Fall back to name if email is not available
            string nameId = $"{visitor.FirstName}_{visitor.LastName}".Replace(" ", "_");
            var subjectId = !string.IsNullOrEmpty(visitor.Email)
                ? ((string)visitor.Email).ToLower(System.Globalization.CultureInfo.InvariantCulture)
                : nameId.ToLower(System.Globalization.CultureInfo.InvariantCulture);

            // Check if visitor has a photo
            if (string.IsNullOrEmpty(visitor.ProfilePhotoPath))
            {
                _logger.LogWarning("Visitor {VisitorId} ({VisitorName}) has no profile photo to remove",
                    request.VisitorId, subjectId);
                return;
            }

            // Store photo path for deletion
            var photoPath = visitor.ProfilePhotoPath;

            // Remove photo path from visitor
            visitor.ProfilePhotoPath = null;
            visitor.UpdateModifiedBy(request.ModifiedBy);

            // Update in repository
            _unitOfWork.Visitors.Update(visitor);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Profile photo path removed from visitor {VisitorId} ({VisitorName})",
                request.VisitorId, subjectId);

            // Try to delete physical file
            await RemovePhotoFile(photoPath);

            // Remove face from CompreFace recognition collection using visitor's name
            try
            {
                var removed = await _faceDetectionService.RemoveFaceFromCollectionAsync(subjectId, cancellationToken);

                if (removed)
                {
                    _logger.LogInformation("Face removed from recognition collection for visitor {VisitorId} ({VisitorName})",
                        request.VisitorId, subjectId);
                }
                else
                {
                    _logger.LogWarning("Failed to remove face from recognition collection for visitor {VisitorId} ({VisitorName})",
                        request.VisitorId, subjectId);
                }
            }
            catch (Exception ex)
            {
                // Don't fail the entire operation if CompreFace is unavailable
                _logger.LogError(ex, "Error removing face from CompreFace collection for visitor {VisitorId} ({VisitorName})",
                    request.VisitorId, subjectId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing profile photo for visitor: {VisitorId}", request.VisitorId);
            throw;
        }
    }

    private async Task RemovePhotoFile(string photoPath)
    {
        try
        {
            var fullPath = Path.Combine(_environment.WebRootPath, photoPath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
                _logger.LogInformation("Removed photo file: {PhotoPath}", photoPath);
            }
            else
            {
                _logger.LogWarning("Photo file not found: {PhotoPath}", photoPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove photo file: {PhotoPath}", photoPath);
            // Don't throw - database update was successful
        }
    }
}
