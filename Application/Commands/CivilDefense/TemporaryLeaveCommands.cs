using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.Services.Cameras;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.CivilDefense;

// ─── Staff Temp Leave ────────────────────────────────────────────────────────

public class StaffTempLeaveCommand : IRequest
{
    public int StaffPresenceId { get; set; }
    public string? Reason { get; set; }
    public int RecordedById { get; set; }
}

public class StaffTempLeaveCommandHandler : IRequestHandler<StaffTempLeaveCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICameraFaceEventService _faceEventService;

    public StaffTempLeaveCommandHandler(IUnitOfWork unitOfWork, ICameraFaceEventService faceEventService)
    {
        _unitOfWork = unitOfWork;
        _faceEventService = faceEventService;
    }

    public async Task Handle(StaffTempLeaveCommand request, CancellationToken cancellationToken)
    {
        var presence = await _unitOfWork.Repository<StaffPresence>()
            .GetByIdAsync(request.StaffPresenceId, cancellationToken)
            ?? throw new KeyNotFoundException($"StaffPresence {request.StaffPresenceId} not found.");

        if (presence.Status != StaffPresenceStatus.Active)
            throw new InvalidOperationException("Staff member is not currently active inside the building.");

        presence.Status = StaffPresenceStatus.TemporarilyAbsent;
        presence.ModifiedOn = DateTime.UtcNow;

        var leave = new TemporaryLeave
        {
            PersonType = TemporaryLeavePersonType.Staff,
            StaffPresenceId = request.StaffPresenceId,
            LeftAt = DateTime.UtcNow,
            IsActive = true,
            Reason = request.Reason?.Trim(),
            RecordedById = request.RecordedById,
            RecordedByReferenceId = request.RecordedById
        };

        await _unitOfWork.Repository<TemporaryLeave>().AddAsync(leave, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _faceEventService.AutoReviewPendingEventsForPersonAsync("Staff", presence.UserId, cancellationToken);
    }
}

// ─── Staff Temp Return ───────────────────────────────────────────────────────

public class StaffTempReturnCommand : IRequest
{
    public int StaffPresenceId { get; set; }
    public int ReturnedById { get; set; }
}

public class StaffTempReturnCommandHandler : IRequestHandler<StaffTempReturnCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICameraFaceEventService _faceEventService;

    public StaffTempReturnCommandHandler(IUnitOfWork unitOfWork, ICameraFaceEventService faceEventService)
    {
        _unitOfWork = unitOfWork;
        _faceEventService = faceEventService;
    }

    public async Task Handle(StaffTempReturnCommand request, CancellationToken cancellationToken)
    {
        var presence = await _unitOfWork.Repository<StaffPresence>()
            .GetByIdAsync(request.StaffPresenceId, cancellationToken)
            ?? throw new KeyNotFoundException($"StaffPresence {request.StaffPresenceId} not found.");

        if (presence.Status != StaffPresenceStatus.TemporarilyAbsent)
            throw new InvalidOperationException("Staff member is not marked as temporarily absent.");

        presence.Status = StaffPresenceStatus.Active;
        presence.ModifiedOn = DateTime.UtcNow;

        // Close the active leave record
        var activeLeave = await _unitOfWork.Repository<TemporaryLeave>()
            .GetQueryable()
            .FirstOrDefaultAsync(
                tl => tl.StaffPresenceId == request.StaffPresenceId && tl.IsActive,
                cancellationToken);

        if (activeLeave != null)
        {
            activeLeave.ReturnedAt = DateTime.UtcNow;
            activeLeave.ReturnedById = request.ReturnedById;
            activeLeave.IsActive = false;
            activeLeave.ModifiedOn = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _faceEventService.AutoReviewPendingEventsForPersonAsync("Staff", presence.UserId, cancellationToken);
    }
}

// ─── Visitor Temp Leave ──────────────────────────────────────────────────────

public class VisitorTempLeaveCommand : IRequest
{
    public int InvitationId { get; set; }
    public string? Reason { get; set; }
    public int RecordedById { get; set; }
}

public class VisitorTempLeaveCommandHandler : IRequestHandler<VisitorTempLeaveCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICameraFaceEventService _faceEventService;

    public VisitorTempLeaveCommandHandler(IUnitOfWork unitOfWork, ICameraFaceEventService faceEventService)
    {
        _unitOfWork = unitOfWork;
        _faceEventService = faceEventService;
    }

    public async Task Handle(VisitorTempLeaveCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Invitation {request.InvitationId} not found.");

        if (invitation.Status != InvitationStatus.Active)
            throw new InvalidOperationException("Visitor is not currently active inside the building.");

        // Check for existing active leave (prevent duplicates)
        var existing = await _unitOfWork.Repository<TemporaryLeave>()
            .GetQueryable()
            .AnyAsync(tl => tl.InvitationId == request.InvitationId && tl.IsActive, cancellationToken);

        if (existing)
            throw new InvalidOperationException("Visitor already has an active temporary leave.");

        var leave = new TemporaryLeave
        {
            PersonType = TemporaryLeavePersonType.Visitor,
            InvitationId = request.InvitationId,
            LeftAt = DateTime.UtcNow,
            IsActive = true,
            Reason = request.Reason?.Trim(),
            RecordedById = request.RecordedById,
            RecordedByReferenceId = request.RecordedById
        };

        await _unitOfWork.Repository<TemporaryLeave>().AddAsync(leave, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _faceEventService.AutoReviewPendingEventsForPersonAsync("Visitor", invitation.VisitorId, cancellationToken);
    }
}

// ─── Visitor Temp Return ─────────────────────────────────────────────────────

public class VisitorTempReturnCommand : IRequest
{
    public int InvitationId { get; set; }
    public int ReturnedById { get; set; }
}

public class VisitorTempReturnCommandHandler : IRequestHandler<VisitorTempReturnCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICameraFaceEventService _faceEventService;

    public VisitorTempReturnCommandHandler(IUnitOfWork unitOfWork, ICameraFaceEventService faceEventService)
    {
        _unitOfWork = unitOfWork;
        _faceEventService = faceEventService;
    }

    public async Task Handle(VisitorTempReturnCommand request, CancellationToken cancellationToken)
    {
        var activeLeave = await _unitOfWork.Repository<TemporaryLeave>()
            .GetQueryable()
            .FirstOrDefaultAsync(
                tl => tl.InvitationId == request.InvitationId && tl.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException("No active temporary leave found for this visitor.");

        activeLeave.ReturnedAt = DateTime.UtcNow;
        activeLeave.ReturnedById = request.ReturnedById;
        activeLeave.IsActive = false;
        activeLeave.ModifiedOn = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.InvitationId, cancellationToken);
        if (invitation != null)
            await _faceEventService.AutoReviewPendingEventsForPersonAsync("Visitor", invitation.VisitorId, cancellationToken);
    }
}
