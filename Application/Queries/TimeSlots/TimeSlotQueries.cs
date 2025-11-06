using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.TimeSlots;

namespace VisitorManagementSystem.Api.Application.Queries.TimeSlots;

/// <summary>
/// Query to get time slots with filtering and pagination
/// </summary>
public class GetTimeSlotsQuery : IRequest<PagedResultDto<TimeSlotDto>>
{
    public int? LocationId { get; set; }
    public bool ActiveOnly { get; set; } = true;
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 50;
    public string SortBy { get; set; } = "DisplayOrder";
    public string SortDirection { get; set; } = "asc";
}

/// <summary>
/// Query to get a time slot by ID
/// </summary>
public class GetTimeSlotByIdQuery : IRequest<TimeSlotDto?>
{
    public int Id { get; set; }
}

/// <summary>
/// Query to get available time slots for a specific date
/// </summary>
public class GetAvailableTimeSlotsQuery : IRequest<List<AvailableTimeSlotDto>>
{
    public DateTime Date { get; set; }
    public int? LocationId { get; set; }
}

/// <summary>
/// Query to get a time slot booking by ID
/// </summary>
public class GetTimeSlotBookingByIdQuery : IRequest<TimeSlotBookingDto?>
{
    public int Id { get; set; }
}

/// <summary>
/// Query to get bookings for a specific time slot
/// </summary>
public class GetTimeSlotBookingsQuery : IRequest<List<TimeSlotBookingDto>>
{
    public int TimeSlotId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}