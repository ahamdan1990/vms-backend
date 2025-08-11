using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.EmergencyContacts;

/// <summary>
/// Handler for delete emergency contact command
/// </summary>
public class DeleteEmergencyContactCommandHandler : IRequestHandler<DeleteEmergencyContactCommand, CommandResultDto<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteEmergencyContactCommandHandler> _logger;

    public DeleteEmergencyContactCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteEmergencyContactCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CommandResultDto<bool>> Handle(DeleteEmergencyContactCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing delete emergency contact command for ID: {ContactId}", request.Id);

            // Get existing emergency contact
            var emergencyContact = await _unitOfWork.EmergencyContacts.GetByIdAsync(request.Id, cancellationToken);
            if (emergencyContact == null)
            {
                throw new InvalidOperationException($"Emergency contact with ID '{request.Id}' not found.");
            }

            if (request.PermanentDelete)
            {
                // Permanent delete
                _unitOfWork.EmergencyContacts.Delete(emergencyContact);
                _logger.LogInformation("Emergency contact permanently deleted: {ContactId} by {DeletedBy}",
                    request.Id, request.DeletedBy);
            }
            else
            {
                // Soft delete
                emergencyContact.SoftDelete(request.DeletedBy);
                _unitOfWork.EmergencyContacts.Update(emergencyContact);
                _logger.LogInformation("Emergency contact soft deleted: {ContactId} by {DeletedBy}",
                    request.Id, request.DeletedBy);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return CommandResultDto<bool>.Success(true, "Emergency contact deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting emergency contact: {ContactId}", request.Id);
            throw;
        }
    }
}
