using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Cameras;
using VisitorManagementSystem.Api.Application.Services.Cameras;
using VisitorManagementSystem.Api.Application.Services.Common;
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
    private readonly ILogger<CameraFaceEventsController> _logger;

    public CameraFaceEventsController(
        IUnitOfWork unitOfWork,
        IFaceSnapshotService snapshotService,
        IUrlResolverService urlResolver,
        ILogger<CameraFaceEventsController> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
        _urlResolver = urlResolver ?? throw new ArgumentNullException(nameof(urlResolver));
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

        return Ok(new CameraFaceEventPagedDto
        {
            Events = events.Select(MapToDto).ToList(),
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

        return Ok(MapToDto(ev));
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

        return Ok(MapToDto(ev));
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

        _logger.LogInformation(
            "Face event {EventId} reviewed. Status={Status}, ReviewedBy={UserId}",
            ev.EventId, request.Status, ev.ReviewedById);

        return Ok(MapToDto(ev));
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

        var result = new CameraFaceEventContextDto { FaceEvent = MapToDto(ev) };

        if (ev.IsKnown && ev.PersonId.HasValue)
        {
            result.Person = ev.PersonType == "Staff"
                ? await BuildStaffContextAsync(ev.PersonId.Value, cancellationToken)
                : await BuildVisitorContextAsync(ev.PersonId.Value, cancellationToken);
        }

        return Ok(result);
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
            IsInsideBuilding = null, // Staff presence not tracked in VMS
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

    private CameraFaceEventDto MapToDto(Domain.Entities.CameraFaceEvent ev)
    {
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

}
