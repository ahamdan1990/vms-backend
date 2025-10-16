using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations;

/// <summary>
/// Handler for submit invitation command
/// </summary>
public class SubmitInvitationCommandHandler : IRequestHandler<SubmitInvitationCommand, InvitationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<SubmitInvitationCommandHandler> _logger;

    public SubmitInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<SubmitInvitationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<InvitationDto> Handle(SubmitInvitationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing submit invitation command for invitation: {InvitationId}", request.InvitationId);

            // Get existing invitation
            var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.InvitationId, cancellationToken);
            if (invitation == null)
            {
                throw new InvalidOperationException($"Invitation with ID '{request.InvitationId}' not found.");
            }

            // Validate submitter exists
            var submitter = await _unitOfWork.Users.GetByIdAsync(request.SubmittedBy, cancellationToken);
            if (submitter == null)
            {
                throw new InvalidOperationException($"User with ID '{request.SubmittedBy}' not found.");
            }

            // Validate invitation data before submission
            var validationErrors = invitation.ValidateInvitation();
            if (validationErrors.Any())
            {
                throw new InvalidOperationException($"Invitation validation failed: {string.Join(", ", validationErrors)}");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Submit the invitation
                invitation.Submit(request.SubmittedBy);
                _unitOfWork.Invitations.Update(invitation);

                // Create submission event
                var submissionEvent = InvitationEvent.Create(
                    invitation.Id,
                    InvitationEventTypes.Submitted,
                    $"Invitation submitted for approval by {submitter.FullName}",
                    request.SubmittedBy
                );
                await _unitOfWork.Repository<InvitationEvent>().AddAsync(submissionEvent, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Invitation submitted successfully: {InvitationId} by {SubmittedBy}",
                    request.InvitationId, request.SubmittedBy);

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
            _logger.LogError(ex, "Error submitting invitation: {InvitationId}", request.InvitationId);
            throw;
        }
    }
}
