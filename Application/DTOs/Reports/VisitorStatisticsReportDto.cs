namespace VisitorManagementSystem.Api.Application.DTOs.Reports;

/// <summary>
/// Statistics by location
/// </summary>
public class LocationStatisticsDto
{
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public int VisitorCount { get; set; }
    public int CheckedInCount { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Statistics by department
/// </summary>
public class DepartmentStatisticsDto
{
    public string Department { get; set; } = string.Empty;
    public int VisitorCount { get; set; }
    public int CheckedInCount { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Statistics by visit purpose
/// </summary>
public class VisitPurposeStatisticsDto
{
    public int? VisitPurposeId { get; set; }
    public string? VisitPurposeName { get; set; }
    public int VisitorCount { get; set; }
    public int CheckedInCount { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Statistics by host
/// </summary>
public class HostStatisticsDto
{
    public int HostId { get; set; }
    public string HostName { get; set; } = string.Empty;
    public int VisitorCount { get; set; }
    public int CheckedInCount { get; set; }
}

/// <summary>
/// Time series data point
/// </summary>
public class TimeSeriesDataPointDto
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public int TotalVisitors { get; set; }
    public int CheckedIn { get; set; }
    public int CheckedOut { get; set; }
}

/// <summary>
/// Comprehensive visitor statistics and analytics report
/// </summary>
public class VisitorStatisticsReportDto
{
    public DateTime GeneratedAt { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Overall statistics
    public int TotalVisitors { get; set; }
    public int TotalCheckedIn { get; set; }
    public int TotalCheckedOut { get; set; }
    public int TotalNoShow { get; set; }
    public int TotalCancelled { get; set; }
    public int AverageDurationMinutes { get; set; }
    public double CheckInRate { get; set; }

    // Breakdown by various dimensions
    public List<LocationStatisticsDto> ByLocation { get; set; } = new();
    public List<DepartmentStatisticsDto> ByDepartment { get; set; } = new();
    public List<VisitPurposeStatisticsDto> ByPurpose { get; set; } = new();
    public List<HostStatisticsDto> TopHosts { get; set; } = new();
    public List<TimeSeriesDataPointDto> TimeSeries { get; set; } = new();
}
