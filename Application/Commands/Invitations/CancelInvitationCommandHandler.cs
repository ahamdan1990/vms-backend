using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations;

/// <summary>
/// Handler for cancel invitation command
/// </summary>
public class CancelInvitationCommandHandler : IRequestHandler<CancelInvitationCommand, InvitationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CancelInvitationCommandHandler> _logger;

    public CancelInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CancelInvitationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<InvitationDto> Handle(CancelInvitationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing cancel invitation command for invitation: {InvitationId}", request.InvitationId);

            // Get existing invitation
            var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.InvitationId, cancellationToken);
            if (invitation == null)
            {
                throw new InvalidOperationException($"Invitation with ID '{request.InvitationId}' not found.");
            }

            // Validate canceller exists
            var canceller = await _unitOfWork.Users.GetByIdAsync(request.CancelledBy, cancellationToken);
            if (canceller == null)
            {
                throw new InvalidOperationException($"User with ID '{request.CancelledBy}' not found.");
            }

            // Check if invitation can be cancelled
            if (!invitation.CanBeCancelled)
            {
                throw new InvalidOperationException($"Invitation with status '{invitation.Status}' cannot be cancelled.");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Cancel the invitation
                invitation.Cancel(request.CancelledBy);
                _unitOfWork.Invitations.Update(invitation);

                // Create cancellation event
                var cancellationEvent = InvitationEvent.Create(
                    invitation.Id,
                    InvitationEventTypes.Cancelled,
                    $"Invitation cancelled by {canceller.FullName}",
                    request.CancelledBy,
                    !string.IsNullOrEmpty(request.Reason) ? $"{{\"reason\": \"{request.Reason}\"}}" : null
                );
                await _unitOfWork.Repository<InvitationEvent>().AddAsync(cancellationEvent, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Invitation cancelled successfully: {InvitationId} by {CancelledBy}",
                    request.InvitationId, request.CancelledBy);

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
            _logger.LogError(ex, "Error cancelling invitation: {InvitationId}", request.InvitationId);
            throw;
        }
    }
}
