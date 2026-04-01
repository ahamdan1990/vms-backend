using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Application.Services.Notifications;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations;

/// <summary>
/// Handler for requesting host consent for a late check-in on an expired invitation.
/// Does NOT change invitation status — only records the request event and notifies the host.
/// </summary>
public class RequestLateCheckInCommandHandler : IRequestHandler<RequestLateCheckInCommand, InvitationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<RequestLateCheckInCommandHandler> _logger;
    private readonly INotificationService _notificationService;

    public RequestLateCheckInCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<RequestLateCheckInCommandHandler> logger,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<InvitationDto> Handle(RequestLateCheckInCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing late check-in consent request for invitation {InvitationId}", request.InvitationId);

        var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.InvitationId, cancellationToken);
        if (invitation == null)
            throw new InvalidOperationException($"Invitation with ID '{request.InvitationId}' not found.");

        if (invitation.Status != InvitationStatus.Expired)
            throw new InvalidOperationException(
                $"Late check-in consent can only be requested for expired invitations. Current status: {invitation.Status}.");

        var requester = await _unitOfWork.Users.GetByIdAsync(request.RequestedBy, cancellationToken);
        if (requester == null)
            throw new InvalidOperationException($"User with ID '{request.RequestedBy}' not found.");

        // Record the request event (no status change)
        var eventData = !string.IsNullOrEmpty(request.Notes)
            ? $"{{\"notes\": \"{request.Notes}\"}}"
            : null;

        var lateCheckInEvent = InvitationEvent.Create(
            invitation.Id,
            InvitationEventTypes.LateCheckInRequested,
            $"Late check-in consent requested by {requester.FullName}",
            request.RequestedBy,
            eventData
        );
        await _unitOfWork.Repository<InvitationEvent>().AddAsync(lateCheckInEvent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Late check-in consent requested for invitation {InvitationId} by {RequestedBy}",
            invitation.Id, request.RequestedBy);

        // Notify the host
        try
        {
            var visitorName = invitation.Visitor?.FullName ?? "visitor";
            var scheduledTime = invitation.ScheduledStartTime.ToLocalTime().ToString("MM/dd/yyyy 'at' h:mm tt");
            var notesText = !string.IsNullOrEmpty(request.Notes) ? $" Notes: {request.Notes}" : string.Empty;

            await _notificationService.NotifyUserAsync(
                invitation.HostId,
                "Late Check-In Consent Requested",
                $"{requester.FullName} is requesting your consent to check in {visitorName}, " +
                $"whose invitation (scheduled {scheduledTime}) has expired.{notesText} " +
                $"Please advise the reception desk.",
                NotificationAlertType.ManualOverride,
                AlertPriority.High,
                new
                {
                    invitationId = invitation.Id,
                    invitationNumber = invitation.InvitationNumber,
                    requestedBy = request.RequestedBy,
                    requestedByName = requester.FullName
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send late check-in consent notification to host {HostId} for invitation {InvitationId}",
                invitation.HostId, invitation.Id);
            // Don't fail the request if notification fails
        }

        var updatedInvitation = await _unitOfWork.Invitations.GetByIdAsync(invitation.Id, cancellationToken);
        return _mapper.Map<InvitationDto>(updatedInvitation);
    }
}
