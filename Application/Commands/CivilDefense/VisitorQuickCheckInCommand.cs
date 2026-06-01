using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.Services.Cameras;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Application.Commands.CivilDefense;

public class VisitorQuickCheckInRequest
{
    public int? VisitorId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public bool? IsCivilian { get; set; }
    public string? AffiliatedOrganization { get; set; }
    public int HostUserId { get; set; }
    public int? VisitPurposeId { get; set; }   // preferred — resolves to name automatically
    public string? Purpose { get; set; }         // fallback free text
    public int? LocationId { get; set; }
    public string? Notes { get; set; }
}

public record VisitorQuickCheckInResult(int VisitorId, int InvitationId);

public class VisitorQuickCheckInCommand : IRequest<VisitorQuickCheckInResult>
{
    public VisitorQuickCheckInRequest Data { get; set; } = new();
    public int RegisteredById { get; set; }
}

public class VisitorQuickCheckInCommandHandler : IRequestHandler<VisitorQuickCheckInCommand, VisitorQuickCheckInResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICameraFaceEventService _faceEventService;
    private readonly ILogger<VisitorQuickCheckInCommandHandler> _logger;

    public VisitorQuickCheckInCommandHandler(
        IUnitOfWork unitOfWork,
        ICameraFaceEventService faceEventService,
        ILogger<VisitorQuickCheckInCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _faceEventService = faceEventService;
        _logger = logger;
    }

    public async Task<VisitorQuickCheckInResult> Handle(VisitorQuickCheckInCommand request, CancellationToken cancellationToken)
    {
        var data = request.Data;
        int visitorId;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Resolve or create visitor
            if (data.VisitorId.HasValue)
            {
                var existing = await _unitOfWork.Visitors.GetByIdAsync(data.VisitorId.Value, cancellationToken)
                    ?? throw new KeyNotFoundException($"Visitor {data.VisitorId} not found.");
                visitorId = existing.Id;
            }
            else
            {
                // Create new visitor with minimal fields
                var phone = data.Phone?.Trim();
                var placeholderEmail = $"cd-{Guid.NewGuid():N}@walkin.local";

                var visitor = new Visitor
                {
                    FirstName = (data.FirstName ?? "").Trim(),
                    LastName = (data.LastName ?? "").Trim(),
                    Email = new Email(placeholderEmail),
                    Company = data.Company?.Trim(),
                    IsActive = true
                };

                if (!string.IsNullOrWhiteSpace(phone) && PhoneNumber.IsValidPhoneNumber(phone))
                    visitor.PhoneNumber = new PhoneNumber(phone);

                visitor.UpdateNormalizedEmail();
                visitor.SetCreatedBy(request.RegisteredById);

                await _unitOfWork.Visitors.AddAsync(visitor, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                visitorId = visitor.Id;
            }

            // Block duplicate active check-in
            var activeVisit = await _unitOfWork.Invitations
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    i => i.VisitorId == visitorId && i.Status == InvitationStatus.Active && !i.IsDeleted,
                    cancellationToken);

            if (activeVisit != null)
                throw new InvalidOperationException("This visitor is already inside the building.");

            // Resolve host
            var host = await _unitOfWork.Users.GetByIdAsync(data.HostUserId, cancellationToken)
                ?? throw new KeyNotFoundException($"Host user {data.HostUserId} not found.");

            // Resolve visit purpose
            string? purposeName = data.Purpose?.Trim();
            int? visitPurposeId = data.VisitPurposeId;
            if (visitPurposeId.HasValue)
            {
                var purpose = await _unitOfWork.VisitPurposes.GetByIdAsync(visitPurposeId.Value, cancellationToken);
                if (purpose != null) purposeName = purpose.Name;
            }

            var now = DateTime.UtcNow;
            var invitation = new Invitation
            {
                InvitationNumber = Invitation.GenerateInvitationNumber(),
                VisitorId = visitorId,
                HostId = data.HostUserId,
                Type = InvitationType.WalkIn,
                Status = InvitationStatus.Active,
                Subject = purposeName ?? "Walk-in Visit",
                Message = data.Notes?.Trim(),
                ScheduledStartTime = now,
                ScheduledEndTime = now.AddHours(8),
                RequiresApproval = false,
                LocationId = data.LocationId,
                VisitPurposeId = visitPurposeId,
                CheckedInAt = now,
                IsCivilian = data.IsCivilian,
                AffiliatedOrganization = data.AffiliatedOrganization?.Trim()
            };

            invitation.SetCreatedBy(request.RegisteredById);

            await _unitOfWork.Invitations.AddAsync(invitation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            await _faceEventService.AutoReviewPendingEventsForPersonAsync("Visitor", visitorId, cancellationToken);

            _logger.LogInformation("CD visitor quick check-in: visitorId={VisitorId} invitationId={InvitationId}",
                visitorId, invitation.Id);
            return new VisitorQuickCheckInResult(visitorId, invitation.Id);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
