using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Capacity;

namespace VisitorManagementSystem.Api.Application.Queries.Capacity;

/// <summary>
/// Query to get capacity settings for all locations
/// </summary>
public class GetCapacitySettingsQuery : IRequest<List<CapacitySettingsDto>>
{
    public bool IncludeInactive { get; set; } = false;
}

/// <summary>
/// Query to get capacity settings for a specific location
/// </summary>
public class GetLocationCapacitySettingsQuery : IRequest<CapacitySettingsDto?>
{
    public int LocationId { get; set; }
}

/// <summary>
/// Query to get current occupancy
/// </summary>
public class GetOccupancyQuery : IRequest<OccupancyDto>
{
    public DateTime DateTime { get; set; }
    public int? LocationId { get; set; }
}

/// <summary>
/// Query to get capacity statistics
/// </summary>
public class GetCapacityStatisticsQuery : IRequest<CapacityStatisticsDto>
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? LocationId { get; set; }
}

/// <summary>
/// Query to get capacity trends
/// </summary>
public class GetCapacityTrendsQuery : IRequest<CapacityTrendsDto>
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? LocationId { get; set; }
    public string GroupBy { get; set; } = "day";
}