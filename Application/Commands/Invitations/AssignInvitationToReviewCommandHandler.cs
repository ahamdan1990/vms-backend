using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Application.Services.Notifications;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations;

/// <summary>
/// Handler for assign invitation to review command (admin only)
/// </summary>
public class AssignInvitationToReviewCommandHandler : IRequestHandler<AssignInvitationToReviewCommand, InvitationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AssignInvitationToReviewCommandHandler> _logger;
    private readonly INotificationService _notificationService;

    public AssignInvitationToReviewCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<AssignInvitationToReviewCommandHandler> logger,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<InvitationDto> Handle(AssignInvitationToReviewCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing assign-to-review command for invitation: {InvitationId}", request.InvitationId);

            var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.InvitationId, cancellationToken);
            if (invitation == null)
                throw new InvalidOperationException($"Invitation with ID '{request.InvitationId}' not found.");

            var reviewer = await _unitOfWork.Users.GetByIdAsync(request.AssignedBy, cancellationToken);
            if (reviewer == null)
                throw new InvalidOperationException($"User with ID '{request.AssignedBy}' not found.");

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Transition to UnderReview (only from Submitted)
                invitation.AssignToReview(request.AssignedBy);
                _unitOfWork.Invitations.Update(invitation);

                // Record the event
                var reviewEvent = InvitationEvent.Create(
                    invitation.Id,
                    InvitationEventTypes.UnderReview,
                    $"Invitation assigned for review by {reviewer.FullName}",
                    request.AssignedBy
                );
                await _unitOfWork.Repository<InvitationEvent>().AddAsync(reviewEvent, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Invitation {InvitationId} assigned for review by {AssignedBy}",
                    request.InvitationId, request.AssignedBy);

                // Notify host that their invitation is under review
                try
                {
                    await _notificationService.NotifyInvitationUnderReviewAsync(
                        invitation.Id, invitation.HostId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending under-review notification for invitation {InvitationId}", invitation.Id);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning invitation {InvitationId} to review", request.InvitationId);
            throw;
        }
    }
}
