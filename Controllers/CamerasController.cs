using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Commands.Cameras;
using VisitorManagementSystem.Api.Application.DTOs.Cameras;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.FaceDetection;
using VisitorManagementSystem.Api.Application.Queries.Cameras;
using VisitorManagementSystem.Api.Application.Services;
using VisitorManagementSystem.Api.Application.Services.Cameras;
using VisitorManagementSystem.Api.Application.Services.FaceDetection;
using VisitorManagementSystem.Api.Application.Services.VideoProcessing;
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
    private readonly IFaceDetectionService _faceDetectionService;
    private readonly ICameraService _cameraService;
    private readonly ICameraFrameRecognitionService _cameraFrameRecognitionService;
    private readonly ICameraFaceEventService _cameraFaceEventService;
    private readonly ICameraStreamRuntimeService _cameraStreamRuntimeService;
    private readonly IFfmpegCapabilityService _ffmpegCapabilityService;
    private readonly IFaceDetectionService _luxandService;
    private readonly IFaceDetectionService _compreFaceService;
    private readonly IFaceTemplateEnrollmentService _faceTemplateEnrollmentService;

    public CamerasController(
        IMediator mediator,
        ILogger<CamerasController> logger,
        IFaceDetectionService faceDetectionService,
        ICameraService cameraService,
        ICameraFrameRecognitionService cameraFrameRecognitionService,
        ICameraFaceEventService cameraFaceEventService,
        ICameraStreamRuntimeService cameraStreamRuntimeService,
        IFfmpegCapabilityService ffmpegCapabilityService,
        [Microsoft.Extensions.DependencyInjection.FromKeyedServices("luxand")] IFaceDetectionService luxandService,
        [Microsoft.Extensions.DependencyInjection.FromKeyedServices("compreface")] IFaceDetectionService compreFaceService,
        IFaceTemplateEnrollmentService faceTemplateEnrollmentService)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _faceDetectionService = faceDetectionService ?? throw new ArgumentNullException(nameof(faceDetectionService));
        _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
        _cameraFrameRecognitionService = cameraFrameRecognitionService ?? throw new ArgumentNullException(nameof(cameraFrameRecognitionService));
        _cameraFaceEventService = cameraFaceEventService ?? throw new ArgumentNullException(nameof(cameraFaceEventService));
        _cameraStreamRuntimeService = cameraStreamRuntimeService ?? throw new ArgumentNullException(nameof(cameraStreamRuntimeService));
        _ffmpegCapabilityService = ffmpegCapabilityService ?? throw new ArgumentNullException(nameof(ffmpegCapabilityService));
        _luxandService = luxandService ?? throw new ArgumentNullException(nameof(luxandService));
        _compreFaceService = compreFaceService ?? throw new ArgumentNullException(nameof(compreFaceService));
        _faceTemplateEnrollmentService = faceTemplateEnrollmentService ?? throw new ArgumentNullException(nameof(faceTemplateEnrollmentService));
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
            var camera = await _mediator.Send(new GetCameraByIdQuery { Id = id });
            
            if (camera == null)
            {
                return NotFoundResponse("Camera", id);
            }

            var result = await _cameraService.TestConnectionDetailsAsync(id, updateStatus, HttpContext.RequestAborted);
            return SuccessResponse(new
            {
                CameraId = id,
                Success = result.IsSuccess,
                result.IsSuccess,
                Status = result.Status,
                result.ErrorMessage,
                result.ResponseTimeMs,
                result.TestedAt,
                result.Details
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
    public async Task<IActionResult> TestConnectionParameters([FromBody] CameraConnectionTestDto testDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return ValidationError(GetModelStateErrors(), "Invalid camera connection test data");
            }

            var result = await _cameraService.TestConnectionAsync(
                testDto.CameraType,
                testDto.ConnectionString,
                testDto.Username,
                testDto.Password,
                testDto.TimeoutSeconds,
                HttpContext.RequestAborted);

            return SuccessResponse(new
            {
                Success = result.IsSuccess,
                result.IsSuccess,
                Status = result.Status,
                result.ErrorMessage,
                result.ResponseTimeMs,
                result.TestedAt,
                result.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing camera connection parameters");
            return ServerErrorResponse("An error occurred while testing camera connection");
        }
    }

    /// <summary>
    /// Gets FFmpeg availability and hardware acceleration capabilities.
    /// </summary>
    /// <returns>FFmpeg capability information</returns>
    [HttpGet("ffmpeg/capabilities")]
    [Authorize(Policy = Permissions.SystemConfig.Read)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    public async Task<IActionResult> GetFfmpegCapabilities()
    {
        try
        {
            var capabilities = await _ffmpegCapabilityService.GetCapabilitiesAsync(HttpContext.RequestAborted);
            return SuccessResponse(new
            {
                capabilities.IsFfmpegAvailable,
                capabilities.FfmpegPath,
                capabilities.HardwareAccelerations,
                capabilities.HasCuda,
                capabilities.ErrorMessage,
                capabilities.CheckedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving FFmpeg capabilities");
            return ServerErrorResponse("An error occurred while retrieving FFmpeg capabilities");
        }
    }

    /// <summary>
    /// Gets current local/fallback face engine availability and initialization diagnostics.
    /// </summary>
    /// <returns>Face engine status information</returns>
    [HttpGet("face-engines/status")]
    [Authorize(Policy = Permissions.SystemConfig.Read)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    public async Task<IActionResult> GetFaceEngineStatus()
    {
        try
        {
            var compreFaceReachable = _compreFaceService.IsAvailable &&
                await _compreFaceService.IsServiceAvailableAsync();

            return SuccessResponse(new
            {
                primaryEngine = new
                {
                    available = _luxandService.IsAvailable,
                    status = _luxandService.InitializationStatus,
                    returnCode = _luxandService.LastReturnCode,
                    error = _luxandService.InitializationError
                },
                backupEngine = new
                {
                    available = _compreFaceService.IsAvailable,
                    reachable = compreFaceReachable
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving face engine status");
            return ServerErrorResponse("An error occurred while retrieving face engine status");
        }
    }

    /// <summary>
    /// Enrolls Luxand face templates from existing visitor and staff/user profile photos.
    /// </summary>
    /// <param name="includeVisitors">Whether visitor profile photos should be scanned.</param>
    /// <param name="includeUsers">Whether staff/user profile photos should be scanned.</param>
    /// <param name="force">When true, adds a new template even if the identity already has one.</param>
    /// <returns>Enrollment summary and per-identity results.</returns>
    [HttpPost("face-templates/enroll-existing")]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    [ProducesResponseType(typeof(ApiResponseDto<FaceTemplateEnrollmentBatchResultDto>), 200)]
    public async Task<IActionResult> EnrollExistingFaceTemplates(
        [FromQuery] bool includeVisitors = true,
        [FromQuery] bool includeUsers = true,
        [FromQuery] bool force = false)
    {
        try
        {
            var result = await _faceTemplateEnrollmentService.EnrollExistingProfilePhotosAsync(
                includeVisitors,
                includeUsers,
                force,
                HttpContext.RequestAborted);

            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enrolling existing face templates");
            return ServerErrorResponse("An error occurred while enrolling face templates");
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
            var camera = await _mediator.Send(new GetCameraByIdQuery { Id = id });
            
            if (camera == null)
            {
                return NotFoundResponse("Camera", id);
            }

            var started = await _cameraService.StartStreamAsync(id, HttpContext.RequestAborted);
            var streamInfo = await _cameraService.GetStreamInfoAsync(id, HttpContext.RequestAborted);

            return SuccessResponse(new
            {
                CameraId = id,
                Success = started,
                IsStreaming = streamInfo?.IsStreaming ?? false,
                AutoStartPaused = false,
                StreamInfo = streamInfo,
                Message = started
                    ? "Camera stream worker started. FFmpeg grabbing and inference status are available in streamInfo."
                    : "Camera stream could not be started because the camera is not operational.",
                StartedAt = streamInfo?.StartedAt ?? DateTime.UtcNow
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
            var camera = await _mediator.Send(new GetCameraByIdQuery { Id = id });
            
            if (camera == null)
            {
                return NotFoundResponse("Camera", id);
            }

            var stopped = await _cameraService.StopStreamAsync(id, graceful, HttpContext.RequestAborted);

            return SuccessResponse(new
            {
                CameraId = id,
                Success = stopped,
                IsStreaming = false,
                AutoStartPaused = true,
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
            var camera = await _mediator.Send(new GetCameraByIdQuery { Id = id });
            
            if (camera == null)
            {
                return NotFoundResponse("Camera", id);
            }

            var streamInfo = await _cameraService.GetStreamInfoAsync(id, HttpContext.RequestAborted);

            return SuccessResponse(new
            {
                CameraId = id,
                IsStreaming = streamInfo?.IsStreaming ?? false,
                StreamInfo = streamInfo
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
            var camera = await _mediator.Send(new GetCameraByIdQuery { Id = id });
            
            if (camera == null)
            {
                return NotFoundResponse("Camera", id);
            }

            var result = await _cameraService.PerformHealthCheckAsync(id, HttpContext.RequestAborted);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing health check for camera {CameraId}", id);
            return ServerErrorResponse("An error occurred while performing health check");
        }
    }

    /// <summary>
    /// Returns the most recently grabbed frame with face detection overlay data for live debug inspection.
    /// Runs raw detection (no cooldowns or event publishing) and annotates which faces pass the current filter thresholds.
    /// </summary>
    [HttpGet("{id:int}/debug-frame")]
    [Authorize(Policy = Permissions.SystemConfig.Read)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> GetDebugFrame(int id)
    {
        try
        {
            var camera = await _mediator.Send(new GetCameraByIdQuery { Id = id });
            if (camera == null)
            {
                return NotFoundResponse("Camera", id);
            }

            var (bytes, capturedAt) = _cameraStreamRuntimeService.GetLastFrameSnapshot(id);
            if (bytes == null)
            {
                return Ok(new
                {
                    HasFrame = false,
                    ErrorMessage = "No frame captured yet — make sure the camera stream is running."
                });
            }

            var result = await _cameraFrameRecognitionService.InspectFrameAsync(id, bytes, HttpContext.RequestAborted);
            result.FrameCapturedAt = capturedAt;
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting debug frame for camera {CameraId}", id);
            return ServerErrorResponse("An error occurred while getting debug frame");
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
            var camera = await _mediator.Send(new GetCameraByIdQuery { Id = id });
            
            if (camera == null)
            {
                return NotFoundResponse("Camera", id);
            }

            var result = await _cameraService.CaptureFrameDetailsAsync(id, HttpContext.RequestAborted);
            if (!result.IsSuccess || result.FrameBytes == null)
            {
                return BadRequestResponse(result.ErrorMessage ?? "Frame capture failed");
            }

            return SuccessResponse(new
            {
                CameraId = id,
                Success = true,
                result.ContentType,
                FrameSize = result.FrameSizeBytes,
                FrameBase64 = Convert.ToBase64String(result.FrameBytes),
                result.CapturedAt,
                result.ElapsedMs,
                result.HardwareAcceleration,
                result.UsedCpuFallback,
                result.Details
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing frame for camera {CameraId}", id);
            return ServerErrorResponse("An error occurred while capturing frame");
        }
    }

    /// <summary>
    /// Processes a client-captured frame through the configured camera recognition path.
    /// USB cameras use this endpoint because their video device is attached to the browser workstation.
    /// </summary>
    /// <param name="id">Camera ID</param>
    /// <param name="request">Multipart frame payload and optional client metadata</param>
    /// <returns>Structured known/unknown face recognition result</returns>
    [HttpPost("{id:int}/process-frame")]
    [Authorize(Policy = Permissions.SystemConfig.Read)]
    [RequestSizeLimit(5_242_880)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponseDto<CameraFrameRecognitionResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> ProcessFrame(int id, [FromForm] CameraFrameProcessingRequestDto request)
    {
        try
        {
            var result = await _cameraFrameRecognitionService.ProcessFrameAsync(
                id,
                request,
                GetCurrentUserId(),
                GetClientIpAddress(),
                HttpContext.RequestAborted);

            if (!result.Success && result.ErrorCode == "CameraNotFound")
            {
                return NotFoundResponse("Camera", id);
            }

            if (!result.Success)
            {
                return BadRequestResponse(result.ErrorMessage ?? "Frame processing failed");
            }

            try
            {
                var events = await _cameraFaceEventService.PublishFrameEventsAsync(
                    result,
                    GetCurrentUserId(),
                    HttpContext.RequestAborted);

                result.Events = events.ToList();
            }
            catch (Exception eventEx)
            {
                _logger.LogError(
                    eventEx,
                    "Frame was processed but camera face event publication failed for camera {CameraId}",
                    id);
            }

            _cameraStreamRuntimeService.RecordInferenceResult(result);

            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing camera frame for camera {CameraId}", id);
            return ServerErrorResponse("An error occurred while processing camera frame");
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
            var results = (await _cameraService.PerformHealthCheckAllAsync(HttpContext.RequestAborted)).ToList();

            var summary = new Dictionary<string, int>
            {
                ["Healthy"] = results.Count(r => r.IsHealthy),
                ["Unhealthy"] = results.Count(r => !r.IsHealthy)
            };

            return SuccessResponse(new
            {
                Summary = summary,
                TotalCameras = results.Count,
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

    #region Face Recognition Status

    /// <summary>
    /// Returns the current availability mode of the CompreFace face recognition service.
    /// Receptionist dashboards call this on load to decide whether to show the FR check-in option.
    /// No permission required — any authenticated user may query this.
    /// </summary>
    [HttpGet("fr-status")]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    public IActionResult GetFaceRecognitionStatus()
    {
        try
        {
            var isAvailable = _faceDetectionService.IsAvailable;
            var mode = isAvailable ? "active" : "degraded";

            return SuccessResponse(new
            {
                isAvailable,
                mode,
                checkedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving face recognition status");
            return ServerErrorResponse("An error occurred while retrieving face recognition status");
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
