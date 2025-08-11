using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations
{
    /// <summary>
    /// Handler for check-in invitation command
    /// </summary>
    public class CheckInInvitationCommandHandler : IRequestHandler<CheckInInvitationCommand, InvitationDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CheckInInvitationCommandHandler> _logger;

        public CheckInInvitationCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CheckInInvitationCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<InvitationDto> Handle(CheckInInvitationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing check-in invitation command for reference: {InvitationReference}", request.InvitationReference);

                // Find invitation by reference (ID or QR code)
                var invitation = await FindInvitationByReferenceAsync(request.InvitationReference, cancellationToken);
                if (invitation == null)
                {
                    throw new InvalidOperationException($"Invitation with reference '{request.InvitationReference}' not found.");
                }

                // Validate operator exists
                var operatorUser = await _unitOfWork.Users.GetByIdAsync(request.CheckedInBy, cancellationToken);
                if (operatorUser == null)
                {
                    throw new InvalidOperationException($"Operator with ID '{request.CheckedInBy}' not found.");
                }

                using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Check in the visitor
                    invitation.CheckIn(request.CheckedInBy);
                    _unitOfWork.Invitations.Update(invitation);

                    // Update visitor statistics
                    await _unitOfWork.Visitors.UpdateVisitStatisticsAsync(invitation.VisitorId, cancellationToken);

                    // Create check-in event
                    var checkInEvent = InvitationEvent.Create(
                        invitation.Id,
                        InvitationEventTypes.CheckedIn,
                        $"Visitor checked in by {operatorUser.FullName}",
                        request.CheckedInBy,
                        !string.IsNullOrEmpty(request.Notes) ? $"{{\"notes\": \"{request.Notes}\"}}" : null
                    );
                    await _unitOfWork.Repository<InvitationEvent>().AddAsync(checkInEvent, cancellationToken);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    _logger.LogInformation("Invitation checked-in successfully: {InvitationId} ({InvitationNumber}) by {CheckedInBy}",
                        invitation.Id, invitation.InvitationNumber, request.CheckedInBy);

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
                _logger.LogError(ex, "Error checking-in invitation with reference: {InvitationReference}", request.InvitationReference);
                throw;
            }
        }

        private async Task<Invitation?> FindInvitationByReferenceAsync(string reference, CancellationToken cancellationToken)
        {
            // Try to parse as invitation ID first
            if (int.TryParse(reference, out var invitationId))
            {
                var invitationById = await _unitOfWork.Invitations.GetByIdAsync(invitationId, cancellationToken);
                if (invitationById != null)
                    return invitationById;
            }

            // Try to find by invitation number
            var invitationByNumber = await _unitOfWork.Invitations.GetByInvitationNumberAsync(reference, cancellationToken);
            if (invitationByNumber != null)
                return invitationByNumber;

            // Try to find by QR code data
            var invitationByQr = await _unitOfWork.Invitations.GetByQrCodeAsync(reference, cancellationToken);
            if (invitationByQr != null)
                return invitationByQr;

            return null;
        }
    }
}
