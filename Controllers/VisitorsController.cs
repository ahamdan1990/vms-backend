using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Text.Json;
using VisitorManagementSystem.Api.Application.Commands.Visitors;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.Queries.Visitors;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Controller for visitor management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VisitorsController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<VisitorsController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IUnitOfWork _unitOfWork;

    public VisitorsController(
        IMediator mediator,
        ILogger<VisitorsController> logger,
        IWebHostEnvironment environment,
        IUnitOfWork unitOfWork)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Gets a paginated list of visitors with optional filtering
    /// </summary>
    /// <param name="pageIndex">Page index (0-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="searchTerm">Search term</param>
    /// <param name="company">Company filter</param>
    /// <param name="isVip">VIP status filter</param>
    /// <param name="isBlacklisted">Blacklisted status filter</param>
    /// <param name="isActive">Active status filter</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortDirection">Sort direction</param>
    /// <param name="includeDeleted">Include deleted visitors</param>
    /// <returns>Paginated list of visitors</returns>
    [HttpGet]
    [Authorize(Policy = "Permissions.Any.Visitor.Read,Visitor.Read.Own,Visitor.Read.All")]
    public async Task<IActionResult> GetVisitors(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? company = null,
        [FromQuery] bool? isVip = null,
        [FromQuery] bool? isBlacklisted = null,
        [FromQuery] bool? isActive = true,
        [FromQuery] string sortBy = "FullName",
        [FromQuery] string sortDirection = "asc",
        [FromQuery] bool includeDeleted = false)
    {
        var query = new GetVisitorsQuery
        {
            PageIndex = pageIndex,
            PageSize = Math.Min(pageSize, 100), // Limit max page size
            SearchTerm = searchTerm,
            Company = company,
            IsVip = isVip,
            IsBlacklisted = isBlacklisted,
            IsActive = isActive,
            SortBy = sortBy,
            SortDirection = sortDirection,
            IncludeDeleted = includeDeleted
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets a visitor by ID
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <param name="includeDeleted">Include deleted visitor</param>
    /// <returns>Visitor details</returns>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permissions.Any.Visitor.Read,Visitor.Read.Own,Visitor.Read.All")]
    public async Task<IActionResult> GetVisitor(int id, [FromQuery] bool includeDeleted = false)
    {
        var query = new GetVisitorByIdQuery { Id = id, IncludeDeleted = includeDeleted };
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFoundResponse("Visitor", id);
        }

        return SuccessResponse(result);
    }

    /// <summary>
    /// Creates a new visitor
    /// </summary>
    /// <param name="createDto">Visitor creation data</param>
    /// <returns>Created visitor</returns>
    [HttpPost]
    [Authorize(Policy = Permissions.Visitor.Create)]
    public async Task<IActionResult> CreateVisitor([FromBody] CreateVisitorDto createDto)
    {
        // Debug logging for invitation creation issue
        _logger.LogDebug("CreateVisitor called with CreateInvitation: {CreateInvitation}", createDto.CreateInvitation);
        _logger.LogDebug("InvitationSubject: {Subject}", createDto.InvitationSubject);
        _logger.LogDebug("InvitationScheduledStartTime: {StartTime}", createDto.InvitationScheduledStartTime);

        // Check for recent duplicate request (within last 5 seconds)
        var cacheKey = $"visitor_create_{createDto.Email}_{GetCurrentUserId()}";
        
        // Check model validation state
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed. Errors: {Errors}", 
                string.Join("; ", ModelState.SelectMany(kvp => kvp.Value?.Errors?.Select(e => $"{kvp.Key}: {e.ErrorMessage}") ?? new string[0])));
        }

        // Validate phone number format
        if (!string.IsNullOrEmpty(createDto.PhoneNumber) &&
            !PhoneNumber.IsValidPhoneNumber(createDto.PhoneNumber))
        {
            return BadRequestResponse($"Invalid phone number format: {createDto.PhoneNumber}");
        }

        // Validate emergency contact phone numbers
        foreach (var contact in createDto.EmergencyContacts)
        {
            if (string.IsNullOrEmpty(contact.PhoneNumber) ||
                !PhoneNumber.IsValidPhoneNumber(contact.PhoneNumber))
            {
                return BadRequestResponse($"Invalid phone number format for emergency contact {contact.FirstName} {contact.LastName}: {contact.PhoneNumber}");
            }
        }

        var command = new CreateVisitorCommand
        {
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            Email = createDto.Email,
            
            // Enhanced phone fields
            PhoneNumber = createDto.PhoneNumber,
            PhoneCountryCode = createDto.PhoneCountryCode,
            PhoneType = createDto.PhoneType,
            
            Company = createDto.Company,
            JobTitle = createDto.JobTitle,
            Address = createDto.Address,
            DateOfBirth = createDto.DateOfBirth,
            GovernmentId = createDto.GovernmentId,
            GovernmentIdType = createDto.GovernmentIdType,
            Nationality = createDto.Nationality,
            Language = createDto.Language,
            
            // Visitor preferences
            PreferredLocationId = createDto.PreferredLocationId,
            DefaultVisitPurposeId = createDto.DefaultVisitPurposeId,
            TimeZone = createDto.TimeZone,
            
            DietaryRequirements = createDto.DietaryRequirements,
            AccessibilityRequirements = createDto.AccessibilityRequirements,
            SecurityClearance = createDto.SecurityClearance,
            IsVip = createDto.IsVip,
            Notes = createDto.Notes,
            ExternalId = createDto.ExternalId,
            EmergencyContacts = createDto.EmergencyContacts,

            // Invitation creation fields
            CreateInvitation = createDto.CreateInvitation,
            InvitationSubject = createDto.InvitationSubject,
            InvitationMessage = createDto.InvitationMessage,
            InvitationScheduledStartTime = createDto.InvitationScheduledStartTime,
            InvitationScheduledEndTime = createDto.InvitationScheduledEndTime,
            InvitationLocationId = createDto.InvitationLocationId,
            InvitationVisitPurposeId = createDto.InvitationVisitPurposeId,
            InvitationExpectedVisitorCount = createDto.InvitationExpectedVisitorCount,
            InvitationSpecialInstructions = createDto.InvitationSpecialInstructions,
            InvitationRequiresApproval = createDto.InvitationRequiresApproval,
            InvitationRequiresEscort = createDto.InvitationRequiresEscort,
            InvitationRequiresBadge = createDto.InvitationRequiresBadge,
            InvitationNeedsParking = createDto.InvitationNeedsParking,
            InvitationParkingInstructions = createDto.InvitationParkingInstructions,
            InvitationSubmitForApproval = createDto.InvitationSubmitForApproval,

            CreatedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        // Debug logging for command mapping
        _logger.LogDebug("Command CreateInvitation after mapping: {CreateInvitation}", command.CreateInvitation);
        _logger.LogDebug("Command InvitationSubject after mapping: {Subject}", command.InvitationSubject);

        var result = await _mediator.Send(command);
        return CreatedResponse(result, Url.Action(nameof(GetVisitor), new { id = result.Id }));
    }

    /// <summary>
    /// Updates an existing visitor
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <param name="updateDto">Visitor update data</param>
    /// <returns>Updated visitor</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.Visitor.Update)]
    public async Task<IActionResult> UpdateVisitor(int id, [FromBody] UpdateVisitorDto updateDto)
    {
        if (updateDto == null)
        {
            return BadRequestResponse("Visitor data is required");
        }

        // Validate phone number format
        if (!string.IsNullOrEmpty(updateDto.PhoneNumber) &&
            !PhoneNumber.IsValidPhoneNumber(updateDto.PhoneNumber))
        {
            return BadRequestResponse($"Invalid phone number format: {updateDto.PhoneNumber}");
        }

        var command = new UpdateVisitorCommand
        {
            Id = id,
            FirstName = updateDto.FirstName,
            LastName = updateDto.LastName,
            Email = updateDto.Email,
            PhoneNumber = updateDto.PhoneNumber,
            Company = updateDto.Company,
            JobTitle = updateDto.JobTitle,
            Address = updateDto.Address,
            DateOfBirth = updateDto.DateOfBirth,
            GovernmentId = updateDto.GovernmentId,
            GovernmentIdType = updateDto.GovernmentIdType,
            Nationality = updateDto.Nationality,
            Language = updateDto.Language,
            DietaryRequirements = updateDto.DietaryRequirements,
            AccessibilityRequirements = updateDto.AccessibilityRequirements,
            SecurityClearance = updateDto.SecurityClearance,
            Notes = updateDto.Notes,
            ExternalId = updateDto.ExternalId,
            ModifiedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Deletes a visitor (soft delete)
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <param name="permanentDelete">Whether to permanently delete</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.Visitor.Delete)]
    public async Task<IActionResult> DeleteVisitor(int id, [FromQuery] bool permanentDelete = false)
    {
        var command = new DeleteVisitorCommand
        {
            Id = id,
            DeletedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated"),
            PermanentDelete = permanentDelete
        };
        try
        {
            var result = await _mediator.Send(command);
            return SuccessResponse(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }

    }

    /// <summary>
    /// Searches for visitors with advanced filtering
    /// </summary>
    /// <param name="searchDto">Search parameters</param>
    /// <returns>Paginated search results</returns>
    [HttpPost("search")]
    [Authorize(Policy = "Permissions.Any.Visitor.Read,Visitor.Read.Own,Visitor.Read.All")]
    public async Task<IActionResult> SearchVisitors([FromBody] VisitorSearchDto searchDto)
    {
        var query = new SearchVisitorsQuery
        {
            SearchTerm = searchDto.SearchTerm,
            Company = searchDto.Company,
            IsVip = searchDto.IsVip,
            IsBlacklisted = searchDto.IsBlacklisted,
            IsActive = searchDto.IsActive,
            Nationality = searchDto.Nationality,
            SecurityClearance = searchDto.SecurityClearance,
            MinVisitCount = searchDto.MinVisitCount,
            MaxVisitCount = searchDto.MaxVisitCount,
            CreatedFrom = searchDto.CreatedFrom,
            CreatedTo = searchDto.CreatedTo,
            LastVisitFrom = searchDto.LastVisitFrom,
            LastVisitTo = searchDto.LastVisitTo,
            PageIndex = searchDto.PageIndex,
            PageSize = Math.Min(searchDto.PageSize, 100),
            SortBy = searchDto.SortBy,
            SortDirection = searchDto.SortDirection,
            IncludeDeleted = searchDto.IncludeDeleted
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Blacklists a visitor
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <param name="reason">Blacklist reason</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:int}/blacklist")]
    [Authorize(Policy = Permissions.Visitor.Blacklist)]
    public async Task<IActionResult> BlacklistVisitor(int id, [FromBody] string reason)
    {
        var command = new BlacklistVisitorCommand
        {
            Id = id,
            Reason = reason,
            BlacklistedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Removes blacklist status from a visitor
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:int}/blacklist")]
    [Authorize(Policy = Permissions.Visitor.RemoveBlacklist)]
    public async Task<IActionResult> RemoveBlacklist(int id)
    {
        var command = new RemoveBlacklistCommand
        {
            Id = id,
            ModifiedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Marks a visitor as VIP
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:int}/vip")]
    [Authorize(Policy = Permissions.Visitor.MarkAsVip)]
    public async Task<IActionResult> MarkAsVip(int id)
    {
        var command = new MarkAsVipCommand
        {
            Id = id,
            ModifiedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Removes VIP status from a visitor
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:int}/vip")]
    [Authorize(Policy = Permissions.Visitor.RemoveVipStatus)]
    public async Task<IActionResult> RemoveVipStatus(int id)
    {
        var command = new RemoveVipStatusCommand
        {
            Id = id,
            ModifiedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets VIP visitors
    /// </summary>
    /// <param name="includeDeleted">Include deleted visitors</param>
    /// <returns>List of VIP visitors</returns>
    [HttpGet("vip")]
    [Authorize(Policy = "Permissions.Any.Visitor.Read,Visitor.Read.Own,Visitor.Read.All")]
    public async Task<IActionResult> GetVipVisitors([FromQuery] bool includeDeleted = false)
    {
        var query = new GetVipVisitorsQuery { IncludeDeleted = includeDeleted };
        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets blacklisted visitors
    /// </summary>
    /// <param name="includeDeleted">Include deleted visitors</param>
    /// <returns>List of blacklisted visitors</returns>
    [HttpGet("blacklisted")]
    [Authorize(Policy = "Permissions.Any.Visitor.Read,Visitor.Read.Own,Visitor.Read.All")]
    public async Task<IActionResult> GetBlacklistedVisitors([FromQuery] bool includeDeleted = false)
    {
        var query = new GetBlacklistedVisitorsQuery { IncludeDeleted = includeDeleted };
        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets visitor statistics
    /// </summary>
    /// <param name="includeDeleted">Include deleted visitors in statistics</param>
    /// <returns>Visitor statistics</returns>
    [HttpGet("statistics")]
    [Authorize(Policy = Permissions.Visitor.ViewStatistics)]
    public async Task<IActionResult> GetVisitorStatistics([FromQuery] bool includeDeleted = false)
    {
        var query = new GetVisitorStatsQuery { IncludeDeleted = includeDeleted };
        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets visitor profile photo
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <returns>Profile photo file or 404 if not found</returns>
    [HttpGet("{id:int}/photo")]
    [Authorize(Policy = "Permissions.Any.Visitor.Read,Visitor.Read.Own,Visitor.Read.All")]
    public async Task<IActionResult> GetVisitorPhoto(int id)
    {
        try
        {
            // Get visitor from database
            var visitorEntity = await _unitOfWork.Visitors.GetByIdAsync(id);
            if (visitorEntity == null)
            {
                return NotFound(new { message = $"Visitor with ID {id} not found" });
            }

            // Check if visitor has a profile photo path
            if (string.IsNullOrEmpty(visitorEntity.ProfilePhotoPath))
            {
                return NotFound(new { message = "No profile photo found for this visitor" });
            }

            // Construct file path
            var relativePath = visitorEntity.ProfilePhotoPath.Replace('/', Path.DirectorySeparatorChar);
            var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRootPath, relativePath);

            _logger.LogDebug("Serving visitor photo: {FilePath}", filePath);

            // Check if file exists
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("Photo file not found: {FilePath} for visitor {VisitorId}", filePath, id);
                return NotFound(new { message = "Photo file not found on server" });
            }

            // Determine content type based on file extension
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".tiff" => "image/tiff",
                _ => "application/octet-stream"
            };

            // Read file and return with proper headers
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            // Override CORP header set by SecurityHeadersMiddleware to allow cross-origin
            // Remove the restrictive header first, then add the permissive one
            Response.Headers.Remove("Cross-Origin-Resource-Policy");
            Response.Headers.Append("Cross-Origin-Resource-Policy", "cross-origin");

            // Ensure CORS headers are set
            if (!Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
            {
                Response.Headers.Append("Access-Control-Allow-Origin", "*");
            }

            return File(fileBytes, contentType, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile photo for visitor {VisitorId}", id);
            return BadRequest(new { message = $"Error retrieving profile photo: {ex.Message}" });
        }
    }

    /// <summary>
    /// Uploads visitor profile photo
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <param name="file">Photo file</param>
    /// <returns>Photo URL</returns>
    [HttpPost("{id}/photo")]
    [Authorize(Policy = Permissions.Visitor.Update)]
    public async Task<IActionResult> UploadVisitorPhoto(int id, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequestResponse("No file provided");
            }

            var command = new UploadVisitorProfilePhotoCommand
            {
                VisitorId = id,
                File = file
            };

            var photoUrl = await _mediator.Send(command);
            return SuccessResponse(new { photoUrl });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid photo upload for visitor {VisitorId}", id);
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo for visitor {VisitorId}", id);
            return BadRequestResponse($"Error uploading photo: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes visitor profile photo
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id}/photo")]
    [Authorize(Policy = Permissions.Visitor.Update)]
    public async Task<IActionResult> RemoveVisitorPhoto(int id)
    {
        try
        {
            // Get visitor to check if photo exists
            var visitor = await _mediator.Send(new GetVisitorByIdQuery { Id = id });
            if (visitor == null)
            {
                return NotFoundResponse("Visitor", id);
            }

            if (string.IsNullOrEmpty(visitor.ProfilePhotoUrl))
            {
                return NotFoundResponse("Profile photo", 0);
            }

            // Use the existing UpdateVisitor command to set ProfilePhotoPath to null
            var updateDto = new UpdateVisitorDto
            {
                FirstName = visitor.FirstName,
                LastName = visitor.LastName,
                Email = visitor.Email,
                PhoneNumber = visitor.PhoneNumber,
                Company = visitor.Company,
                JobTitle = visitor.JobTitle,
                DateOfBirth = visitor.DateOfBirth,
                GovernmentId = visitor.GovernmentId,
                GovernmentIdType = visitor.GovernmentIdType,
                Nationality = visitor.Nationality,
                Language = visitor.Language,
                DietaryRequirements = visitor.DietaryRequirements,
                AccessibilityRequirements = visitor.AccessibilityRequirements,
                SecurityClearance = visitor.SecurityClearance,
                Notes = visitor.Notes,
                ExternalId = visitor.ExternalId,
                Address = visitor.Address
            };

            var command = new UpdateVisitorCommand
            {
                Id = id,
                FirstName = updateDto.FirstName,
                LastName = updateDto.LastName,
                Email = updateDto.Email,
                PhoneNumber = updateDto.PhoneNumber,
                Company = updateDto.Company,
                JobTitle = updateDto.JobTitle,
                Address = updateDto.Address,
                DateOfBirth = updateDto.DateOfBirth,
                GovernmentId = updateDto.GovernmentId,
                GovernmentIdType = updateDto.GovernmentIdType,
                Nationality = updateDto.Nationality,
                Language = updateDto.Language,
                DietaryRequirements = updateDto.DietaryRequirements,
                AccessibilityRequirements = updateDto.AccessibilityRequirements,
                SecurityClearance = updateDto.SecurityClearance,
                Notes = updateDto.Notes,
                ExternalId = updateDto.ExternalId,
                ModifiedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
            };

            await _mediator.Send(command);
            return SuccessResponse(new { message = "Profile photo removed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing photo for visitor {VisitorId}", id);
            return BadRequestResponse($"Error removing photo: {ex.Message}");
        }
    }

    /// <summary>
    /// Debug endpoint to test model binding for invitation creation
    /// </summary>
    [HttpPost("debug-binding")]
    [Authorize(Policy = Permissions.Visitor.Create)]
    public async Task<IActionResult> DebugBinding([FromBody] object rawData)
    {
        try
        {
            // Read the raw request body
            Request.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();
            _logger.LogInformation("Raw HTTP Body: {RawBody}", rawBody);

            var json = System.Text.Json.JsonSerializer.Serialize(rawData);
            _logger.LogInformation("Raw JSON received: {Json}", json);

            // Try to deserialize to our DTO
            var createDto = System.Text.Json.JsonSerializer.Deserialize<CreateVisitorDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogInformation("Deserialized CreateInvitation: {CreateInvitation}", createDto?.CreateInvitation);
            _logger.LogInformation("Deserialized InvitationSubject: {Subject}", createDto?.InvitationSubject);

            return Ok(new
            {
                RawHttpBody = rawBody,
                RawJson = json,
                CreateInvitation = createDto?.CreateInvitation,
                InvitationSubject = createDto?.InvitationSubject,
                InvitationScheduledStartTime = createDto?.InvitationScheduledStartTime,
                InvitationScheduledEndTime = createDto?.InvitationScheduledEndTime,
                ModelValidationErrors = ModelState.Where(x => x.Value?.Errors?.Count > 0)
                    .ToDictionary(x => x.Key, x => x.Value?.Errors?.Select(e => e.ErrorMessage))
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Debug binding failed");
            return BadRequest(new { error = ex.Message });
        }
    }
}
