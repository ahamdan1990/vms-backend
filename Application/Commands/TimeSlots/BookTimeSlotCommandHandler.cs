using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.TimeSlots;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.TimeSlots;

/// <summary>
/// Handler for booking a time slot
/// </summary>
public class BookTimeSlotCommandHandler : IRequestHandler<BookTimeSlotCommand, TimeSlotBookingDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public BookTimeSlotCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<TimeSlotBookingDto> Handle(BookTimeSlotCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate and get time slot
        var timeSlot = await _unitOfWork.Repository<TimeSlot>()
            .GetQueryable()
            .Include(ts => ts.Location)
            .FirstOrDefaultAsync(ts => ts.Id == request.TimeSlotId, cancellationToken);

        if (timeSlot == null)
        {
            throw new ArgumentException($"Time slot with ID {request.TimeSlotId} not found.");
        }

        if (!timeSlot.IsActive)
        {
            throw new InvalidOperationException("Cannot book an inactive time slot.");
        }

        // 2. Validate booking date
        if (request.BookingDate.Date < DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Cannot book a time slot for a past date.");
        }

        // 3. Check day of week
        var dayOfWeek = (int)request.BookingDate.DayOfWeek == 0 ? 7 : (int)request.BookingDate.DayOfWeek;
        var activeDays = timeSlot.ActiveDays?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => int.TryParse(d.Trim(), out var day) ? day : 0)
            .Where(d => d > 0)
            .ToList() ?? new List<int>();

        if (activeDays.Any() && !activeDays.Contains(dayOfWeek))
        {
            throw new InvalidOperationException(
                $"Time slot '{timeSlot.Name}' is not active on {request.BookingDate.DayOfWeek}.");
        }

        // 4. Check capacity
        var existingBookings = await _unitOfWork.Repository<TimeSlotBooking>()
            .GetQueryable()
            .Where(b => b.TimeSlotId == request.TimeSlotId &&
                       b.BookingDate.Date == request.BookingDate.Date &&
                       b.Status == BookingStatus.Confirmed)
            .ToListAsync(cancellationToken);

        var currentBookingCount = existingBookings.Sum(b => b.VisitorCount);
        var availableCapacity = timeSlot.MaxVisitors - currentBookingCount;

        if (request.VisitorCount > availableCapacity)
        {
            if (!timeSlot.AllowOverlapping)
            {
                throw new InvalidOperationException(
                    $"Cannot book {request.VisitorCount} visitor(s). Only {availableCapacity} spot(s) available " +
                    $"for {timeSlot.Name} on {request.BookingDate:yyyy-MM-dd}. " +
                    $"(Current: {currentBookingCount}/{timeSlot.MaxVisitors})");
            }
        }

        // 5. Check if invitation already has a booking
        if (request.InvitationId.HasValue)
        {
            var existingInvitationBooking = await _unitOfWork.Repository<TimeSlotBooking>()
                .GetQueryable()
                .FirstOrDefaultAsync(b => b.InvitationId == request.InvitationId &&
                                         b.Status != BookingStatus.Cancelled,
                                    cancellationToken);

            if (existingInvitationBooking != null)
            {
                throw new InvalidOperationException(
                    $"Invitation already has an active booking for time slot '{existingInvitationBooking.TimeSlot?.Name}'.");
            }
        }

        // 6. Create booking
        var booking = new TimeSlotBooking
        {
            TimeSlotId = request.TimeSlotId,
            BookingDate = request.BookingDate.Date,
            InvitationId = request.InvitationId,
            VisitorCount = request.VisitorCount,
            Notes = request.Notes?.Trim(),
            Status = BookingStatus.Confirmed,
            BookedBy = request.BookedBy,
            BookedOn = DateTime.UtcNow
        };

        booking.SetCreatedBy(request.BookedBy);

        // 7. Validate business rules
        var validationErrors = booking.ValidateBooking();
        if (validationErrors.Any())
        {
            throw new ArgumentException($"Validation failed: {string.Join(", ", validationErrors)}");
        }

        // 8. Save to database
        await _unitOfWork.Repository<TimeSlotBooking>().AddAsync(booking);
        await _unitOfWork.SaveChangesAsync();

        // 9. Load relationships for DTO mapping
        var savedBooking = await _unitOfWork.Repository<TimeSlotBooking>()
            .GetQueryable()
            .Include(b => b.TimeSlot)
            .Include(b => b.Invitation)
            .Include(b => b.BookedByUser)
            .FirstOrDefaultAsync(b => b.Id == booking.Id, cancellationToken);

        // 10. Return mapped DTO
        return _mapper.Map<TimeSlotBookingDto>(savedBooking);
    }
}
