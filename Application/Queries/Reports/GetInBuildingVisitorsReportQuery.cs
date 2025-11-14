using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Reports;

namespace VisitorManagementSystem.Api.Application.Queries.Reports;

/// <summary>
/// Query to get the current list of visitors who are inside the building.
/// </summary>
public class GetInBuildingVisitorsReportQuery : IRequest<WhoIsInBuildingReportDto>
{
    /// <summary>
    /// Optional location filter. When provided the report will include only the selected location.
    /// </summary>
    public int? LocationId { get; set; }
}
