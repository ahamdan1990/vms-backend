using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Reports;

namespace VisitorManagementSystem.Api.Application.Queries.Reports;

/// <summary>
/// Query to get visitor statistics and analytics for a given time period
/// </summary>
public class GetVisitorStatisticsQuery : IRequest<VisitorStatisticsReportDto>
{
    /// <summary>
    /// Start date for statistics (inclusive)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for statistics (inclusive)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Optional location filter
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Group by period (daily, weekly, monthly)
    /// </summary>
    public string GroupBy { get; set; } = "daily";
}
