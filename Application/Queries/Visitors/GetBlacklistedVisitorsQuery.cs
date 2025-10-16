using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Queries.Visitors;

/// <summary>
/// Query for getting blacklisted visitors
/// </summary>
public class GetBlacklistedVisitorsQuery : IRequest<List<VisitorListDto>>
{
    public bool IncludeDeleted { get; set; } = false;
}
