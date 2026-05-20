using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.Commands.FaceEvents;
using VisitorManagementSystem.Api.Application.DTOs.Cameras;
using VisitorManagementSystem.Api.Application.Services.Cameras;
using VisitorManagementSystem.Api.Application.Services.Common;
using VisitorManagementSystem.Api.Application.Services.FaceDetection;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Controllers;

[ApiController]
[Route("api/face-events")]
[Authorize]
[Produces("application/json")]
public class CameraFaceEventsController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFaceSnapshotService _snapshotService;
    private readonly IUrlResolverService _urlResolver;
    private readonly IFaceTemplateEnrollmentService _enrollmentService;
    private readonly IMediator _mediator;
    private readonly ICameraFaceEventService _faceEventService;
    private readonly ILogger<CameraFaceEventsController> _logger;

    public CameraFaceEventsController(
        IUnitOfWork unitOfWork,
        IFaceSnapshotService snapshotService,
        IUrlResolverService urlResolver,
        IFaceTemplateEnrollmentService enrollmentService,
        IMediator mediator,
        ICameraFaceEventService faceEventService,
        ILogger<CameraFaceEventsController> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
        _urlResolver = urlResolver ?? throw new ArgumentNullException(nameof(urlResolver));
        _enrollmentService = enrollmentService ?? throw new ArgumentNullException(nameof(enrollmentService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _faceEventService = faceEventService ?? throw new ArgumentNullException(nameof(faceEventService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Returns a paged list of face events with optional filters.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "Composite.AdminOrReceptionist")]
    public async Task<ActionResult<CameraFaceEventPagedDto>> GetFaceEvents(
        [FromQuery] int? cameraId = null,
        [FromQuery] bool? isKnown = null,
        [FromQuery] FaceEventReviewStatus? reviewStatus = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (events, total) = await _unitOfWork.FaceEvents.GetPagedAsync(
            cameraId, isKnown, reviewStatus, from, to, page, pageSize, cancellationToken);
        var personNames = await BuildPersonNameLookupAsync(events, cancellationToken);

        return Ok(new CameraFaceEventPagedDto
        {
            Events = events.Select(ev => MapToDto(ev, personNames)).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Returns a single face event by its DB id.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "Composite.AdminOrReceptionist")]
    public async Task<ActionResult<CameraFaceEventDto>> GetFaceEvent(
        int id,
        CancellationToken cancellationToken = default)
    {
        var ev = await _unitOfWork.FaceEvents.GetByIdAsync(id, cancellationToken);
        if (ev == null)
            return NotFound();

        return Ok(await MapToDtoAsync(ev, cancellationToken));
    }

    /// <summary>
    /// Returns a single face event by its client EventId GUID.
    /// </summary>
    [HttpGet("by-event-id/{eventId}")]
    [Authorize(Policy = "Composite.AdminOrReceptionist")]
    public async Task<ActionResult<CameraFaceEventDto>> GetByEventId(
        string eventId,
        CancellationToken cancellationToken = default)
    {
        var ev = await _unitOfWork.FaceEvents.GetByEventIdAsync(eventId, cancellationToken);
        if (ev == null)
            return NotFound();

        return Ok(await MapToDtoAsync(ev, cancellationToken));
    }

    /// <summary>
    /// Updates the review status of a face event.
    /// </summary>
    [HttpPost("{id:int}/review")]
    [Authorize(Policy = "Composite.AdminOrReceptionist")]
    public async Task<ActionResult<CameraFaceEventDto>> ReviewFaceEvent(
        int id,
        [FromBody] ReviewFaceEventRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var ev = await _unitOfWork.FaceEvents.GetByIdAsync(id, cancellationToken);
        if (ev == null)
            return NotFound();

        ev.ReviewStatus = request.Status;
        ev.ReviewNotes = request.Notes;
        ev.ReviewedAt = DateTime.UtcNow;
        ev.ReviewedById = GetCurrentUserId();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (request.Status == FaceEventReviewStatus.Dismissed)
            _faceEventService.ClearPersonCooldown(ev.CameraId, ev.IsKnown, ev.PersonType, ev.PersonId, ev.SubjectId);

        _logger.LogInformation(
            "Face event {EventId} reviewed. Status={Status}, ReviewedBy={UserId}",
            ev.EventId, request.Status, ev.ReviewedById);

        return Ok(await MapToDtoAsync(ev, cancellationToken));
    }

    /// <summary>
    /// Dismisses a face event (sets ReviewStatus = Dismissed).
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Composite.AdminOrReceptionist")]
    public async Task<IActionResult> DismissFaceEvent(
        int id,
        CancellationToken cancellationToken = default)
    {
        var ev = await _unitOfWork.FaceEvents.GetByIdAsync(id, cancellationToken);
        if (ev == null)
            return NotFound();

        ev.ReviewStatus = FaceEventReviewStatus.Dismissed;
        ev.ReviewedAt = DateTime.UtcNow;
        ev.ReviewedById = GetCurrentUserId();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _faceEventService.ClearPersonCooldown(ev.CameraId, ev.IsKnown, ev.PersonType, ev.PersonId, ev.SubjectId);

        return NoContent();
    }

    /// <summary>
    /// Hard-deletes all face events matching the optional filters.
    /// Returns the count of deleted records.
    /// Requires Admin role.
    /// </summary>
    [HttpDelete]
    [Authorize(Policy = "Composite.AdminOrReceptionist")]
    public async Task<ActionResult<BulkClearFaceEventsResultDto>> BulkClearFaceEvents(
        [FromQuery] int? cameraId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _unitOfWork.FaceEvents.BulkClearAsync(cameraId, from, to, cancellationToken);

        _logger.LogInformation(
            "Bulk face event clear. CameraId={CameraId}, From={From}, To={To}, Deleted={Deleted}, RequestedBy={UserId}",
            cameraId, from, to, deleted, GetCurrentUserId());

        return Ok(new BulkClearFaceEventsResultDto { Deleted = deleted });
    }

    /// <summary>
    /// Returns a face event with enriched person context (profile, invitations, presence).
    /// </summary>
    [HttpGet("{id:int}/context")]
    [Authorize(Policy = "Composite.AdminOrReceptionist")]
    public async Task<ActionResult<CameraFaceEventContextDto>> GetFaceEventContext(
        int id,
        CancellationToken cancellationToken = default)
    {
        var ev = await _unitOfWork.FaceEvents.GetByIdAsync(id, cancellationToken);
        if (ev == null)
            return NotFound();

        var result = new CameraFaceEventContextDto { FaceEvent = await MapToDtoAsync(ev, cancellationToken) };

        if (ev.IsKnown && ev.PersonId.HasValue)
        {
            result.Person = ev.PersonType == "Staff"
                ? await BuildStaffContextAsync(ev.PersonId.Value, cancellationToken)
                : await BuildVisitorContextAsync(ev.PersonId.Value, cancellationToken);

            if (result.Person != null)
            {
                result.FaceEvent.FullName = result.Person.FullName;
            }
        }

        return Ok(result);
    }

    /// <summary>
    /// Enrolls the snapshot from a known face event as an additional face template for
    /// the matched person. The person's profile photo is never changed.
    /// Returns SuggestPromoteToPrimary=true when the snapshot quality exceeds the primary template.
    /// </summary>
    [HttpPost("{id:int}/enroll-as-template")]
    [Authorize(Policy = "Composite.AdminOrReceptionist")]
    public async Task<ActionResult<EnrollFromEventResultDto>> EnrollAsTemplate(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _enrollmentService.EnrollFromFaceEventSnapshotAsync(id, cancellationToken);

        if (!result.Success && !result.Skipped)
            return BadRequest(new EnrollFromEventResultDto
            {
                Success = false,
                Message = result.Message
            });

        if (result.Success)
        {
            _logger.LogInformation(
                "Snapshot from face event {EventId} enrolled as additional template for {PersonType} {PersonId}. TemplateId={TemplateId}",
                id, result.PersonType, result.PersonId, result.TemplateId);
        }

        return Ok(new EnrollFromEventResultDto
        {
            Success = result.Success,
            Message = result.Message,
            TemplateId = result.TemplateId,
            NewTemplateQuality = result.NewTemplateQuality,
            PrimaryTemplateQuality = result.PrimaryTemplateQuality,
            SuggestPromoteToPrimary = result.SuggestPromoteToPrimary
        });
    }

    /// <summary>
    /// Manually assigns an unknown face event to an existing visitor or staff member.
    /// Optionally enrolls the event snapshot as an additional face template for the matched person.
    /// </summary>
    [HttpPost("{id:int}/assign")]
    [Authorize(Policy = "Composite.AdminOrReceptionist")]
    public async Task<ActionResult<AssignFaceEventResultDto>> AssignToPerson(
        int id,
        [FromBody] AssignFaceEventRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var command = new AssignFaceEventToPersonCommand
        {
            FaceEventId = id,
            PersonType = request.PersonType,
            PersonId = request.PersonId,
            EnrollSnapshot = request.EnrollSnapshot,
            Notes = request.Notes,
            ReviewedById = GetCurrentUserId() ?? 0
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
            return BadRequest(new AssignFaceEventResultDto
            {
                Success = false,
                Message = result.Message
            });

        return Ok(new AssignFaceEventResultDto
        {
            Success = result.Success,
            Message = result.Message,
            TemplateId = result.TemplateId,
            NewTemplateQuality = result.NewTemplateQuality,
            PrimaryTemplateQuality = result.PrimaryTemplateQuality,
            SuggestPromoteToPrimary = result.SuggestPromoteToPrimary
        });
    }

    private async Task<PersonContextDto?> BuildVisitorContextAsync(int visitorId, CancellationToken ct)
    {
        var visitor = await _unitOfWork.Visitors.GetByIdAsync(visitorId, ct);
        if (visitor == null) return null;

        var hasFaceTemplate = await _unitOfWork.Repository<FaceTemplate>()
            .AnyAsync(t => t.VisitorId == visitorId && !t.IsDeleted, ct);

        var allInvitations = await _unitOfWork.Invitations.GetByVisitorIdAsync(visitorId, ct);
        var today = DateTime.UtcNow.Date;
        var todayInvitations = allInvitations
            .Where(i => i.ScheduledStartTime.Date == today || i.Status == InvitationStatus.Active)
            .OrderBy(i => i.ScheduledStartTime)
            .Select(MapInvitationSummary)
            .ToList();

        var activeVisit = allInvitations.FirstOrDefault(i => i.Status == InvitationStatus.Active);

        var isTempAway = false;
        if (activeVisit != null)
        {
            isTempAway = await _unitOfWork.Repository<TemporaryLeave>()
                .GetQueryable()
                .AsNoTracking()
                .AnyAsync(tl => tl.InvitationId == activeVisit.Id && tl.IsActive, ct);
        }

        var isInside = activeVisit != null && !isTempAway;

        return new PersonContextDto
        {
            PersonId = visitorId,
            PersonType = "Visitor",
            FullName = $"{visitor.FirstName} {visitor.LastName}".Trim(),
            Email = visitor.Email,
            PhoneNumber = visitor.PhoneNumber?.Value,
            Company = visitor.Company,
            JobTitle = visitor.JobTitle,
            ProfilePhotoUrl = !string.IsNullOrEmpty(visitor.ProfilePhotoPath)
                ? _urlResolver.GetAbsoluteUrl(visitor.ProfilePhotoPath) : null,
            IsVip = visitor.IsVip,
            IsBlacklisted = visitor.IsBlacklisted,
            BlacklistReason = visitor.BlacklistReason,
            HasFaceEnrollment = hasFaceTemplate,
            IsInsideBuilding = isInside,
            IsTempAway = isTempAway,
            ActiveVisit = activeVisit != null ? MapInvitationSummary(activeVisit) : null,
            TodaysInvitations = todayInvitations
        };
    }

    private async Task<PersonContextDto?> BuildStaffContextAsync(int userId, CancellationToken ct)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, ct);
        if (user == null) return null;

        var hasFaceTemplate = await _unitOfWork.Repository<FaceTemplate>()
            .AnyAsync(t => t.UserId == userId && !t.IsDeleted, ct);

        var staffPresence = await _unitOfWork.Repository<StaffPresence>()
            .GetQueryable()
            .Where(sp => sp.UserId == userId && sp.Status != StaffPresenceStatus.CheckedOut)
            .OrderByDescending(sp => sp.CheckedInAt)
            .FirstOrDefaultAsync(ct);

        bool? isInsideBuilding = staffPresence?.Status == StaffPresenceStatus.Active ? true
            : staffPresence != null ? false  // TemporarilyAbsent
            : (bool?)false;

        return new PersonContextDto
        {
            PersonId = userId,
            PersonType = "Staff",
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            Email = user.Email,
            PhoneNumber = user.PhoneNumber?.Value,
            Company = user.Department,
            JobTitle = user.JobTitle,
            ProfilePhotoUrl = !string.IsNullOrEmpty(user.ProfilePhotoPath)
                ? _urlResolver.GetAbsoluteUrl(user.ProfilePhotoPath) : null,
            IsVip = false,
            IsBlacklisted = false,
            HasFaceEnrollment = hasFaceTemplate,
            IsInsideBuilding = isInsideBuilding,
            TodaysInvitations = new()
        };
    }

    private static InvitationSummaryDto MapInvitationSummary(Domain.Entities.Invitation inv) =>
        new()
        {
            Id = inv.Id,
            InvitationNumber = inv.InvitationNumber,
            Subject = inv.Subject,
            ScheduledStartTime = inv.ScheduledStartTime,
            ScheduledEndTime = inv.ScheduledEndTime,
            Status = inv.Status,
            CheckedInAt = inv.CheckedInAt,
            HostName = inv.Host != null ? $"{inv.Host.FirstName} {inv.Host.LastName}".Trim() : null
        };

    private async Task<CameraFaceEventDto> MapToDtoAsync(
        Domain.Entities.CameraFaceEvent ev,
        CancellationToken cancellationToken)
    {
        var personNames = await BuildPersonNameLookupAsync(new[] { ev }, cancellationToken);
        return MapToDto(ev, personNames);
    }

    private async Task<Dictionary<(string PersonType, int PersonId), string>> BuildPersonNameLookupAsync(
        IEnumerable<Domain.Entities.CameraFaceEvent> events,
        CancellationToken cancellationToken)
    {
        var knownEvents = events
            .Where(ev => ev.IsKnown && ev.PersonId.HasValue)
            .ToList();

        var result = new Dictionary<(string PersonType, int PersonId), string>();
        if (knownEvents.Count == 0)
        {
            return result;
        }

        var visitorIds = knownEvents
            .Where(ev => ev.PersonType.Equals("Visitor", StringComparison.OrdinalIgnoreCase))
            .Select(ev => ev.PersonId!.Value)
            .Distinct()
            .ToList();

        if (visitorIds.Count > 0)
        {
            var visitors = await _unitOfWork.Repository<Visitor>()
                .GetQueryable()
                .AsNoTracking()
                .Where(visitor => visitorIds.Contains(visitor.Id))
                .Select(visitor => new
                {
                    visitor.Id,
                    visitor.FirstName,
                    visitor.LastName
                })
                .ToListAsync(cancellationToken);

            foreach (var visitor in visitors)
            {
                result[("Visitor", visitor.Id)] = BuildDisplayName(visitor.FirstName, visitor.LastName);
            }
        }

        var staffIds = knownEvents
            .Where(ev => ev.PersonType.Equals("Staff", StringComparison.OrdinalIgnoreCase) ||
                         ev.PersonType.Equals("User", StringComparison.OrdinalIgnoreCase))
            .Select(ev => ev.PersonId!.Value)
            .Distinct()
            .ToList();

        if (staffIds.Count > 0)
        {
            var users = await _unitOfWork.Repository<User>()
                .GetQueryable()
                .AsNoTracking()
                .Where(user => staffIds.Contains(user.Id))
                .Select(user => new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName
                })
                .ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                result[("Staff", user.Id)] = BuildDisplayName(user.FirstName, user.LastName);
            }
        }

        return result;
    }

    private CameraFaceEventDto MapToDto(
        Domain.Entities.CameraFaceEvent ev,
        IReadOnlyDictionary<(string PersonType, int PersonId), string>? personNames = null)
    {
        var normalizedPersonType = ev.PersonType.Equals("User", StringComparison.OrdinalIgnoreCase)
            ? "Staff"
            : ev.PersonType;
        var fullName = ev.PersonId.HasValue && personNames != null
            ? personNames.GetValueOrDefault((normalizedPersonType, ev.PersonId.Value))
            : null;

        return new CameraFaceEventDto
        {
            Id = ev.Id,
            EventId = ev.EventId,
            CameraId = ev.CameraId,
            CameraName = ev.CameraName,
            LocationId = ev.LocationId,
            CapturedAt = ev.CapturedAt,
            IsKnown = ev.IsKnown,
            PersonType = ev.PersonType,
            PersonId = ev.PersonId,
            SubjectId = ev.SubjectId,
            FullName = fullName,
            Similarity = ev.Similarity,
            Confidence = ev.Confidence,
            SnapshotUrl = ev.SnapshotPath != null ? _snapshotService.GetSnapshotUrl(ev.SnapshotPath) : null,
            BoundingBoxX = ev.BoundingBoxX,
            BoundingBoxY = ev.BoundingBoxY,
            BoundingBoxWidth = ev.BoundingBoxWidth,
            BoundingBoxHeight = ev.BoundingBoxHeight,
            SuggestedAction = ev.SuggestedAction,
            ReviewStatus = ev.ReviewStatus,
            ReviewedById = ev.ReviewedById,
            ReviewedAt = ev.ReviewedAt,
            ReviewNotes = ev.ReviewNotes,
            IsKnownCandidate = ev.IsKnownCandidate,
            ExpiresAt = ev.ExpiresAt,
            AlertId = ev.AlertId
        };
    }

    private static string BuildDisplayName(string? firstName, string? lastName)
    {
        return $"{firstName ?? string.Empty} {lastName ?? string.Empty}".Trim();
    }

}
