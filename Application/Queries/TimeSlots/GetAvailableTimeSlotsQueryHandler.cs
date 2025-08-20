using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.TimeSlots;
using VisitorManagementSystem.Api.Application.Queries.TimeSlots;
using VisitorManagementSystem.Api.Domain.Entities;
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

        // Filter by day of week
        var dayOfWeek = request.Date.DayOfWeek.ToString();
        query = query.Where(ts => ts.ActiveDays.Contains(dayOfWeek));

        var timeSlots = await query
            .Include(ts => ts.Location)
            .OrderBy(ts => ts.DisplayOrder)
            .ThenBy(ts => ts.StartTime)
            .ToListAsync(cancellationToken);

        var availableTimeSlots = new List<AvailableTimeSlotDto>();

        foreach (var timeSlot in timeSlots)
        {
            // Calculate current bookings for this time slot on the specified date
            var currentBookings = await GetCurrentBookingsForTimeSlot(timeSlot.Id, request.Date);
            var availableSlots = timeSlot.MaxVisitors - currentBookings;

            availableTimeSlots.Add(new AvailableTimeSlotDto
            {
                Id = timeSlot.Id,
                Name = timeSlot.Name,
                StartTime = timeSlot.StartTime,
                EndTime = timeSlot.EndTime,
                MaxVisitors = timeSlot.MaxVisitors,
                CurrentBookings = currentBookings,
                AvailableSlots = Math.Max(0, availableSlots),
                IsAvailable = availableSlots > 0,
                LocationName = timeSlot.Location?.Name
            });
        }

        return availableTimeSlots;
    }

    private async Task<int> GetCurrentBookingsForTimeSlot(int timeSlotId, DateTime date)
    {
        // This would calculate the current bookings for the time slot on the specified date
        // Implementation would depend on how invitations/bookings are linked to time slots
        // For now, returning 0 as a placeholder
        return 0;
    }
}