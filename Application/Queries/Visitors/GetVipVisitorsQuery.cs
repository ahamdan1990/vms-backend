using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Queries.Visitors;

/// <summary>
/// Query for getting VIP visitors
/// </summary>
public class GetVipVisitorsQuery : IRequest<List<VisitorListDto>>
{
    public bool IncludeDeleted { get; set; } = false;
}
