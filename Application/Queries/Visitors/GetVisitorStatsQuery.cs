using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Queries.Visitors;

/// <summary>
/// Query for getting visitor statistics
/// </summary>
public class GetVisitorStatsQuery : IRequest<VisitorStatsDto>
{
    /// <summary>
    /// Current user ID for access filtering
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// User permissions for determining access level
    /// </summary>
    public List<string> UserPermissions { get; set; } = new();

    public bool IncludeDeleted { get; set; } = false;
}
