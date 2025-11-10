namespace VisitorManagementSystem.Api.Application.DTOs.Analytics;

/// <summary>
/// Comprehensive analytics DTO containing all system metrics and insights
/// </summary>
public class ComprehensiveAnalyticsDto
{
    // Real-time Overview Metrics
    public RealTimeMetricsDto RealTimeMetrics { get; set; } = new();

    // Capacity & Utilization
    public CapacityMetricsDto CapacityMetrics { get; set; } = new();

    // Visitor Metrics
    public VisitorMetricsDto VisitorMetrics { get; set; } = new();

    // Invitation Metrics
    public InvitationMetricsDto InvitationMetrics { get; set; } = new();

    // Trends & Analytics
    public TrendAnalyticsDto Trends { get; set; } = new();

    // Insights
    public InsightsDto Insights { get; set; } = new();

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string TimeZone { get; set; } = "UTC";
}

/// <summary>
/// Real-time metrics for dashboard overview
/// </summary>
public class RealTimeMetricsDto
{
    public int ExpectedVisitorsToday { get; set; }
    public int CheckedInToday { get; set; }
    public int PendingCheckouts { get; set; }
    public int WalkInsToday { get; set; }
    public int ActiveVisitorsInSystem { get; set; }
    public int TodayVisitors { get; set; }
    public int OverdueVisitors { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Capacity utilization metrics
/// </summary>
public class CapacityMetricsDto
{
    public decimal CurrentUtilization { get; set; } // Percentage
    public int CurrentOccupancy { get; set; }
    public int MaxCapacity { get; set; }
    public int AvailableSlots { get; set; }
    public List<LocationCapacityDto> LocationBreakdown { get; set; } = new();
    public List<PeakHourDto> PeakHours { get; set; } = new();
}

/// <summary>
/// Visitor-related metrics
/// </summary>
public class VisitorMetricsDto
{
    public int TotalVisitors { get; set; }
    public int ActiveToday { get; set; }
    public int CompletedToday { get; set; }
    public decimal AverageVisitDurationMinutes { get; set; }
    public decimal CheckInRate { get; set; } // Percentage of visitors who checked in
    public decimal NoShowRate { get; set; } // Percentage of no-shows
    public List<DailyVisitorTrendDto> DailyTrend { get; set; } = new(); // Last 30 days
}

/// <summary>
/// Invitation-related metrics
/// </summary>
public class InvitationMetricsDto
{
    public int TotalInvitations { get; set; }
    public int PendingApproval { get; set; }
    public int ApprovedToday { get; set; }
    public int RejectedToday { get; set; }
    public int ActiveToday { get; set; }
    public int CompletedToday { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
}

/// <summary>
/// Trend analytics
/// </summary>
public class TrendAnalyticsDto
{
    public List<DailyVisitorTrendDto> Last30Days { get; set; } = new();
    public List<HourlyTrendDto> TodayHourly { get; set; } = new();
    public List<PopularLocationDto> PopularLocations { get; set; } = new();
    public List<VisitPurposeTrendDto> VisitPurposeTrends { get; set; } = new();
}

/// <summary>
/// System insights and recommendations
/// </summary>
public class InsightsDto
{
    public List<CheckInActivityDto> TodaysCheckIns { get; set; } = new();
    public int PendingInvitations { get; set; }
    public int TotalApproved { get; set; }
    public int TotalActiveToday { get; set; }
    public int TotalRejected { get; set; }
    public int TotalCompleted { get; set; }
    public List<string> Recommendations { get; set; } = new();
    public List<AlertSummaryDto> RecentAlerts { get; set; } = new();
}

/// <summary>
/// Location capacity details
/// </summary>
public class LocationCapacityDto
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int CurrentOccupancy { get; set; }
    public int MaxCapacity { get; set; }
    public decimal UtilizationRate { get; set; }
    public int AvailableSlots { get; set; }
}

/// <summary>
/// Peak hour information
/// </summary>
public class PeakHourDto
{
    public int Hour { get; set; } // 0-23
    public string TimeRange { get; set; } = string.Empty; // e.g., "09:00 - 10:00"
    public int VisitorCount { get; set; }
    public decimal UtilizationRate { get; set; }
    public bool IsPeakHour { get; set; }
}

/// <summary>
/// Daily visitor trend data point
/// </summary>
public class DailyVisitorTrendDto
{
    public DateTime Date { get; set; }
    public int TotalVisitors { get; set; }
    public int CheckedIn { get; set; }
    public int NoShows { get; set; }
    public int WalkIns { get; set; }
    public decimal AverageVisitDurationMinutes { get; set; }
}

/// <summary>
/// Hourly trend data point
/// </summary>
public class HourlyTrendDto
{
    public int Hour { get; set; }
    public int VisitorCount { get; set; }
    public int CheckIns { get; set; }
    public int CheckOuts { get; set; }
}

/// <summary>
/// Popular location statistics
/// </summary>
public class PopularLocationDto
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int VisitorCount { get; set; }
    public decimal AverageVisitDurationMinutes { get; set; }
    public decimal UtilizationRate { get; set; }
    public int Rank { get; set; }
}

/// <summary>
/// Visit purpose trend
/// </summary>
public class VisitPurposeTrendDto
{
    public int VisitPurposeId { get; set; }
    public string PurposeName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// Check-in activity for insights
/// </summary>
public class CheckInActivityDto
{
    public int VisitorId { get; set; }
    public string VisitorName { get; set; } = string.Empty;
    public DateTime CheckInTime { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string VisitPurpose { get; set; } = string.Empty;
}

/// <summary>
/// Alert summary for insights
/// </summary>
public class AlertSummaryDto
{
    public int AlertId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public bool IsAcknowledged { get; set; }
}
