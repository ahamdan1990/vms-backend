using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Cameras;
using VisitorManagementSystem.Api.Application.Services;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Application.Commands.Cameras;

/// <summary>
/// Handler for updating camera configuration with comprehensive validation and security
/// Implements optimistic concurrency control and secure credential management
/// </summary>
public class UpdateCameraCommandHandler : IRequestHandler<UpdateCameraCommand, CameraDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateCameraCommandHandler> _logger;

    public UpdateCameraCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateCameraCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CameraDto> Handle(UpdateCameraCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing update camera command for camera ID: {CameraId}", request.Id);

            // Retrieve existing camera with tracking for optimistic concurrency
            var camera = await _unitOfWork.Cameras.GetByIdAsync(request.Id, cancellationToken);
            
            if (camera == null)
            {
                _logger.LogWarning("Camera not found for update: {CameraId}", request.Id);
                throw new InvalidOperationException($"Camera with ID {request.Id} not found.");
            }

            if (camera.IsDeleted)
            {
                _logger.LogWarning("Attempted to update deleted camera: {CameraId}", request.Id);
                throw new InvalidOperationException($"Cannot update deleted camera {request.Id}.");
            }

            // Store original values for change tracking
            var originalName = camera.Name;
            var originalConnectionString = camera.ConnectionString;
            var originalCameraType = camera.CameraType;

            // Validate name uniqueness (exclude current camera)
            await ValidateCameraUniqueness(request, cancellationToken);

            // Validate location exists if specified
            if (request.LocationId.HasValue)
            {
                await ValidateLocationExists(request.LocationId.Value, cancellationToken);
            }

            // Update camera configuration
            var configuration = UpdateCameraConfiguration(camera.GetConfiguration(), request.Configuration);

            // Apply updates to camera entity
            await ApplyUpdatesToCamera(camera, request, configuration);

            // Validate updated configuration
            if (!camera.IsConfigurationValid(out var validationErrors))
            {
                var errorMessage = string.Join("; ", validationErrors);
                _logger.LogWarning("Updated camera configuration validation failed for {CameraId}: {Errors}",
                    request.Id, errorMessage);
                throw new InvalidOperationException($"Updated camera configuration is invalid: {errorMessage}");
            }

            // Save changes with optimistic concurrency handling
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while updating camera {CameraId}", request.Id);
                throw new InvalidOperationException("The camera was modified by another user. Please refresh and try again.");
            }

            _logger.LogInformation("Successfully updated camera {CameraName} (ID: {CameraId})", 
                camera.Name, camera.Id);

            // Log significant configuration changes
            LogConfigurationChanges(originalName, originalConnectionString, originalCameraType, camera);

            // Note: Connection testing will be handled by dedicated worker services
            // as part of the camera management infrastructure

            // Map to DTO and return
            var cameraDto = _mapper.Map<CameraDto>(camera);
            
            // Enhance DTO with additional information
            await EnhanceCameraDto(cameraDto, camera, cancellationToken);

            return cameraDto;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError(ex, "Error updating camera {CameraId}: {Error}", request.Id, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Validates camera name uniqueness within the location (excluding current camera)
    /// </summary>
    private async Task ValidateCameraUniqueness(UpdateCameraCommand request, CancellationToken cancellationToken)
    {
        var existingCamera = await _unitOfWork.Cameras
            .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Name.ToLower().Trim() &&
                           c.LocationId == request.LocationId &&
                           c.Id != request.Id &&
                           !c.IsDeleted,
                      cancellationToken);

        if (existingCamera != null)
        {
            var locationInfo = request.LocationId.HasValue ? $" in location {request.LocationId}" : "";
            var errorMessage = $"Another camera with name '{request.Name}'{locationInfo} already exists.";
            
            _logger.LogWarning("Camera name uniqueness validation failed during update: {Error}", errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
    }

    /// <summary>
    /// Validates that the specified location exists and is active
    /// </summary>
    private async Task ValidateLocationExists(int locationId, CancellationToken cancellationToken)
    {
        var location = await _unitOfWork.Locations.GetByIdAsync(locationId, cancellationToken);
        
        if (location == null || !location.IsActive)
        {
            _logger.LogWarning("Invalid location specified for camera update: {LocationId}", locationId);
            throw new InvalidOperationException($"Location with ID {locationId} does not exist or is inactive.");
        }
    }

    /// <summary>
    /// Updates camera configuration by merging existing values with new ones
    /// </summary>
    private static CameraConfiguration UpdateCameraConfiguration(
        CameraConfiguration existing, 
        CameraConfigurationDto? updates)
    {
        if (updates == null)
            return existing;

        // Merge configuration values, preserving existing where not specified
        return new CameraConfiguration
        {
            ResolutionWidth = updates.ResolutionWidth ?? existing.ResolutionWidth,
            ResolutionHeight = updates.ResolutionHeight ?? existing.ResolutionHeight,
            FrameRate = updates.FrameRate ?? existing.FrameRate,
            Quality = updates.Quality ?? existing.Quality,
            AutoStart = updates.AutoStart,
            MaxConnections = updates.MaxConnections > 0 ? updates.MaxConnections : existing.MaxConnections,
            ConnectionTimeoutSeconds = updates.ConnectionTimeoutSeconds > 0 ? updates.ConnectionTimeoutSeconds : existing.ConnectionTimeoutSeconds,
            RetryIntervalSeconds = updates.RetryIntervalSeconds > 0 ? updates.RetryIntervalSeconds : existing.RetryIntervalSeconds,
            MaxRetryAttempts = updates.MaxRetryAttempts > 0 ? updates.MaxRetryAttempts : existing.MaxRetryAttempts,
            EnableMotionDetection = updates.EnableMotionDetection,
            MotionSensitivity = updates.MotionSensitivity ?? existing.MotionSensitivity,
            EnableRecording = updates.EnableRecording,
            RecordingDurationMinutes = updates.RecordingDurationMinutes ?? existing.RecordingDurationMinutes,
            EnableFacialRecognition = updates.EnableFacialRecognition,
            FacialRecognitionThreshold = updates.FacialRecognitionThreshold ?? existing.FacialRecognitionThreshold,
            ExtendedConfiguration = updates.ExtendedConfiguration ?? existing.ExtendedConfiguration
        };
    }

    /// <summary>
    /// Applies all updates to the camera entity with secure credential handling
    /// </summary>
    private async Task ApplyUpdatesToCamera(Camera camera, UpdateCameraCommand request, CameraConfiguration configuration)
    {
        // Update basic properties
        camera.Name = request.Name.Trim();
        camera.Description = request.Description?.Trim();
        camera.CameraType = request.CameraType;
        camera.ConnectionString = request.ConnectionString.Trim();
        camera.Username = request.Username?.Trim();
        camera.LocationId = request.LocationId;
        camera.EnableFacialRecognition = request.EnableFacialRecognition;
        camera.Priority = request.Priority;
        camera.Manufacturer = request.Manufacturer?.Trim();
        camera.Model = request.Model?.Trim();
        camera.FirmwareVersion = request.FirmwareVersion?.Trim();
        camera.SerialNumber = request.SerialNumber?.Trim();
        camera.Metadata = request.Metadata;

        // Handle password updates securely
        if (request.Password != null)
        {
            if (string.IsNullOrEmpty(request.Password))
            {
                // Empty string means remove password
                camera.Password = null;
            }
            else
            {
                // Encrypt new password
                camera.Password = await EncryptPassword(request.Password);
            }
        }
        // If Password is null, keep existing password unchanged

        // Update configuration
        camera.SetConfiguration(configuration);

        // Update active status
        if (camera.IsActive != request.IsActive)
        {
            if (request.IsActive)
                camera.Activate(request.ModifiedBy);
            else
                camera.Deactivate(request.ModifiedBy);
        }
        else
        {
            // Update audit information
            camera.UpdateModifiedBy(request.ModifiedBy);
        }

        // Reset error state if configuration changed significantly
        if (HasCriticalChanges(camera.ConnectionString, camera.CameraType, camera))
        {
            camera.ResetErrorState(request.ModifiedBy);
        }
    }

    /// <summary>
    /// Encrypts password for secure storage (placeholder implementation)
    /// </summary>
    private async Task<string> EncryptPassword(string password)
    {
        // TODO: Implement proper encryption using IDataProtector or similar
        return await Task.FromResult(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password)));
    }

    /// <summary>
    /// Determines if critical connection settings have changed
    /// </summary>
    private static bool HasCriticalChanges(string originalConnectionString, CameraType originalType, Camera updatedCamera)
    {
        return originalConnectionString != updatedCamera.ConnectionString ||
               originalType != updatedCamera.CameraType;
    }

    /// <summary>
    /// Logs significant configuration changes for audit trail
    /// </summary>
    private void LogConfigurationChanges(string originalName, string originalConnectionString, 
        CameraType originalType, Camera updatedCamera)
    {
        var changes = new List<string>();

        if (originalName != updatedCamera.Name)
            changes.Add($"Name: '{originalName}' → '{updatedCamera.Name}'");

        if (originalConnectionString != updatedCamera.ConnectionString)
            changes.Add($"Connection: '{originalConnectionString}' → '{updatedCamera.GetSafeConnectionString()}'");

        if (originalType != updatedCamera.CameraType)
            changes.Add($"Type: {originalType} → {updatedCamera.CameraType}");

        if (changes.Count > 0)
        {
            _logger.LogInformation("Camera {CameraId} configuration changes: {Changes}", 
                updatedCamera.Id, string.Join("; ", changes));
        }
    }

    /// <summary>
    /// Enhances camera DTO with additional computed information
    /// </summary>
    private async Task EnhanceCameraDto(CameraDto cameraDto, Camera camera, CancellationToken cancellationToken)
    {
        // Set display values
        cameraDto.CameraTypeDisplay = camera.CameraType.ToString();
        cameraDto.StatusDisplay = camera.Status.ToString();
        cameraDto.DisplayName = camera.GetDisplayName();
        cameraDto.IsOperational = camera.IsOperational();
        cameraDto.IsAvailableForStreaming = camera.IsAvailableForStreaming();

        // Load location name if applicable
        if (camera.LocationId.HasValue)
        {
            var location = await _unitOfWork.Locations.GetByIdAsync(camera.LocationId.Value, cancellationToken);
            cameraDto.LocationName = location?.Name;
        }

        // Load modifier information
        if (camera.ModifiedBy.HasValue)
        {
            var modifier = await _unitOfWork.Users.GetByIdAsync(camera.ModifiedBy.Value, cancellationToken);
            cameraDto.ModifiedByName = modifier != null ? $"{modifier.FirstName} {modifier.LastName}" : "Unknown";
        }

        // Load creator information
        if (camera.CreatedBy.HasValue)
        {
            var creator = await _unitOfWork.Users.GetByIdAsync(camera.CreatedBy.Value, cancellationToken);
            cameraDto.CreatedByName = creator != null ? $"{creator.FirstName} {creator.LastName}" : "Unknown";
        }

        // Parse and map configuration
        var config = camera.GetConfiguration();
        cameraDto.Configuration = _mapper.Map<CameraConfigurationDto>(config);
        
        if (cameraDto.Configuration != null)
        {
            cameraDto.Configuration.ResolutionDisplay = config.GetResolutionString();
        }

        // Set safe connection string (credentials masked)
        cameraDto.ConnectionString = camera.GetSafeConnectionString();

        // Calculate time-based fields
        var timeSinceHealthCheck = camera.TimeSinceLastHealthCheck();
        cameraDto.MinutesSinceLastHealthCheck = timeSinceHealthCheck?.TotalMinutes > 0 
            ? (int)timeSinceHealthCheck.Value.TotalMinutes 
            : null;

        var timeSinceOnline = camera.TimeSinceLastOnline();
        cameraDto.MinutesSinceLastOnline = timeSinceOnline?.TotalMinutes > 0 
            ? (int)timeSinceOnline.Value.TotalMinutes 
            : null;
    }
}