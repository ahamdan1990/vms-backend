using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Application.Services.Notifications;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations
{
    /// <summary>
    /// Handler for check-out invitation command
    /// </summary>
    public class CheckOutInvitationCommandHandler : IRequestHandler<CheckOutInvitationCommand, InvitationDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CheckOutInvitationCommandHandler> _logger;
        private readonly INotificationService _notificationService;

        public CheckOutInvitationCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CheckOutInvitationCommandHandler> logger,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<InvitationDto> Handle(CheckOutInvitationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing check-out invitation command for invitation: {InvitationId}", request.InvitationId);

                // Get existing invitation
                var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.InvitationId, cancellationToken);
                if (invitation == null)
                {
                    throw new InvalidOperationException($"Invitation with ID '{request.InvitationId}' not found.");
                }

                // Validate operator exists
                var operatorUser = await _unitOfWork.Users.GetByIdAsync(request.CheckedOutBy, cancellationToken);
                if (operatorUser == null)
                {
                    throw new InvalidOperationException($"Operator with ID '{request.CheckedOutBy}' not found.");
                }

                using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Check out the visitor
                    invitation.CheckOut(request.CheckedOutBy);
                    _unitOfWork.Invitations.Update(invitation);

                    // Create check-out event
                    var checkOutEvent = InvitationEvent.Create(
                        invitation.Id,
                        InvitationEventTypes.CheckedOut,
                        $"Visitor checked out by {operatorUser.FullName}",
                        request.CheckedOutBy,
                        !string.IsNullOrEmpty(request.Notes) ? $"{{\"notes\": \"{request.Notes}\"}}" : null
                    );
                    await _unitOfWork.Repository<InvitationEvent>().AddAsync(checkOutEvent, cancellationToken);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    _logger.LogInformation("Invitation checked-out successfully: {InvitationId} ({InvitationNumber}) by {CheckedOutBy}",
                        invitation.Id, invitation.InvitationNumber, request.CheckedOutBy);

                    // Notify host of check-out
                    try
                    {
                        var visitorName = invitation.Visitor != null
                            ? $"{invitation.Visitor.FirstName} {invitation.Visitor.LastName}"
                            : "Unknown Visitor";
                        await _notificationService.NotifyVisitorCheckOutAsync(
                            invitation.HostId,
                            invitation.VisitorId,
                            visitorName,
                            invitation.CheckedOutAt ?? DateTime.UtcNow,
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending check-out notification for invitation {InvitationId}", invitation.Id);
                    }

                    // Return updated invitation DTO
                    var updatedInvitation = await _unitOfWork.Invitations.GetByIdAsync(invitation.Id, cancellationToken);
                    var invitationDto = _mapper.Map<InvitationDto>(updatedInvitation);
                    return invitationDto;
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking-out invitation: {InvitationId}", request.InvitationId);
                throw;
            }
        }
    }
}
