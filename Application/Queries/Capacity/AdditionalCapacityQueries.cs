using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Capacity;

namespace VisitorManagementSystem.Api.Application.Queries.Capacity;

/// <summary>
/// Query to get capacity overview for multiple locations
/// </summary>
public class GetCapacityOverviewQuery : IRequest<List<LocationCapacityOverviewDto>>
{
    public DateTime DateTime { get; set; } = DateTime.Now;
    public int[]? LocationIds { get; set; }
    public bool IncludeInactive { get; set; } = false;
}

/// <summary>
/// Query to get capacity utilization trends
/// </summary>
public class GetCapacityUtilizationTrendsQuery : IRequest<CapacityTrendsDto>
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? LocationId { get; set; }
    public string GroupBy { get; set; } = "day"; // hour, day, week
}

/// <summary>
/// Query to get alternative time slots
/// </summary>
public class GetAlternativeTimeSlotsQuery : IRequest<List<AlternativeTimeSlotDto>>
{
    public DateTime OriginalDateTime { get; set; }
    public int ExpectedVisitors { get; set; } = 1;
    public int? LocationId { get; set; }
    public int DaysToCheck { get; set; } = 7; // Look ahead days
}