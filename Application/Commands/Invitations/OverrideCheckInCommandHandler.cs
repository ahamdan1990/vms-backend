using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Application.Services.Notifications;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations;

/// <summary>
/// Handler for force check-in override on an expired invitation (admin/operator use only).
/// Bypasses timing and status guards to allow a late check-in when authorized.
/// </summary>
public class OverrideCheckInCommandHandler : IRequestHandler<OverrideCheckInCommand, InvitationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<OverrideCheckInCommandHandler> _logger;
    private readonly INotificationService _notificationService;

    public OverrideCheckInCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<OverrideCheckInCommandHandler> logger,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<InvitationDto> Handle(OverrideCheckInCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing override check-in for invitation {InvitationId} by {OverriddenBy}",
            request.InvitationId, request.OverriddenBy);

        var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.InvitationId, cancellationToken);
        if (invitation == null)
            throw new InvalidOperationException($"Invitation with ID '{request.InvitationId}' not found.");

        var overrider = await _unitOfWork.Users.GetByIdAsync(request.OverriddenBy, cancellationToken);
        if (overrider == null)
            throw new InvalidOperationException($"User with ID '{request.OverriddenBy}' not found.");

        using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Force check-in regardless of timing/status (domain method validates only Expired or Approved)
            invitation.ForceCheckIn(request.OverriddenBy);
            _unitOfWork.Invitations.Update(invitation);

            // Update visitor statistics
            await _unitOfWork.Visitors.UpdateVisitStatisticsAsync(invitation.VisitorId, cancellationToken);

            // Record the override event
            var notesJson = !string.IsNullOrEmpty(request.Notes)
                ? $"{{\"notes\": \"{request.Notes}\", \"override\": true}}"
                : "{\"override\": true}";

            var overrideEvent = InvitationEvent.Create(
                invitation.Id,
                InvitationEventTypes.LateCheckInOverride,
                $"Late check-in override performed by {overrider.FullName}",
                request.OverriddenBy,
                notesJson
            );
            await _unitOfWork.Repository<InvitationEvent>().AddAsync(overrideEvent, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Override check-in completed for invitation {InvitationId} ({InvitationNumber}) by {OverriddenBy}",
                invitation.Id, invitation.InvitationNumber, request.OverriddenBy);

            // Notify host that visitor was checked in via override
            try
            {
                var visitorName = invitation.Visitor?.FullName ?? "visitor";
                var notesText = !string.IsNullOrEmpty(request.Notes) ? $" Notes: {request.Notes}" : string.Empty;

                await _notificationService.NotifyUserAsync(
                    invitation.HostId,
                    "Late Check-In Override Performed",
                    $"{visitorName} has been checked in by {overrider.FullName} via late check-in override for expired invitation #{invitation.InvitationNumber}.{notesText}",
                    NotificationAlertType.ManualOverride,
                    AlertPriority.High,
                    new
                    {
                        invitationId = invitation.Id,
                        invitationNumber = invitation.InvitationNumber,
                        overriddenBy = request.OverriddenBy,
                        overriddenByName = overrider.FullName
                    },
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify host {HostId} of override check-in for invitation {InvitationId}",
                    invitation.HostId, invitation.Id);
            }

            var updatedInvitation = await _unitOfWork.Invitations.GetByIdAsync(invitation.Id, cancellationToken);
            return _mapper.Map<InvitationDto>(updatedInvitation);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
