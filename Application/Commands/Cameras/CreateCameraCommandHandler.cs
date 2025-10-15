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
/// Handler for creating a new camera with comprehensive validation and configuration
/// Implements security best practices for credential handling and connection validation
/// </summary>
public class CreateCameraCommandHandler : IRequestHandler<CreateCameraCommand, CameraDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateCameraCommandHandler> _logger;

    public CreateCameraCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateCameraCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CameraDto> Handle(CreateCameraCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing create camera command for camera: {CameraName}", request.Name);

            // Validate camera name uniqueness within location (if specified)
            await ValidateCameraUniqueness(request, cancellationToken);

            // Validate location exists if specified
            if (request.LocationId.HasValue)
            {
                await ValidateLocationExists(request.LocationId.Value, cancellationToken);
            }

            // Create camera configuration
            var configuration = CreateCameraConfiguration(request.Configuration);

            // Create camera entity with secure credential handling
            var camera = new Camera
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                CameraType = request.CameraType,
                ConnectionString = request.ConnectionString.Trim(),
                Username = request.Username?.Trim(),
                Password = await EncryptPassword(request.Password), // Encrypt sensitive data
                LocationId = request.LocationId,
                Status = CameraStatus.Inactive, // Start in inactive state
                EnableFacialRecognition = request.EnableFacialRecognition,
                Priority = request.Priority,
                Manufacturer = request.Manufacturer?.Trim(),
                Model = request.Model?.Trim(),
                FirmwareVersion = request.FirmwareVersion?.Trim(),
                SerialNumber = request.SerialNumber?.Trim(),
                Metadata = request.Metadata,
                ConfigurationJson = configuration.ToJsonString()
            };

            // Set audit information
            camera.SetCreatedBy(request.CreatedBy);

            // Validate camera configuration
            if (!camera.IsConfigurationValid(out var validationErrors))
            {
                var errorMessage = string.Join("; ", validationErrors);
                _logger.LogWarning("Camera configuration validation failed for {CameraName}: {Errors}",
                    request.Name, errorMessage);
                throw new InvalidOperationException($"Camera configuration is invalid: {errorMessage}");
            }

            // Add to repository
            await _unitOfWork.Cameras.AddAsync(camera, cancellationToken);

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created camera {CameraName} with ID {CameraId}",
                camera.Name, camera.Id);

            // Note: Connection testing will be handled by dedicated worker services
            // as part of the camera management infrastructure
            
            // Map to DTO and return
            var cameraDto = _mapper.Map<CameraDto>(camera);
            
            // Enhance DTO with additional information
            await EnhanceCameraDto(cameraDto, camera, cancellationToken);

            return cameraDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating camera {CameraName}: {Error}",
                request.Name, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Validates that the camera name is unique within the specified location
    /// </summary>
    private async Task ValidateCameraUniqueness(CreateCameraCommand request, CancellationToken cancellationToken)
    {
        var existingCamera = await _unitOfWork.Cameras
            .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Name.ToLower().Trim() &&
                           c.LocationId == request.LocationId &&
                           !c.IsDeleted,
                      cancellationToken);

        if (existingCamera != null)
        {
            var locationInfo = request.LocationId.HasValue ? $" in location {request.LocationId}" : "";
            var errorMessage = $"A camera with name '{request.Name}'{locationInfo} already exists.";
            
            _logger.LogWarning("Camera name uniqueness validation failed: {Error}", errorMessage);
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
            _logger.LogWarning("Invalid location specified for camera: {LocationId}", locationId);
            throw new InvalidOperationException($"Location with ID {locationId} does not exist or is inactive.");
        }
    }

    /// <summary>
    /// Creates camera configuration with defaults for missing values
    /// </summary>
    private static CameraConfiguration CreateCameraConfiguration(CameraConfigurationDto? configDto)
    {
        if (configDto == null)
            return CameraConfiguration.Default;

        return new CameraConfiguration
        {
            ResolutionWidth = configDto.ResolutionWidth ?? CameraConfiguration.Default.ResolutionWidth,
            ResolutionHeight = configDto.ResolutionHeight ?? CameraConfiguration.Default.ResolutionHeight,
            FrameRate = configDto.FrameRate ?? CameraConfiguration.Default.FrameRate,
            Quality = configDto.Quality ?? CameraConfiguration.Default.Quality,
            AutoStart = configDto.AutoStart,
            MaxConnections = configDto.MaxConnections > 0 ? configDto.MaxConnections : CameraConfiguration.Default.MaxConnections,
            ConnectionTimeoutSeconds = configDto.ConnectionTimeoutSeconds > 0 ? configDto.ConnectionTimeoutSeconds : CameraConfiguration.Default.ConnectionTimeoutSeconds,
            RetryIntervalSeconds = configDto.RetryIntervalSeconds > 0 ? configDto.RetryIntervalSeconds : CameraConfiguration.Default.RetryIntervalSeconds,
            MaxRetryAttempts = configDto.MaxRetryAttempts > 0 ? configDto.MaxRetryAttempts : CameraConfiguration.Default.MaxRetryAttempts,
            EnableMotionDetection = configDto.EnableMotionDetection,
            MotionSensitivity = configDto.MotionSensitivity,
            EnableRecording = configDto.EnableRecording,
            RecordingDurationMinutes = configDto.RecordingDurationMinutes,
            EnableFacialRecognition = configDto.EnableFacialRecognition,
            FacialRecognitionThreshold = configDto.FacialRecognitionThreshold ?? CameraConfiguration.Default.FacialRecognitionThreshold,
            ExtendedConfiguration = configDto.ExtendedConfiguration
        };
    }

    /// <summary>
    /// Encrypts password for secure storage (placeholder for actual encryption)
    /// In production, use proper encryption/hashing mechanisms
    /// </summary>
    private async Task<string?> EncryptPassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return null;

        // TODO: Implement proper encryption using IDataProtector or similar
        // This is a placeholder - in production, use proper encryption
        return await Task.FromResult(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password)));
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
    }
}