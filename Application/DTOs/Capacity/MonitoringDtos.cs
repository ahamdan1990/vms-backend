namespace VisitorManagementSystem.Api.Application.DTOs.Capacity;

/// <summary>
/// DTO for location capacity overview
/// </summary>
public class LocationCapacityOverviewDto
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public int MaxCapacity { get; set; }
    public int CurrentOccupancy { get; set; }
    public int ReservedCount { get; set; }
    public int AvailableSlots { get; set; }
    public decimal OccupancyPercentage { get; set; }
    public bool IsAtCapacity { get; set; }
    public bool IsWarningLevel { get; set; }
    public bool IsOverCapacity { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<TimeSlotCapacityDto> TimeSlots { get; set; } = new();
}

/// <summary>
/// DTO for time slot capacity within a location
/// </summary>
public class TimeSlotCapacityDto
{
    public int TimeSlotId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int MaxVisitors { get; set; }
    public int CurrentBookings { get; set; }
    public int AvailableSlots { get; set; }
    public decimal UtilizationPercentage { get; set; }
    public bool IsAvailable { get; set; }
}

/// <summary>
/// DTO for alternative time slot suggestion
/// </summary>
public class AlternativeTimeSlotDto
{
    public DateTime DateTime { get; set; }
    public int? TimeSlotId { get; set; }
    public string? TimeSlotName { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int AvailableSlots { get; set; }
    public int MaxCapacity { get; set; }
    public string Reason { get; set; } = string.Empty; // Why this is an alternative
    public bool IsRecommended { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AvailableCapacity { get; set; }
    public decimal OccupancyPercentage { get; set; }
}



/// <summary>
/// DTO for individual trend data point
/// </summary>
public class CapacityTrendDataPointDto
{
    public DateTime Period { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }
    public int AverageOccupancy { get; set; }
    public int PeakOccupancy { get; set; }
    public decimal AverageUtilization { get; set; }
    public decimal PeakUtilization { get; set; }
    public int TotalVisitors { get; set; }
    public bool WasOverCapacity { get; set; }
    public int Capacity { get; set; }
    public int Bookings { get; set; }
    public double UtilizationPercentage { get; set; }
}

/// <summary>
/// DTO for capacity trend summary
/// </summary>
public class CapacityTrendSummaryDto
{
    public decimal OverallAverageUtilization { get; set; }
    public decimal PeakUtilization { get; set; }
    public DateTime PeakUtilizationDate { get; set; }
    public decimal LowestUtilization { get; set; }
    public DateTime LowestUtilizationDate { get; set; }
    public string TrendDirection { get; set; } = string.Empty; // "Increasing", "Decreasing", "Stable"
    public decimal TrendPercentage { get; set; }
    public int DaysOverCapacity { get; set; }
    public int DaysAtWarningLevel { get; set; }
    public string BusiestDayOfWeek { get; set; } = string.Empty;
    public string QuietestDayOfWeek { get; set; } = string.Empty;
    public TimeOnly? BusiestTimeOfDay { get; set; }
    public TimeOnly? QuietestTimeOfDay { get; set; }
    public double AverageUtilization { get; set; }
}