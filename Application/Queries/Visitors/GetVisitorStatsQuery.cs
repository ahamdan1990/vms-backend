using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Queries.Visitors;

/// <summary>
/// Query for getting visitor statistics
/// </summary>
public class GetVisitorStatsQuery : IRequest<VisitorStatsDto>
{
    public bool IncludeDeleted { get; set; } = false;
}
