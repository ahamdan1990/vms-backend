using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.VisitPurposes;

namespace VisitorManagementSystem.Api.Application.Queries.VisitPurposes;

/// <summary>
/// Query to get all visit purposes
/// </summary>
public class GetVisitPurposesQuery : IRequest<List<VisitPurposeDto>>
{
    /// <summary>
    /// Only purposes that require approval
    /// </summary>
    public bool? RequiresApproval { get; set; }

    /// <summary>
    /// Include inactive purposes
    /// </summary>
    public bool IncludeInactive { get; set; } = false;
}
