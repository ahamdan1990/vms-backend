namespace VisitorManagementSystem.Api.Application.DTOs.Reports;

/// <summary>
/// Single visitor record in comprehensive report
/// </summary>
public class VisitorReportItemDto
{
    public int InvitationId { get; set; }
    public int VisitorId { get; set; }
    public string VisitorName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? VisitorEmail { get; set; }
    public string? VisitorPhone { get; set; }
    public int HostId { get; set; }
    public string HostName { get; set; } = string.Empty;
    public string? HostDepartment { get; set; }
    public string? HostEmail { get; set; }
    public string? HostPhone { get; set; }
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string? VisitPurpose { get; set; }
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public int MinutesOnSite { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Summary statistics for the report
/// </summary>
public class ReportSummaryDto
{
    public int TotalRecords { get; set; }
    public int TotalCheckedIn { get; set; }
    public int TotalCheckedOut { get; set; }
    public int TotalOverdue { get; set; }
    public int TotalPending { get; set; }
    public int TotalActive { get; set; }
}

/// <summary>
/// Applied filters for the report
/// </summary>
public class ReportFiltersDto
{
    public int? LocationId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
    public string? SearchTerm { get; set; }
    public int? HostId { get; set; }
    public int? VisitPurposeId { get; set; }
    public string? Department { get; set; }
    public bool? CheckedInOnly { get; set; }
    public bool? CheckedOutOnly { get; set; }
    public bool? OverdueOnly { get; set; }
}

/// <summary>
/// Pagination information
/// </summary>
public class PaginationDto
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

/// <summary>
/// Comprehensive visitor report with filters, pagination, and summary statistics
/// </summary>
public class ComprehensiveVisitorReportDto
{
    public DateTime GeneratedAt { get; set; }
    public ReportSummaryDto Summary { get; set; } = new();
    public List<VisitorReportItemDto> Visitors { get; set; } = new();
    public ReportFiltersDto Filters { get; set; } = new();
    public PaginationDto Pagination { get; set; } = new();
}
