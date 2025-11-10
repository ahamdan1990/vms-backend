using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Analytics;

namespace VisitorManagementSystem.Api.Application.Queries.Analytics;

/// <summary>
/// Query to get comprehensive analytics for the dashboard
/// </summary>
public class GetComprehensiveAnalyticsQuery : IRequest<ComprehensiveAnalyticsDto>
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? LocationId { get; set; }
    public string TimeZone { get; set; } = "UTC";

    public GetComprehensiveAnalyticsQuery()
    {
        // Default to last 30 days if not specified
        StartDate = DateTime.UtcNow.AddDays(-30).Date;
        EndDate = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
    }
}
