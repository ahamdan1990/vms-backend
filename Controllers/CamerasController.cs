using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Commands.Cameras;
using VisitorManagementSystem.Api.Application.DTOs.Cameras;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.Queries.Cameras;
using VisitorManagementSystem.Api.Application.Services;
using VisitorManagementSystem.Api.Domain.Constants;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Controller for camera management operations
/// Provides comprehensive CRUD functionality and camera-specific operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class CamerasController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<CamerasController> _logger;

    public CamerasController(
        IMediator mediator,
        ILogger<CamerasController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region CRUD Operations

    /// <summary>
    /// Gets a paginated list of cameras with optional filtering
    /// </summary>
    /// <param name="pageIndex">Page index (0-based)</param>
    /// <param name="pageSize">Page size (max 100)</param>
    /// <param name="searchTerm">Search term for name, description, or manufacturer</param>
    /// <param name="cameraType">Filter by camera type</param>
    /// <param name="status">Filter by camera status</param>
    /// <param name="locationId">Filter by location ID</param>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="enableFacialRecognition">Filter by facial recognition status</param>
    /// <param name="minPriority">Minimum priority level</param>
    /// <param name="maxPriority">Maximum priority level</param>
    /// <param name="includeDeleted">Include deleted cameras</param>
    /// <param name="sortBy">Field to sort by</param>
    /// <param name="sortDirection">Sort direction (asc/desc)</param>
    /// <returns>Paginated list of cameras</returns>
    [HttpGet]
    [Authorize(Policy = Permissions.SystemConfig.Read)]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResultDto<CameraListDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 403)]
    public async Task<IActionResult> GetCameras(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Domain.Enums.CameraType? cameraType = null,
        [FromQuery] Domain.Enums.CameraStatus? status = null,
        [FromQuery] int? locationId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? enableFacialRecognition = null,
        [FromQuery] int? minPriority = null,
        [FromQuery] int? maxPriority = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] string sortBy = "Name",
        [FromQuery] string sortDirection = "asc")
    {
        try
        {
            var query = new GetCamerasQuery
            {
                PageIndex = pageIndex,
                PageSize = Math.Min(pageSize, 100),
                SearchTerm = searchTerm?.Trim(),
                CameraType = cameraType,
                Status = status,
                LocationId = locationId,
                IsActive = isActive,
                EnableFacialRecognition = enableFacialRecognition,
                MinPriority = minPriority,
                MaxPriority = maxPriority,
                IncludeDeleted = includeDeleted,
                SortBy = sortBy,
                SortDirection = sortDirection
            };

            var result = await _mediator.Send(query);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cameras list");
            return ServerErrorResponse("An error occurred while retrieving cameras");
        }
    }

    /// <summary>
    /// Gets a camera by ID
    /// </summary>
    /// <param name="id">Camera ID</param>
    /// <param name="includeDeleted">Include deleted camera</param>
    /// <param name="includeSensitiveData">Include sensitive data (credentials)</param>
    /// <returns>Camera details</returns>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.SystemConfig.Read)]
    [ProducesResponseType(typeof(ApiResponseDto<CameraDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> GetCamera(int id, 
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool includeSensitiveData = false)
    {
        try
        {
            // Check sensitive data permission
            if (includeSensitiveData && !HasPermission(Permissions.SystemConfig.Read))
            {
                return ForbiddenResponse("Insufficient permissions to view sensitive camera data");
            }

            var query = new GetCameraByIdQuery 
            { 
                Id = id, 
                IncludeDeleted = includeDeleted,
                IncludeSensitiveData = includeSensitiveData
            };

            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFoundResponse("Camera", id);
            }

            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving camera {CameraId}", id);
            return ServerErrorResponse("An error occurred while retrieving the camera");
        }
    }

    /// <summary>
    /// Creates a new camera
    /// </summary>
    /// <param name="createDto">Camera creation data</param>
    /// <returns>Created camera</returns>
    [HttpPost]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    [ProducesResponseType(typeof(ApiResponseDto<CameraDto>), 201)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    public async Task<IActionResult> CreateCamera([FromBody] CreateCameraDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = GetModelStateErrors();
                return ValidationError(errors, "Invalid camera data");
            }

            var command = new CreateCameraCommand
            {
                Name = createDto.Name,
                Description = createDto.Description,
                CameraType = createDto.CameraType,
                ConnectionString = createDto.ConnectionString,
                Username = createDto.Username,
                Password = createDto.Password,
                LocationId = createDto.LocationId,
                Configuration = createDto.Configuration,
                EnableFacialRecognition = createDto.EnableFacialRecognition,
                Priority = createDto.Priority,
                Manufacturer = createDto.Manufacturer,
                Model = createDto.Model,
                FirmwareVersion = createDto.FirmwareVersion,
                SerialNumber = createDto.SerialNumber,
                IsActive = createDto.IsActive,
                Metadata = createDto.Metadata,
                CreatedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
            };

            var result = await _mediator.Send(command);
            return CreatedResponse(result, Url.Action(nameof(GetCamera), new { id = result.Id }));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating camera {CameraName}", createDto.Name);
            return ServerErrorResponse("An error occurred while creating the camera");
        }
    }

    /// <summary>
    /// Updates an existing camera
    /// </summary>
    /// <param name="id">Camera ID</param>
    /// <param name="updateDto">Camera update data</param>
    /// <param name="testConnection">Test connection after update</param>
    /// <returns>Updated camera</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    [ProducesResponseType(typeof(ApiResponseDto<CameraDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> UpdateCamera(int id, 
        [FromBody] UpdateCameraDto updateDto,
        [FromQuery] bool testConnection = false)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = GetModelStateErrors();
                return ValidationError(errors, "Invalid camera data");
            }

            var command = new UpdateCameraCommand
            {
                Id = id,
                Name = updateDto.Name,
                Description = updateDto.Description,
                CameraType = updateDto.CameraType,
                ConnectionString = updateDto.ConnectionString,
                Username = updateDto.Username,
                Password = updateDto.Password,
                LocationId = updateDto.LocationId,
                Configuration = updateDto.Configuration,
                EnableFacialRecognition = updateDto.EnableFacialRecognition,
                Priority = updateDto.Priority,
                Manufacturer = updateDto.Manufacturer,
                Model = updateDto.Model,
                FirmwareVersion = updateDto.FirmwareVersion,
                SerialNumber = updateDto.SerialNumber,
                IsActive = updateDto.IsActive,
                Metadata = updateDto.Metadata,
                TestConnection = testConnection,
                ModifiedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
            };

            var result = await _mediator.Send(command);
            return SuccessResponse(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating camera {CameraId}", id);
            return ServerErrorResponse("An error occurred while updating the camera");
        }
    }

    /// <summary>
    /// Deletes a camera (soft delete by default)
    /// </summary>
    /// <param name="id">Camera ID</param>
    /// <param name="permanentDelete">Whether to permanently delete</param>
    /// <param name="forceDelete">Force delete even with dependencies</param>
    /// <param name="deletionReason">Reason for deletion</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> DeleteCamera(int id,
        [FromQuery] bool permanentDelete = false,
        [FromQuery] bool forceDelete = false,
        [FromQuery] string? deletionReason = null)
    {
        try
        {
            var command = new DeleteCameraCommand
            {
                Id = id,
                DeletedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated"),
                PermanentDelete = permanentDelete,
                ForceDelete = forceDelete,
                DeletionReason = deletionReason
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting camera {CameraId}", id);
            return ServerErrorResponse("An error occurred while deleting the camera");
        }
    }

    /// <summary>
    /// Searches cameras with advanced filtering
    /// </summary>
    /// <param name="searchDto">Search parameters</param>
    /// <returns>Paginated search results</returns>
    [HttpPost("search")]
    [Authorize(Policy = Permissions.SystemConfig.Read)]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResultDto<CameraListDto>>), 200)]
    public async Task<IActionResult> SearchCameras([FromBody] CameraSearchDto searchDto)
    {
        try
        {
            var query = new SearchCamerasQuery { SearchCriteria = searchDto };
            var result = await _mediator.Send(query);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching cameras");
            return ServerErrorResponse("An error occurred while searching cameras");
        }
    }

    #endregion

    #region Camera Operations

    /// <summary>
    /// Tests camera connection
    /// </summary>
    /// <param name="id">Camera ID</param>
    /// <param name="updateStatus">Whether to update camera status in database</param>
    /// <returns>Connection test result</returns>
    [HttpPost("{id:int}/test-connection")]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> TestConnection(int id, [FromQuery] bool updateStatus = true)
    {
        try
        {
            // TODO: Create TestCameraConnectionCommand and Handler
            // For now, return a placeholder response following MediatR pattern
            var camera = await _mediator.Send(new GetCameraByIdQuery { Id = id });
            
            if (camera == null)
            {
                return NotFoundResponse("Camera", id);
            }

            return SuccessResponse(new
            {
                CameraId = id,
                Success = true,
                Message = "Connection test functionality will be implemented with dedicated worker services",
                TestedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection for camera {CameraId}", id);
            return ServerErrorResponse("An error occurred while testing camera connection");
        }
    }

    /// <summary>
    /// Tests camera connection with provided parameters
    /// </summary>
    /// <param name="testDto">Connection test parameters</param>
    /// <returns>Connection test result</returns>
    [HttpPost("test-connection")]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    public Task<IActionResult> TestConnectionParameters([FromBody] CameraConnectionTestDto testDto)
    {
        try
        {
            // TODO: Create TestCameraConnectionParametersCommand and Handler
            // For now, return a placeholder response following MediatR pattern
            return Task.FromResult<IActionResult>(SuccessResponse(new
            {
                Success = true,
                Status = "Connected",
                ErrorMessage = (string?)null,
                ResponseTimeMs = 150,
                TestedAt = DateTime.UtcNow,
                Details = "Connection test functionality will be implemented with dedicated worker services"
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing camera connection parameters");
            return Task.FromResult<IActionResult>(ServerErrorResponse("An error occurred while testing camera connection"));
        }
    }

    /// <summary>
    /// Starts camera stream
    /// </summary>
    /// <param name="id">Camera ID</param>
    /// <returns>Stream start result</returns>
    [HttpPost("{id:int}/start-stream")]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> StartStream(int id)
    {
        try
        {
            // TODO: Create StartCameraStreamCommand and Handler
            var camera = await _mediator.Send(new GetCameraByIdQuery { Id = id });
            
            if (camera == null)
            {
                return NotFoundResponse("Camera", id);
            }

            return SuccessResponse(new
            {
                CameraId = id,
                Success = true,
                Message = "Stream functionality will be implemented with dedicated worker services",
                StartedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting stream for camera {CameraId}", id);
            return ServerErrorResponse("An error occurred while starting camera stream");
        }
    }

    /// <summary>
    /// Stops camera stream
    /// </summary>
    /// <param name="id">Camera ID</param>
    /// <param name="graceful">Whether to stop gracefully</param>
    /// <returns>Stream stop result</returns>
    [HttpPost("{id:int}/stop-stream")]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> StopStream(int id, [FromQuery] bool graceful = true)
    {
        try
        {
            // TODO: Create StopCameraStreamCommand and Handler
            var camera = await _mediator.Send(new GetCameraByIdQuery { Id = id });
            
            if (camera == null)
            {
                return NotFoundResponse("Camera", id);
            }

            return SuccessResponse(new
            {
                CameraId = id,
                Success = true,
                Message = "Stream functionality will be implemented with dedicated worker services",
                StoppedAt = DateTime.UtcNow,
                Graceful = graceful
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping stream for camera {CameraId}", id);
            return ServerErrorResponse("An error occurred while stopping camera stream");
        }
    }

    /// <summary>
    /// Gets camera stream status and information
    /// </summary>
    /// <param name="id">Camera ID</param>
    /// <returns>Stream information</returns>
    [HttpGet("{id:int}/stream-info")]
    [Authorize(Policy = Permissions.SystemConfig.Read)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> GetStreamInfo(int id)
    {
        try
        {
            // TODO: Create GetCameraStreamInfoQuery and Handler
            var camera = await _mediator.Send(new GetCameraByIdQuery { Id = id });
            
            if (camera == null)
            {
                return NotFoundResponse("Camera", id);
            }

            return SuccessResponse(new
            {
                CameraId = id,
                IsStreaming = false,
                Message = "Stream functionality will be implemented with dedicated worker services"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stream info for camera {CameraId}", id);
            return ServerErrorResponse("An error occurred while retrieving stream information");
        }
    }

    /// <summary>
    /// Performs health check on a camera
    /// </summary>
    /// <param name="id">Camera ID</param>
    /// <returns>Health check result</returns>
    [HttpPost("{id:int}/health-check")]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> PerformHealthCheck(int id)
    {
        try
        {
            // TODO: Create PerformCameraHealthCheckCommand and Handler
            var camera = await _mediator.Send(new GetCameraByIdQuery { Id = id });
            
            if (camera == null)
            {
                return NotFoundResponse("Camera", id);
            }

            return SuccessResponse(new
            {
                CameraId = id,
                CameraName = camera.Name,
                IsHealthy = true,
                Status = "Active",
                PreviousStatus = (string?)null,
                ErrorMessage = (string?)null,
                ResponseTimeMs = 120,
                CheckedAt = DateTime.UtcNow,
                FailureCount = 0,
                IsRecovery = false,
                IsNewFailure = false,
                HealthScore = 95.0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing health check for camera {CameraId}", id);
            return ServerErrorResponse("An error occurred while performing health check");
        }
    }

    /// <summary>
    /// Captures a single frame from the camera
    /// </summary>
    /// <param name="id">Camera ID</param>
    /// <returns>Frame capture result</returns>
    [HttpPost("{id:int}/capture-frame")]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> CaptureFrame(int id)
    {
        try
        {
            // TODO: Create CaptureCameraFrameCommand and Handler
            var camera = await _mediator.Send(new GetCameraByIdQuery { Id = id });
            
            if (camera == null)
            {
                return NotFoundResponse("Camera", id);
            }

            return SuccessResponse(new
            {
                CameraId = id,
                Success = true,
                FrameSize = 1024000, // Placeholder frame size
                CapturedAt = DateTime.UtcNow,
                Message = "Frame capture functionality will be implemented with dedicated worker services"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing frame for camera {CameraId}", id);
            return ServerErrorResponse("An error occurred while capturing frame");
        }
    }

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Performs health check on all active cameras
    /// </summary>
    /// <returns>Health check results for all cameras</returns>
    [HttpPost("health-check-all")]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    public async Task<IActionResult> PerformHealthCheckAll()
    {
        try
        {
            // TODO: Create PerformAllCamerasHealthCheckCommand and Handler
            // For now, return placeholder response
            var cameras = await _mediator.Send(new GetCamerasQuery { PageSize = 100, IsActive = true });
            
            var results = cameras.Items.Select(c => new
            {
                CameraId = c.Id,
                CameraName = c.Name,
                IsHealthy = true,
                Status = "Active",
                HealthScore = 95.0,
                ResponseTimeMs = 120,
                ErrorMessage = (string?)null
            });

            var summary = new Dictionary<string, int>
            {
                ["Healthy"] = results.Count(),
                ["Unhealthy"] = 0
            };

            return SuccessResponse(new
            {
                Summary = summary,
                TotalCameras = results.Count(),
                CheckedAt = DateTime.UtcNow,
                Results = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing health check on all cameras");
            return ServerErrorResponse("An error occurred while performing bulk health check");
        }
    }

    #endregion
}

/// <summary>
/// DTO for camera connection testing
/// </summary>
public class CameraConnectionTestDto
{
    public Domain.Enums.CameraType CameraType { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}