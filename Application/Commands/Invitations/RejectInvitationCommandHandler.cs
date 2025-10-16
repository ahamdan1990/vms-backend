using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations;

/// <summary>
/// Handler for reject invitation command
/// </summary>
public class RejectInvitationCommandHandler : IRequestHandler<RejectInvitationCommand, InvitationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<RejectInvitationCommandHandler> _logger;

    public RejectInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<RejectInvitationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<InvitationDto> Handle(RejectInvitationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing reject invitation command for invitation: {InvitationId}", request.InvitationId);

            // Get existing invitation
            var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.InvitationId, cancellationToken);
            if (invitation == null)
            {
                throw new InvalidOperationException($"Invitation with ID '{request.InvitationId}' not found.");
            }

            // Validate rejector exists
            var rejector = await _unitOfWork.Users.GetByIdAsync(request.RejectedBy, cancellationToken);
            if (rejector == null)
            {
                throw new InvalidOperationException($"Rejector with ID '{request.RejectedBy}' not found.");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Reject the invitation
                invitation.Reject(request.RejectedBy, request.Reason);
                _unitOfWork.Invitations.Update(invitation);

                // Create rejection event
                var rejectionEvent = InvitationEvent.Create(
                    invitation.Id,
                    InvitationEventTypes.Rejected,
                    $"Invitation rejected by {rejector.FullName}: {request.Reason}",
                    request.RejectedBy,
                    request.Reason
                );
                await _unitOfWork.Repository<InvitationEvent>().AddAsync(rejectionEvent, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Invitation rejected successfully: {InvitationId} by {RejectedBy} with reason: {Reason}",
                    request.InvitationId, request.RejectedBy, request.Reason);

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
            _logger.LogError(ex, "Error rejecting invitation: {InvitationId}", request.InvitationId);
            throw;
        }
    }
}
