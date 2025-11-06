using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.TimeSlots;

/// <summary>
/// Handler for cancelling a time slot booking
/// </summary>
public class CancelTimeSlotBookingCommandHandler : IRequestHandler<CancelTimeSlotBookingCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public CancelTimeSlotBookingCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(CancelTimeSlotBookingCommand request, CancellationToken cancellationToken)
    {
        // 1. Get the booking
        var booking = await _unitOfWork.Repository<TimeSlotBooking>()
            .GetQueryable()
            .Include(b => b.TimeSlot)
            .Include(b => b.Invitation)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking == null)
        {
            throw new ArgumentException($"Time slot booking with ID {request.BookingId} not found.");
        }

        // 2. Check if booking can be cancelled
        if (!booking.CanBeCancelled)
        {
            throw new InvalidOperationException(
                $"Cannot cancel booking. Current status: {booking.Status}");
        }

        // 3. Cancel the booking
        booking.Cancel(request.CancelledBy, request.CancellationReason);

        // 4. Save changes
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}
