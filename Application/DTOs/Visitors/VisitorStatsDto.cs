namespace VisitorManagementSystem.Api.Application.DTOs.Visitors;

/// <summary>
/// DTO for visitor statistics
/// </summary>
public class VisitorStatsDto
{
    /// <summary>
    /// Total number of visitors
    /// </summary>
    public int TotalVisitors { get; set; }

    /// <summary>
    /// Number of active visitors
    /// </summary>
    public int ActiveVisitors { get; set; }

    /// <summary>
    /// Number of VIP visitors
    /// </summary>
    public int VipVisitors { get; set; }

    /// <summary>
    /// Number of blacklisted visitors
    /// </summary>
    public int BlacklistedVisitors { get; set; }

    /// <summary>
    /// Number of visitors with incomplete profiles
    /// </summary>
    public int IncompleteProfiles { get; set; }

    /// <summary>
    /// Visitors created this month
    /// </summary>
    public int VisitorsThisMonth { get; set; }

    /// <summary>
    /// Visitors created this year
    /// </summary>
    public int VisitorsThisYear { get; set; }

    /// <summary>
    /// Average visits per visitor
    /// </summary>
    public double AverageVisitsPerVisitor { get; set; }

    /// <summary>
    /// Top companies by visitor count
    /// </summary>
    public List<CompanyVisitorCountDto> TopCompanies { get; set; } = new();

    /// <summary>
    /// Visitor growth data (monthly)
    /// </summary>
    public List<VisitorGrowthDto> GrowthData { get; set; } = new();

    /// <summary>
    /// Visitor distribution by nationality
    /// </summary>
    public Dictionary<string, int> NationalityDistribution { get; set; } = new();

    /// <summary>
    /// Recent visitor registrations
    /// </summary>
    public List<VisitorListDto> RecentRegistrations { get; set; } = new();
}

/// <summary>
/// Company visitor count DTO
/// </summary>
public class CompanyVisitorCountDto
{
    /// <summary>
    /// Company name
    /// </summary>
    public string Company { get; set; } = string.Empty;

    /// <summary>
    /// Number of unique visitors
    /// </summary>
    public int VisitorCount { get; set; }

    /// <summary>
    /// Total number of visits
    /// </summary>
    public int TotalVisits { get; set; }

    /// <summary>
    /// Date of last visit from this company
    /// </summary>
    public DateTime? LastVisit { get; set; }
}

/// <summary>
/// Visitor growth data DTO
/// </summary>
public class VisitorGrowthDto
{
    /// <summary>
    /// Month/period
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Number of new visitors in the period
    /// </summary>
    public int NewVisitors { get; set; }

    /// <summary>
    /// Total visits in the period
    /// </summary>
    public int TotalVisits { get; set; }

    /// <summary>
    /// Growth percentage from previous period
    /// </summary>
    public double GrowthPercentage { get; set; }
}
