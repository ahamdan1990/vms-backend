using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations;

/// <summary>
/// Handler for update invitation command
/// </summary>
public class UpdateInvitationCommandHandler : IRequestHandler<UpdateInvitationCommand, InvitationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateInvitationCommandHandler> _logger;

    public UpdateInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateInvitationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<InvitationDto> Handle(UpdateInvitationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing update invitation command for invitation: {InvitationId}", request.Id);

            // Get existing invitation
            var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.Id, cancellationToken);
            if (invitation == null)
            {
                throw new InvalidOperationException($"Invitation with ID '{request.Id}' not found.");
            }

            // Check if invitation can be modified
            if (!invitation.CanBeModified)
            {
                throw new InvalidOperationException($"Invitation with status '{invitation.Status}' cannot be modified.");
            }

            // Validate modifier exists
            var modifier = await _unitOfWork.Users.GetByIdAsync(request.ModifiedBy, cancellationToken);
            if (modifier == null)
            {
                throw new InvalidOperationException($"User with ID '{request.ModifiedBy}' not found.");
            }

            // Validate visit purpose if specified
            if (request.VisitPurposeId.HasValue)
            {
                var visitPurpose = await _unitOfWork.VisitPurposes.GetByIdAsync(request.VisitPurposeId.Value, cancellationToken);
                if (visitPurpose == null)
                {
                    throw new InvalidOperationException($"Visit purpose with ID '{request.VisitPurposeId}' not found.");
                }
            }

            // Validate location if specified
            if (request.LocationId.HasValue)
            {
                var location = await _unitOfWork.Locations.GetByIdAsync(request.LocationId.Value, cancellationToken);
                if (location == null)
                {
                    throw new InvalidOperationException($"Location with ID '{request.LocationId}' not found.");
                }
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Capture original values for change tracking
                var originalSubject = invitation.Subject;
                var originalStartTime = invitation.ScheduledStartTime;
                var originalEndTime = invitation.ScheduledEndTime;

                // Update invitation properties
                invitation.VisitPurposeId = request.VisitPurposeId;
                invitation.LocationId = request.LocationId;
                invitation.Type = request.Type;
                invitation.Subject = request.Subject.Trim();
                invitation.Message = request.Message?.Trim();
                invitation.ScheduledStartTime = request.ScheduledStartTime;
                invitation.ScheduledEndTime = request.ScheduledEndTime;
                invitation.ExpectedVisitorCount = request.ExpectedVisitorCount;
                invitation.SpecialInstructions = request.SpecialInstructions?.Trim();
                invitation.RequiresApproval = request.RequiresApproval;
                invitation.RequiresEscort = request.RequiresEscort;
                invitation.RequiresBadge = request.RequiresBadge;
                invitation.NeedsParking = request.NeedsParking;
                invitation.ParkingInstructions = request.ParkingInstructions?.Trim();

                // Validate updated invitation data
                var validationErrors = invitation.ValidateInvitation();
                if (validationErrors.Any())
                {
                    throw new InvalidOperationException($"Invitation validation failed: {string.Join(", ", validationErrors)}");
                }

                // Update audit information
                invitation.UpdateModifiedBy(request.ModifiedBy);
                _unitOfWork.Invitations.Update(invitation);

                // Create modification event with change details
                var changes = BuildChangeDescription(originalSubject, originalStartTime, originalEndTime, request);
                var modificationEvent = InvitationEvent.Create(
                    invitation.Id,
                    InvitationEventTypes.Modified,
                    $"Invitation modified by {modifier.FullName}",
                    request.ModifiedBy,
                    !string.IsNullOrEmpty(changes) ? $"{{\"changes\": \"{changes}\"}}" : null
                );
                await _unitOfWork.Repository<InvitationEvent>().AddAsync(modificationEvent, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Invitation updated successfully: {InvitationId} by {ModifiedBy}",
                    request.Id, request.ModifiedBy);

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
            _logger.LogError(ex, "Error updating invitation: {InvitationId}", request.Id);
            throw;
        }
    }

    private static string BuildChangeDescription(string originalSubject, DateTime originalStartTime, DateTime originalEndTime, UpdateInvitationCommand request)
    {
        var changes = new List<string>();

        if (originalSubject != request.Subject)
            changes.Add($"Subject: '{originalSubject}' → '{request.Subject}'");

        if (originalStartTime != request.ScheduledStartTime)
            changes.Add($"Start time: {originalStartTime:yyyy-MM-dd HH:mm} → {request.ScheduledStartTime:yyyy-MM-dd HH:mm}");

        if (originalEndTime != request.ScheduledEndTime)
            changes.Add($"End time: {originalEndTime:yyyy-MM-dd HH:mm} → {request.ScheduledEndTime:yyyy-MM-dd HH:mm}");

        return string.Join(", ", changes);
    }
}
