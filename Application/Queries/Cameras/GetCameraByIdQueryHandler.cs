using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Cameras;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Cameras;

/// <summary>
/// Handler for retrieving a single camera by ID with comprehensive data enrichment
/// Implements security controls for sensitive information access
/// </summary>
public class GetCameraByIdQueryHandler : IRequestHandler<GetCameraByIdQuery, CameraDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCameraByIdQueryHandler> _logger;

    public GetCameraByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetCameraByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CameraDto?> Handle(GetCameraByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Retrieving camera by ID: {CameraId}, IncludeDeleted: {IncludeDeleted}",
                request.Id, request.IncludeDeleted);

            // Build query with includes for related data
            var query = _unitOfWork.Cameras.GetQueryable()
                .Include(c => c.Location)
                .Include(c => c.CreatedByUser)
                .Include(c => c.ModifiedByUser)
                .AsQueryable();

            // Apply deletion filter
            if (!request.IncludeDeleted)
            {
                query = query.Where(c => !c.IsDeleted);
            }

            // Get camera by ID
            var camera = await query.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (camera == null)
            {
                _logger.LogDebug("Camera not found: {CameraId}", request.Id);
                return null;
            }

            // Map to DTO
            var cameraDto = _mapper.Map<CameraDto>(camera);

            // Enhance with computed properties
            await EnhanceCameraDto(cameraDto, camera, request.IncludeSensitiveData, cancellationToken);

            _logger.LogDebug("Successfully retrieved camera: {CameraName} (ID: {CameraId})",
                camera.Name, camera.Id);

            return cameraDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving camera by ID {CameraId}: {Error}",
                request.Id, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Enhances camera DTO with computed properties and related data
    /// </summary>
    private async Task EnhanceCameraDto(CameraDto cameraDto, Domain.Entities.Camera camera,
        bool includeSensitiveData, CancellationToken cancellationToken)
    {
        // Set display values
        cameraDto.CameraTypeDisplay = camera.CameraType.ToString();
        cameraDto.StatusDisplay = GetStatusDisplayWithContext(camera);
        cameraDto.DisplayName = camera.GetDisplayName();
        cameraDto.IsOperational = camera.IsOperational();
        cameraDto.IsAvailableForStreaming = camera.IsAvailableForStreaming();

        // Set location information
        if (camera.Location != null)
        {
            cameraDto.LocationName = camera.Location.Name;
        }

        // Set user information
        if (camera.CreatedByUser != null)
        {
            cameraDto.CreatedByName = $"{camera.CreatedByUser.FirstName} {camera.CreatedByUser.LastName}".Trim();
        }

        if (camera.ModifiedByUser != null)
        {
            cameraDto.ModifiedByName = $"{camera.ModifiedByUser.FirstName} {camera.ModifiedByUser.LastName}".Trim();
        }

        // Parse and map configuration
        var config = camera.GetConfiguration();
        cameraDto.Configuration = _mapper.Map<CameraConfigurationDto>(config);
        
        if (cameraDto.Configuration != null)
        {
            cameraDto.Configuration.ResolutionDisplay = config.GetResolutionString();
        }

        // Handle sensitive data based on permissions
        if (includeSensitiveData)
        {
            // Include actual connection string and credentials (for admin use)
            cameraDto.ConnectionString = camera.ConnectionString;
            cameraDto.Username = camera.Username;
            // Note: Never expose actual passwords, even for admins
        }
        else
        {
            // Use safe connection string with masked credentials
            cameraDto.ConnectionString = camera.GetSafeConnectionString();
            cameraDto.Username = !string.IsNullOrEmpty(camera.Username) ? "***" : null;
        }

        // Calculate time-based fields
        var timeSinceHealthCheck = camera.TimeSinceLastHealthCheck();
        cameraDto.MinutesSinceLastHealthCheck = timeSinceHealthCheck?.TotalMinutes > 0 
            ? (int)timeSinceHealthCheck.Value.TotalMinutes 
            : null;

        var timeSinceOnline = camera.TimeSinceLastOnline();
        cameraDto.MinutesSinceLastOnline = timeSinceOnline?.TotalMinutes > 0 
            ? (int)timeSinceOnline.Value.TotalMinutes 
            : null;

        await Task.CompletedTask; // For future async enhancements
    }

    /// <summary>
    /// Gets status display text with contextual information
    /// </summary>
    private static string GetStatusDisplayWithContext(Domain.Entities.Camera camera)
    {
        var baseStatus = camera.Status.ToString();

        return camera.Status switch
        {
            Domain.Enums.CameraStatus.Active => "Active",
            Domain.Enums.CameraStatus.Inactive => "Inactive",
            Domain.Enums.CameraStatus.Connecting => "Connecting...",
            Domain.Enums.CameraStatus.Error => $"Error ({camera.FailureCount} failures)",
            Domain.Enums.CameraStatus.Disconnected => "Disconnected",
            Domain.Enums.CameraStatus.Maintenance => "Under Maintenance",
            _ => baseStatus
        };
    }
}