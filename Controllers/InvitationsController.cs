using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.Commands.Invitations;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Application.Queries.Invitations;
using VisitorManagementSystem.Api.Application.Services.QrCode;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Controller for invitation management operations
/// </summary>
[ApiController]
[Route("api/invitations")]
[Authorize]
public class InvitationsController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IQrCodeService _qrCodeService;
    private readonly ILogger<InvitationsController> _logger;

    public InvitationsController(IMediator mediator, IQrCodeService qrCodeService, ILogger<InvitationsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _qrCodeService = qrCodeService ?? throw new ArgumentNullException(nameof(qrCodeService));
        _logger = logger;
    }

    /// <summary>
    /// Gets invitations with filtering and paging
    /// </summary>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="searchTerm">Search term</param>
    /// <param name="status">Status filter</param>
    /// <param name="type">Type filter</param>
    /// <param name="hostId">Host filter</param>
    /// <param name="visitorId">Visitor filter</param>
    /// <param name="visitPurposeId">Visit purpose filter</param>
    /// <param name="locationId">Location filter</param>
    /// <param name="startDate">Start date filter</param>
    /// <param name="endDate">End date filter</param>
    /// <param name="includeDeleted">Include deleted</param>
    /// <param name="pendingApprovalsOnly">Only pending approvals</param>
    /// <param name="activeOnly">Only active invitations</param>
    /// <param name="expiredOnly">Only expired invitations</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortDirection">Sort direction</param>
    /// <returns>Paged list of invitations</returns>
    [HttpGet]
    [Authorize(Policy = Permissions.Invitation.ReadOwn)]
    public async Task<IActionResult> GetInvitations(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] InvitationStatus? status = null,
        [FromQuery] InvitationType? type = null,
        [FromQuery] int? hostId = null,
        [FromQuery] int? visitorId = null,
        [FromQuery] int? visitPurposeId = null,
        [FromQuery] int? locationId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool pendingApprovalsOnly = false,
        [FromQuery] bool activeOnly = false,
        [FromQuery] bool expiredOnly = false,
        [FromQuery] string sortBy = "ScheduledStartTime",
        [FromQuery] string sortDirection = "desc")
    {
        var query = new GetInvitationsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            Status = status,
            Type = type,
            HostId = hostId,
            VisitorId = visitorId,
            VisitPurposeId = visitPurposeId,
            LocationId = locationId,
            StartDate = startDate,
            EndDate = endDate,
            IncludeDeleted = includeDeleted,
            PendingApprovalsOnly = pendingApprovalsOnly,
            ActiveOnly = activeOnly,
            ExpiredOnly = expiredOnly,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }
    /// <summary>
    /// Gets an invitation by ID
    /// </summary>
    /// <param name="id">Invitation ID</param>
    /// <param name="includeDeleted">Include deleted invitation</param>
    /// <param name="includeEvents">Include events timeline</param>
    /// <param name="includeApprovals">Include approvals workflow</param>
    /// <returns>Invitation details</returns>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.Invitation.ReadOwn)]
    public async Task<IActionResult> GetInvitation(
        int id,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool includeEvents = false,
        [FromQuery] bool includeApprovals = false)
    {
        var query = new GetInvitationByIdQuery
        {
            Id = id,
            IncludeDeleted = includeDeleted,
            IncludeEvents = includeEvents,
            IncludeApprovals = includeApprovals
        };

        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFoundResponse("Invitation", id);
        }

        return SuccessResponse(result);
    }

    /// <summary>
    /// Creates a new invitation
    /// </summary>
    /// <param name="createDto">Invitation creation data</param>
    /// <returns>Created invitation</returns>
    [HttpPost]
    [Authorize(Policy = Permissions.Invitation.Create)]
    public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationDto createDto)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        // Enhanced logging for debugging
        _logger.LogDebug("CreateInvitation called. CorrelationId: {CorrelationId}", correlationId);
        _logger.LogDebug("Request Content-Type: {ContentType}", HttpContext.Request.ContentType);
        
        if (createDto == null)
        {
            _logger.LogWarning("CreateInvitationDto is null. CorrelationId: {CorrelationId}", correlationId);
            return BadRequest("Request body cannot be null.");
        }

        // Log the received DTO for debugging
        _logger.LogDebug("Received CreateInvitationDto: {@CreateDto}. CorrelationId: {CorrelationId}", createDto, correlationId);
        // Determine the visitor ID based on invitation type
        int visitorId;
        if (createDto.Type == InvitationType.Single)
        {
            if (!createDto.VisitorId.HasValue)
            {
                return BadRequest("VisitorId is required for single visitor invitations.");
            }
            visitorId = createDto.VisitorId.Value;
        }
        else if (createDto.Type == InvitationType.Group)
        {
            if (createDto.VisitorIds == null || !createDto.VisitorIds.Any())
            {
                return BadRequest("At least one visitor ID is required for group invitations.");
            }
            // For now, use the first visitor as the primary visitor
            // TODO: Enhance system to support multiple visitors properly
            visitorId = createDto.VisitorIds.First();
        }
        else
        {
            return BadRequest("Invalid invitation type specified.");
        }

        var command = new CreateInvitationCommand
        {
            VisitorId = visitorId,
            HostId = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated"),
            VisitPurposeId = createDto.VisitPurposeId,
            LocationId = createDto.LocationId,
            Type = createDto.Type,
            Subject = createDto.Subject,
            Message = createDto.Message,
            ScheduledStartTime = createDto.ScheduledStartTime,
            ScheduledEndTime = createDto.ScheduledEndTime,
            ExpectedVisitorCount = createDto.ExpectedVisitorCount,
            SpecialInstructions = createDto.SpecialInstructions,
            RequiresApproval = createDto.RequiresApproval,
            RequiresEscort = createDto.RequiresEscort,
            RequiresBadge = createDto.RequiresBadge,
            NeedsParking = createDto.NeedsParking,
            ParkingInstructions = createDto.ParkingInstructions,
            TemplateId = createDto.TemplateId,
            SubmitForApproval = createDto.SubmitForApproval,
            CreatedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return CreatedResponse(result, Url.Action(nameof(GetInvitation), new { id = result.Id }));
    }

    /// <summary>
    /// Updates an existing invitation
    /// </summary>
    /// <param name="id">Invitation ID</param>
    /// <param name="updateDto">Invitation update data</param>
    /// <returns>Updated invitation</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.Invitation.UpdateOwn)]
    public async Task<IActionResult> UpdateInvitation(int id, [FromBody] UpdateInvitationDto updateDto)
    {
        var command = new UpdateInvitationCommand
        {
            Id = id,
            VisitPurposeId = updateDto.VisitPurposeId,
            LocationId = updateDto.LocationId,
            Type = updateDto.Type,
            Subject = updateDto.Subject,
            Message = updateDto.Message,
            ScheduledStartTime = updateDto.ScheduledStartTime,
            ScheduledEndTime = updateDto.ScheduledEndTime,
            ExpectedVisitorCount = updateDto.ExpectedVisitorCount,
            SpecialInstructions = updateDto.SpecialInstructions,
            RequiresApproval = updateDto.RequiresApproval,
            RequiresEscort = updateDto.RequiresEscort,
            RequiresBadge = updateDto.RequiresBadge,
            NeedsParking = updateDto.NeedsParking,
            ParkingInstructions = updateDto.ParkingInstructions,
            ModifiedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Approves an invitation
    /// </summary>
    /// <param name="id">Invitation ID</param>
    /// <param name="approvalDto">Approval data</param>
    /// <returns>Updated invitation</returns>
    [HttpPost("{id:int}/approve")]
    [Authorize(Policy = Permissions.Invitation.Approve)]
    public async Task<IActionResult> ApproveInvitation(int id, [FromBody] ApproveInvitationDto approvalDto)
    {
        var command = new ApproveInvitationCommand
        {
            InvitationId = id,
            Comments = approvalDto.Comments,
            ApprovedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Rejects an invitation
    /// </summary>
    /// <param name="id">Invitation ID</param>
    /// <param name="rejectionDto">Rejection data</param>
    /// <returns>Updated invitation</returns>
    [HttpPost("{id:int}/reject")]
    [Authorize(Policy = Permissions.Invitation.Approve)]
    public async Task<IActionResult> RejectInvitation(int id, [FromBody] RejectInvitationDto rejectionDto)
    {
        var command = new RejectInvitationCommand
        {
            InvitationId = id,
            Reason = rejectionDto.Reason,
            RejectedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets pending approvals for the current user or all (admin)
    /// </summary>
    /// <param name="forCurrentUserOnly">Only for current user</param>
    /// <returns>List of pending approvals</returns>
    [HttpGet("pending-approvals")]
    [Authorize(Policy = Permissions.Invitation.Approve)]
    public async Task<IActionResult> GetPendingApprovals([FromQuery] bool forCurrentUserOnly = false)
    {
        var query = new GetInvitationsQuery
        {
            PendingApprovalsOnly = true,
            HostId = forCurrentUserOnly ? GetCurrentUserId() : null,
            PageSize = 100 // Get more pending approvals
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets invitation statistics
    /// </summary>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <param name="hostId">Optional host filter</param>
    /// <param name="includeDeleted">Include deleted invitations</param>
    /// <returns>Statistics</returns>
    [HttpGet("statistics")]
    [Authorize(Policy = Permissions.Invitation.ReadOwn)]
    public async Task<IActionResult> GetInvitationStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? hostId = null,
        [FromQuery] bool includeDeleted = false)
    {
        var query = new GetInvitationStatisticsQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            HostId = hostId,
            IncludeDeleted = includeDeleted
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Generates QR code for invitation
    /// </summary>
    /// <param name="id">Invitation ID</param>
    /// <returns>QR code data</returns>
    [HttpGet("{id:int}/qr-code")]
    [Authorize(Policy = Permissions.Invitation.ReadOwn)]
    public async Task<IActionResult> GetInvitationQrCode(int id)
    {
        var query = new GetInvitationByIdQuery { Id = id };
        var invitation = await _mediator.Send(query);

        if (invitation == null)
        {
            return NotFoundResponse("Invitation", id);
        }

        if (string.IsNullOrEmpty(invitation.QrCode))
        {
            return BadRequestResponse("QR code not generated for this invitation");
        }

        return SuccessResponse(new { QrCode = invitation.QrCode });
    }


    /// <summary>
    /// Cancels an invitation
    /// </summary>
    /// <param name="id">Invitation ID</param>
    /// <param name="cancellationDto">Cancellation data</param>
    /// <returns>Updated invitation</returns>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Policy = Permissions.Invitation.CancelAll)]
    public async Task<IActionResult> CancelInvitation(int id, [FromBody] CancelInvitationDto cancellationDto)
    {
        var command = new CancelInvitationCommand
        {
            InvitationId = id,
            Reason = cancellationDto.Reason,
            CancelledBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Deletes an invitation (only if status is Cancelled)
    /// </summary>
    /// <param name="id">Invitation ID</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.Invitation.Delete)]
    public async Task<IActionResult> DeleteInvitation(int id)
    {
        var command = new DeleteInvitationCommand
        {
            Id = id,
            DeletedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        try
        {
            var result = await _mediator.Send(command);
            return SuccessResponse(result, "Invitation deleted successfully");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
    }

    /// <summary>
    /// Submits an invitation for approval
    /// </summary>
    /// <param name="id">Invitation ID</param>
    /// <returns>Updated invitation</returns>
    [HttpPost("{id:int}/submit")]
    [Authorize(Policy = Permissions.Invitation.Create)]
    public async Task<IActionResult> SubmitInvitation(int id)
    {
        var command = new SubmitInvitationCommand
        {
            InvitationId = id,
            SubmittedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Checks in a visitor using invitation
    /// </summary>
    /// <param name="checkInDto">Check-in data</param>
    /// <returns>Updated invitation</returns>
    [HttpPost("check-in")]
    [Authorize(Policy = Permissions.CheckIn.Process)]
    public async Task<IActionResult> CheckInInvitation([FromBody] CheckInInvitationDto checkInDto)
    {
        var command = new CheckInInvitationCommand
        {
            InvitationReference = checkInDto.InvitationReference,
            CheckedInBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated"),
            Notes = checkInDto.Notes
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Checks out a visitor
    /// </summary>
    /// <param name="id">Invitation ID</param>
    /// <param name="checkOutDto">Check-out data</param>
    /// <returns>Updated invitation</returns>
    [HttpPost("{id:int}/check-out")]
    [Authorize(Policy = Permissions.CheckIn.Process)]
    public async Task<IActionResult> CheckOutInvitation(int id, [FromBody] CheckOutInvitationDto checkOutDto)
    {
        var command = new CheckOutInvitationCommand
        {
            InvitationId = id,
            CheckedOutBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated"),
            Notes = checkOutDto.Notes
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets QR code image for an invitation
    /// </summary>
    /// <param name="id">Invitation ID</param>
    /// <param name="size">QR code size in pixels (default: 300)</param>
    /// <param name="branded">Include company branding (default: false)</param>
    /// <returns>QR code image as PNG</returns>
    [HttpGet("{id:int}/qr-code/image")]
    [Authorize(Policy = Permissions.Invitation.Read)]
    public async Task<IActionResult> GetInvitationQrCodeImage(int id, [FromQuery] int size = 300, [FromQuery] bool branded = false)
    {
        try
        {
            var invitation = await _mediator.Send(new GetInvitationByIdQuery { Id = id });
            if (invitation?.QrCode == null)
            {
                return NotFound("Invitation not found or QR code not generated");
            }

            byte[] qrImageBytes;
            
            if (branded)
            {
                // Try to use branded QR code with logo
                qrImageBytes = await _qrCodeService.GenerateBrandedQrCodeImageAsync(invitation.QrCode, null, size);
            }
            else
            {
                // Generate standard QR code
                qrImageBytes = await _qrCodeService.GenerateQrCodeImageAsync(invitation.QrCode, size);
            }

            return File(qrImageBytes, "image/png", $"invitation-{invitation.InvitationNumber}-qr.png");
        }
        catch (Exception ex)
        {
            return BadRequestResponse($"Failed to generate QR code: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates QR code data
    /// </summary>
    /// <param name="qrData">QR code data to validate</param>
    /// <returns>Validation result</returns>
    [HttpPost("qr-code/validate")]
    [Authorize(Policy = Permissions.Invitation.Read)]
    public async Task<IActionResult> ValidateQrCode([FromBody] ValidateQrCodeDto validateDto)
    {
        try
        {
            var isValid = await _qrCodeService.ValidateQrCodeDataAsync(validateDto.QrData);
            
            if (!isValid)
            {
                return BadRequestResponse("Invalid QR code format");
            }

            var invitationNumber = _qrCodeService.ExtractInvitationNumberFromQrData(validateDto.QrData);
            
            if (invitationNumber != null)
            {
                var invitation = await _mediator.Send(new GetInvitationByNumberQuery { InvitationNumber = invitationNumber });
                if (invitation != null)
                {
                    return SuccessResponse(new
                    {
                        IsValid = true,
                        InvitationNumber = invitationNumber,
                        Invitation = invitation
                    });
                }
            }

            return SuccessResponse(new { IsValid = true, InvitationNumber = invitationNumber });
        }
        catch (Exception ex)
        {
            return BadRequestResponse($"QR code validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets QR code data for an invitation
    /// </summary>
    /// <param name="id">Invitation ID</param>
    /// <returns>QR code data</returns>
    [HttpGet("{id:int}/qr-code/data")]
    [Authorize(Policy = Permissions.Invitation.Read)]
    public async Task<IActionResult> GetInvitationQrCodeData(int id)
    {
        try
        {
            var invitation = await _mediator.Send(new GetInvitationByIdQuery { Id = id });
            if (invitation?.QrCode == null)
            {
                return NotFound("Invitation not found or QR code not generated");
            }

            return SuccessResponse(new { QrCode = invitation.QrCode });
        }
        catch (Exception ex)
        {
            return BadRequestResponse($"Failed to get QR code data: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends QR code to visitor via email
    /// </summary>
    /// <param name="id">Invitation ID</param>
    /// <param name="emailDto">Email sending options</param>
    /// <returns>Email sending result</returns>
    [HttpPost("{id:int}/send-qr-email")]
    [Authorize(Policy = Permissions.Invitation.Read)]
    public async Task<IActionResult> SendQrCodeEmail(int id, [FromBody] SendQrEmailDto emailDto)
    {
        try { 
            var response = await _mediator.Send(new SendQrEmailCommand(id, emailDto));

            return SuccessResponse("QR code sent successfully to visitor's email");
        }
        catch (Exception ex)
        {
            return BadRequestResponse($"Failed to send QR code: {ex.Message}");
        }
    }
}

/// <summary>
/// Additional DTOs for workflow operations
/// </summary>
public class CancelInvitationDto
{
    /// <summary>
    /// Cancellation reason
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class CheckInInvitationDto
{
    /// <summary>
    /// Invitation reference (ID or QR code)
    /// </summary>
    [Required]
    public string InvitationReference { get; set; } = string.Empty;

    /// <summary>
    /// Check-in notes
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class CheckOutInvitationDto
{
    /// <summary>
    /// Check-out notes
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}
/// <summary>
/// DTO for QR code validation
/// </summary>
public class ValidateQrCodeDto
{
    /// <summary>
    /// QR code data to validate
    /// </summary>
    [Required]
    public string QrData { get; set; } = string.Empty;
}

/// <summary>
/// DTO for sending QR code via email
/// </summary>
public class SendQrEmailDto
{
    /// <summary>
    /// Custom message to include in email (optional)
    /// </summary>
    [MaxLength(500)]
    public string? CustomMessage { get; set; }

    /// <summary>
    /// Include QR code as image attachment
    /// </summary>
    public bool IncludeQrImage { get; set; } = true;

    /// <summary>
    /// Alternative email address (if different from visitor's email)
    /// </summary>
    [EmailAddress]
    public string? AlternativeEmail { get; set; }
}
