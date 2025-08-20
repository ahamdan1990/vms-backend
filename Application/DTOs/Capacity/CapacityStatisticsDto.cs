namespace VisitorManagementSystem.Api.Application.DTOs.Capacity;

/// <summary>
/// DTO for capacity statistics
/// </summary>
public class CapacityStatisticsDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public int TotalCapacity { get; set; }
    public int TotalBookings { get; set; }
    public int PeakOccupancy { get; set; }
    public double AverageOccupancy { get; set; }
    public double UtilizationPercentage { get; set; }
    public int DaysAtCapacity { get; set; }
    public int DaysAtWarning { get; set; }
    public DateTime? PeakOccupancyDate { get; set; }
    public List<DailyCapacityDto> DailyBreakdown { get; set; } = new();
}

/// <summary>
/// DTO for daily capacity breakdown
/// </summary>
public class DailyCapacityDto
{
    public DateTime Date { get; set; }
    public int Capacity { get; set; }
    public int Bookings { get; set; }
    public double UtilizationPercentage { get; set; }
    public bool WasAtCapacity { get; set; }
    public bool WasAtWarning { get; set; }
}

/// <summary>
/// DTO for capacity trends
/// </summary>
public class CapacityTrendsDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string GroupBy { get; set; } = string.Empty;
    public List<CapacityTrendDataPointDto> DataPoints { get; set; } = new();
    public CapacityTrendSummaryDto Summary { get; set; } = new();
}


