namespace VisitorManagementSystem.Api.Domain.Models;

/// <summary>
/// Visitor statistics data structure
/// </summary>
public class VisitorStatistics
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
}

/// <summary>
/// Company visitor count data structure
/// </summary>
public class CompanyVisitorCount
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
