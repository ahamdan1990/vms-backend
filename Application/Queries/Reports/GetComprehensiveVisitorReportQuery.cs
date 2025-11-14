using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Reports;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Queries.Reports;

/// <summary>
/// Query to get a comprehensive visitor report with advanced filtering, pagination, and sorting.
/// </summary>
public class GetComprehensiveVisitorReportQuery : IRequest<ComprehensiveVisitorReportDto>
{
    /// <summary>
    /// Optional location filter
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Start date for the report (inclusive)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for the report (inclusive)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Filter by invitation status
    /// </summary>
    public InvitationStatus? Status { get; set; }

    /// <summary>
    /// Search term for visitor name, company, or email
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by host ID
    /// </summary>
    public int? HostId { get; set; }

    /// <summary>
    /// Filter by visit purpose ID
    /// </summary>
    public int? VisitPurposeId { get; set; }

    /// <summary>
    /// Filter by department
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// Include only checked-in visitors
    /// </summary>
    public bool? CheckedInOnly { get; set; }

    /// <summary>
    /// Include only checked-out visitors
    /// </summary>
    public bool? CheckedOutOnly { get; set; }

    /// <summary>
    /// Include only overdue visitors
    /// </summary>
    public bool? OverdueOnly { get; set; }

    /// <summary>
    /// Page number (0-based)
    /// </summary>
    public int PageIndex { get; set; } = 0;

    /// <summary>
    /// Page size (default 50, max 500)
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Sort column
    /// </summary>
    public string SortBy { get; set; } = "CheckedInAt";

    /// <summary>
    /// Sort direction (asc or desc)
    /// </summary>
    public string SortDirection { get; set; } = "desc";
}
