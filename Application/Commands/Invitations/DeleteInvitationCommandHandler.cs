using MediatR;
using System.Linq;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations;

/// <summary>
/// Handler for delete invitation command
/// </summary>
public class DeleteInvitationCommandHandler : IRequestHandler<DeleteInvitationCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteInvitationCommandHandler> _logger;

    public DeleteInvitationCommandHandler(IUnitOfWork unitOfWork, ILogger<DeleteInvitationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteInvitationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing delete invitation command for ID: {Id}", request.Id);

            // Get existing invitation
            var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.Id, cancellationToken);

            if (invitation == null)
            {
                _logger.LogWarning("Invitation not found: {Id}", request.Id);
                throw new InvalidOperationException($"Invitation with ID '{request.Id}' not found.");
            }

            // Check if invitation status allows deletion.
            // Allowed: Cancelled (admin/host), Rejected (host withdrawing), Submitted (host withdrawing before review),
            // Expired (admin cleanup of lapsed invitations).
            var allowedStatuses = new[]
            {
                InvitationStatus.Cancelled,
                InvitationStatus.Rejected,
                InvitationStatus.Submitted,
                InvitationStatus.Expired
            };
            if (!allowedStatuses.Contains(invitation.Status))
            {
                _logger.LogWarning("Cannot delete invitation {Id} - status is {Status}", request.Id, invitation.Status);
                throw new InvalidOperationException(
                    $"Cannot delete invitation with status '{invitation.Status}'. " +
                    $"Only Cancelled, Rejected, Submitted, or Expired invitations can be deleted.");
            }

            // First, delete all invitation events for this invitation
            var invitationEventRepo = _unitOfWork.Repository<InvitationEvent>();
            var invitationEvents = await invitationEventRepo.GetAsync(
                ie => ie.InvitationId == request.Id, 
                cancellationToken);

            if (invitationEvents.Any())
            {
                _logger.LogInformation("Deleting {Count} invitation events for invitation {InvitationId}", 
                    invitationEvents.Count, request.Id);
                invitationEventRepo.RemoveRange(invitationEvents);
            }

            // Then delete the invitation
            _unitOfWork.Invitations.Remove(invitation);

            // Save all changes in a single transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted invitation {InvitationId} and {EventCount} related events by user {UserId}", 
                request.Id, invitationEvents.Count, request.DeletedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invitation with ID: {Id}", request.Id);
            throw;
        }
    }
}
