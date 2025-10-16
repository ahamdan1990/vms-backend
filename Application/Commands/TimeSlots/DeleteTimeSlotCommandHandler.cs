using MediatR;
using VisitorManagementSystem.Api.Application.Commands.TimeSlots;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.TimeSlots;

/// <summary>
/// Handler for deleting a time slot
/// </summary>
public class DeleteTimeSlotCommandHandler : IRequestHandler<DeleteTimeSlotCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTimeSlotCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteTimeSlotCommand request, CancellationToken cancellationToken)
    {
        // Get existing time slot
        var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetByIdAsync(request.Id);
        if (timeSlot == null)
        {
            return false;
        }

        // Check if time slot is being used
        var isInUse = await CheckIfTimeSlotInUse(request.Id);
        if (isInUse && !request.HardDelete)
        {
            throw new InvalidOperationException("Cannot delete time slot that is currently in use. Consider soft delete instead.");
        }

        if (request.HardDelete)
        {
            // Hard delete
            _unitOfWork.Repository<TimeSlot>().Delete(timeSlot);
        }
        else
        {
            // Soft delete
            timeSlot.IsActive = false;
            timeSlot.ModifiedBy = request.DeletedBy;
            _unitOfWork.Repository<TimeSlot>().Update(timeSlot);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private Task<bool> CheckIfTimeSlotInUse(int timeSlotId)
    {
        // Check if there are any active invitations using this time slot
        // This would need to be implemented based on your invitation structure
        // For now, returning false as a placeholder
        return Task.FromResult(false);
    }
}