using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Permissions;

namespace VisitorManagementSystem.Api.Application.Queries.Permissions;

/// <summary>
/// Query to get all roles
/// </summary>
public class GetAllRolesQuery : IRequest<List<RoleDto>>
{
    /// <summary>
    /// Filter by active status (optional)
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Include permission and user counts
    /// </summary>
    public bool IncludeCounts { get; set; } = true;
}
