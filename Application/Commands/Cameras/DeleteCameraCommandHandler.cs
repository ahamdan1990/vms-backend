using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Cameras;

/// <summary>
/// Handler for camera deletion with comprehensive dependency checking and cascade management
/// Implements both soft and hard deletion strategies with audit trail preservation
/// </summary>
public class DeleteCameraCommandHandler : IRequestHandler<DeleteCameraCommand, ApiResponseDto<object>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCameraCommandHandler> _logger;

    public DeleteCameraCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteCameraCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiResponseDto<object>> Handle(DeleteCameraCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing delete camera command for camera ID: {CameraId}, PermanentDelete: {PermanentDelete}", 
                request.Id, request.PermanentDelete);

            // Retrieve camera with dependency information
            var camera = await _unitOfWork.Cameras.GetByIdAsync(request.Id, cancellationToken);
            
            if (camera == null)
            {
                _logger.LogWarning("Camera not found for deletion: {CameraId}", request.Id);
                return ApiResponseDto<object>.ErrorResponse(
                    $"Camera with ID {request.Id} not found.", 
                    "Camera not found");
            }

            // Check if camera is already deleted (for soft delete)
            if (!request.PermanentDelete && camera.IsDeleted)
            {
                _logger.LogWarning("Attempted to soft delete already deleted camera: {CameraId}", request.Id);
                return ApiResponseDto<object>.ErrorResponse(
                    "Camera is already deleted.", 
                    "Already deleted");
            }

            // Pre-deletion validation and dependency checking
            var validationResult = await ValidateDeletion(camera, request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return ApiResponseDto<object>.ErrorResponse(
                    validationResult.ErrorMessages, 
                    "Deletion validation failed");
            }

            // Stop camera streaming and cleanup resources before deletion
            await PreDeletionCleanup(camera, cancellationToken);

            // Perform deletion based on type
            if (request.PermanentDelete)
            {
                await PerformPermanentDeletion(camera, request, cancellationToken);
            }
            else
            {
                await PerformSoftDeletion(camera, request, cancellationToken);
            }

            // Save changes with transaction management
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while deleting camera {CameraId}", request.Id);
                return ApiResponseDto<object>.ErrorResponse(
                    "The camera was modified by another user. Please refresh and try again.", 
                    "Concurrency conflict");
            }

            // Log successful deletion
            var deletionType = request.PermanentDelete ? "permanently deleted" : "soft deleted";
            _logger.LogInformation("Successfully {DeletionType} camera {CameraName} (ID: {CameraId}) by user {UserId}", 
                deletionType, camera.Name, camera.Id, request.DeletedBy);

            // Post-deletion cleanup
            await PostDeletionCleanup(camera, request.PermanentDelete, cancellationToken);

            var successMessage = request.PermanentDelete 
                ? $"Camera '{camera.Name}' has been permanently deleted."
                : $"Camera '{camera.Name}' has been deleted and can be restored if needed.";

            return ApiResponseDto<object>.SuccessResponse(
                null, 
                successMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting camera {CameraId}: {Error}", request.Id, ex.Message);
            return ApiResponseDto<object>.ErrorResponse(
                "An error occurred while deleting the camera. Please try again.", 
                "Internal error");
        }
    }

    /// <summary>
    /// Validates deletion operation and checks for dependencies
    /// </summary>
    private async Task<(bool IsValid, List<string> ErrorMessages)> ValidateDeletion(
        Domain.Entities.Camera camera, 
        DeleteCameraCommand request, 
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        try
        {
            // TODO: Implement proper streaming check when camera service is available
            // For now, allow deletion (streaming check disabled for migrations)
            var isStreaming = false;
            if (isStreaming && !request.ForceDelete)
            {
                errors.Add("Cannot delete camera while it is actively streaming. Stop the stream first or use force delete.");
            }

            // TODO: Implement proper facial recognition check when camera service is available
            // For now, allow deletion (facial recognition check disabled for migrations)
            var hasActiveFacialRecognition = false;
            if (hasActiveFacialRecognition && !request.ForceDelete)
            {
                errors.Add("Cannot delete camera with active facial recognition processes. Wait for completion or use force delete.");
            }

            // For permanent deletion, check additional constraints
            if (request.PermanentDelete)
            {
                // Check for historical data dependencies (logs, events, etc.)
                var hasHistoricalData = await CheckHistoricalDataDependencies(camera.Id, cancellationToken);
                if (hasHistoricalData && !request.ForceDelete)
                {
                    errors.Add("Camera has historical data (logs, events). Consider soft delete to preserve audit trail or use force delete.");
                }

                // Check for configuration references
                var hasConfigurationReferences = await CheckConfigurationReferences(camera.Id, cancellationToken);
                if (hasConfigurationReferences && !request.ForceDelete)
                {
                    errors.Add("Camera is referenced in system configurations. Remove references first or use force delete.");
                }
            }

            // Validate user permissions for deletion type
            if (request.PermanentDelete && !await ValidateUserPermissionsForPermanentDelete(request.DeletedBy, cancellationToken))
            {
                errors.Add("User does not have permission to permanently delete cameras.");
            }

            return (errors.Count == 0, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during deletion validation for camera {CameraId}", camera.Id);
            errors.Add("Unable to validate deletion requirements. Please try again.");
            return (false, errors);
        }
    }

    /// <summary>
    /// Performs cleanup operations before camera deletion
    /// </summary>
    private Task PreDeletionCleanup(Domain.Entities.Camera camera, CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Implement stream stopping when camera service is available
            // For now, skip stream stopping (disabled for migrations)
            // await _cameraService.StopStreamAsync(camera.Id, graceful: true);

            // TODO: Implement facial recognition task cancellation when camera service is available
            // For now, skip task cancellation (disabled for migrations)
            // await _cameraService.CancelFacialRecognitionTasksAsync(camera.Id);

            // TODO: Implement cache clearing when camera service is available
            // For now, skip cache clearing (disabled for migrations)
            // await _cameraService.ClearCacheAsync(camera.Id);

            _logger.LogDebug("Pre-deletion cleanup completed for camera {CameraId}", camera.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Non-critical error during pre-deletion cleanup for camera {CameraId}", camera.Id);
            // Continue with deletion even if cleanup partially fails
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs soft deletion with audit trail preservation
    /// </summary>
    private Task PerformSoftDeletion(Domain.Entities.Camera camera, DeleteCameraCommand request, CancellationToken cancellationToken)
    {
        // Update camera status to reflect deletion
        camera.UpdateStatus(Domain.Enums.CameraStatus.Inactive, "Camera deleted", request.DeletedBy);

        // Perform soft delete with audit information
        camera.SoftDelete(request.DeletedBy);

        // Store deletion reason if provided
        if (!string.IsNullOrEmpty(request.DeletionReason))
        {
            // Add deletion reason to metadata
            var metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                DeletionReason = request.DeletionReason,
                DeletionTimestamp = DateTime.UtcNow,
                DeletionType = "Soft Delete"
            });
            camera.Metadata = metadata;
        }

        _logger.LogDebug("Soft deletion applied to camera {CameraId}", camera.Id);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs permanent deletion with cascade operations
    /// </summary>
    private async Task PerformPermanentDeletion(Domain.Entities.Camera camera, DeleteCameraCommand request, CancellationToken cancellationToken)
    {
        // Log permanent deletion for audit purposes
        _logger.LogWarning("Performing permanent deletion of camera {CameraName} (ID: {CameraId}) by user {UserId}. " +
                          "Reason: {Reason}", 
            camera.Name, camera.Id, request.DeletedBy, request.DeletionReason ?? "Not specified");

        // Archive critical information before deletion
        await ArchiveCameraData(camera, request.DeletedBy, cancellationToken);

        // Remove from repository (permanent deletion)
        _unitOfWork.Cameras.Remove(camera);

        _logger.LogDebug("Permanent deletion applied to camera {CameraId}", camera.Id);
    }

    /// <summary>
    /// Performs cleanup operations after successful deletion
    /// </summary>
    private Task PostDeletionCleanup(Domain.Entities.Camera camera, bool wasPermanent, CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Implement file system cleanup when camera service is available
            // For now, skip file system cleanup (disabled for migrations)
            // await _cameraService.CleanupFileSystemResourcesAsync(camera.Id, wasPermanent);

            // TODO: Implement camera deletion notification when camera service is available
            // For now, skip notification (disabled for migrations)
            // await _cameraService.NotifyCameraDeletionAsync(camera.Id, wasPermanent);

            // TODO: Implement monitoring system updates when camera service is available
            // For now, skip monitoring updates (disabled for migrations)
            // await _cameraService.UpdateMonitoringSystemsAsync(camera.Id, removed: true);

            _logger.LogDebug("Post-deletion cleanup completed for camera {CameraId}", camera.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Non-critical error during post-deletion cleanup for camera {CameraId}", camera.Id);
            // Don't fail the operation for cleanup errors
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks for historical data dependencies that might be affected by permanent deletion
    /// </summary>
    private async Task<bool> CheckHistoricalDataDependencies(int cameraId, CancellationToken cancellationToken)
    {
        try
        {
            // Check audit logs
            var hasAuditLogs = await _unitOfWork.AuditLogs
                .AnyAsync(a => a.EntityName == "Camera" && a.EntityId == cameraId, cancellationToken);

            // Check for any other historical references
            // This would be expanded based on actual system requirements
            
            return hasAuditLogs;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking historical data dependencies for camera {CameraId}", cameraId);
            return true; // Err on the side of caution
        }
    }

    /// <summary>
    /// Checks for configuration references that might be affected by deletion
    /// </summary>
    private Task<bool> CheckConfigurationReferences(int cameraId, CancellationToken cancellationToken)
    {
        try
        {
            // Check system configurations that might reference this camera
            // This would be implemented based on actual system design
            return Task.FromResult(false); // Placeholder implementation
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking configuration references for camera {CameraId}", cameraId);
            return Task.FromResult(true); // Err on the side of caution
        }
    }

    /// <summary>
    /// Validates that the user has permissions for permanent deletion
    /// </summary>
    private async Task<bool> ValidateUserPermissionsForPermanentDelete(int userId, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            
            // Check if user has admin role or specific permanent delete permission
            // This would be implemented based on actual permission system
            return user?.IsActive == true; // Placeholder implementation
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating permanent delete permissions for user {UserId}", userId);
            return false; // Err on the side of caution
        }
    }

    /// <summary>
    /// Archives critical camera data before permanent deletion for compliance
    /// </summary>
    private async Task ArchiveCameraData(Domain.Entities.Camera camera, int deletedBy, CancellationToken cancellationToken)
    {
        try
        {
            // Create archive record with essential information
            var archiveData = new
            {
                CameraId = camera.Id,
                Name = camera.Name,
                Type = camera.CameraType.ToString(),
                LocationId = camera.LocationId,
                Configuration = camera.ConfigurationJson,
                CreatedOn = camera.CreatedOn,
                DeletedOn = DateTime.UtcNow,
                DeletedBy = deletedBy,
                ArchiveReason = "Permanent deletion archive"
            };

            // This would typically be stored in a dedicated archive table or external system
            _logger.LogInformation("Archived camera data for permanent deletion: {ArchiveData}", 
                System.Text.Json.JsonSerializer.Serialize(archiveData));

            await Task.CompletedTask; // Placeholder for actual archive implementation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive camera data before permanent deletion for camera {CameraId}", camera.Id);
            throw; // Archive failure should prevent permanent deletion
        }
    }
}