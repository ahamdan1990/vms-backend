using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.TimeSlots;
using VisitorManagementSystem.Api.Application.Queries.TimeSlots;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.TimeSlots;

/// <summary>
/// Handler for getting available time slots for a specific date
/// </summary>
public class GetAvailableTimeSlotsQueryHandler : IRequestHandler<GetAvailableTimeSlotsQuery, List<AvailableTimeSlotDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAvailableTimeSlotsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<AvailableTimeSlotDto>> Handle(GetAvailableTimeSlotsQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<TimeSlot>().GetQueryable()
            .Where(ts => ts.IsActive);

        // Filter by location if specified
        if (request.LocationId.HasValue)
        {
            query = query.Where(ts => ts.LocationId == null || ts.LocationId == request.LocationId.Value);
        }

        // Filter by day of week (1=Monday, 7=Sunday)
        var dayOfWeek = (int)request.Date.DayOfWeek == 0 ? 7 : (int)request.Date.DayOfWeek;
        query = query.Where(ts => ts.ActiveDays.Contains(dayOfWeek.ToString()));

        var timeSlots = await query
            .Include(ts => ts.Location)
            .OrderBy(ts => ts.DisplayOrder)
            .ThenBy(ts => ts.StartTime)
            .ToListAsync(cancellationToken);

        var availableTimeSlots = new List<AvailableTimeSlotDto>();

        foreach (var timeSlot in timeSlots)
        {
            // Calculate current bookings for this time slot on the specified date
            var currentBookings = await GetCurrentBookingsForTimeSlot(timeSlot.Id, request.Date, cancellationToken);
            var availableSpots = timeSlot.MaxVisitors - currentBookings;
            var occupancyPercentage = timeSlot.MaxVisitors > 0
                ? (double)currentBookings / timeSlot.MaxVisitors * 100
                : 0;

            var dto = _mapper.Map<AvailableTimeSlotDto>(timeSlot);
            dto.CurrentBookings = currentBookings;
            dto.AvailableSpots = Math.Max(0, availableSpots);
            dto.IsFullyBooked = availableSpots <= 0;
            dto.OccupancyPercentage = Math.Round(occupancyPercentage, 2);

            // Calculate next available date if fully booked
            if (dto.IsFullyBooked)
            {
                dto.NextAvailableDate = await GetNextAvailableDate(timeSlot, request.Date, cancellationToken);
            }

            availableTimeSlots.Add(dto);
        }

        return availableTimeSlots;
    }

    /// <summary>
    /// Gets the current number of confirmed bookings for a time slot on a specific date
    /// </summary>
    private async Task<int> GetCurrentBookingsForTimeSlot(int timeSlotId, DateTime date, CancellationToken cancellationToken)
    {
        var bookings = await _unitOfWork.Repository<TimeSlotBooking>()
            .GetQueryable()
            .Where(b => b.TimeSlotId == timeSlotId &&
                       b.BookingDate.Date == date.Date &&
                       b.Status == BookingStatus.Confirmed)
            .ToListAsync(cancellationToken);

        return bookings.Sum(b => b.VisitorCount);
    }

    /// <summary>
    /// Finds the next available date (within 30 days) when this time slot has capacity
    /// </summary>
    private async Task<DateTime?> GetNextAvailableDate(TimeSlot timeSlot, DateTime startDate, CancellationToken cancellationToken)
    {
        var activeDays = timeSlot.ActiveDays?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => int.TryParse(d.Trim(), out var day) ? day : 0)
            .Where(d => d > 0)
            .ToList() ?? new List<int>();

        if (!activeDays.Any())
            return null;

        // Check next 30 days
        for (int i = 1; i <= 30; i++)
        {
            var checkDate = startDate.AddDays(i);
            var checkDayOfWeek = (int)checkDate.DayOfWeek == 0 ? 7 : (int)checkDate.DayOfWeek;

            // Skip if not an active day
            if (!activeDays.Contains(checkDayOfWeek))
                continue;

            var currentBookings = await GetCurrentBookingsForTimeSlot(timeSlot.Id, checkDate, cancellationToken);
            if (currentBookings < timeSlot.MaxVisitors)
            {
                return checkDate;
            }
        }

        return null; // No availability in next 30 days
    }
}