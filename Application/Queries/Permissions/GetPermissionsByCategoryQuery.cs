using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Permissions;

namespace VisitorManagementSystem.Api.Application.Queries.Permissions;

/// <summary>
/// Query to get permissions grouped by category
/// </summary>
public class GetPermissionsByCategoryQuery : IRequest<List<PermissionCategoryDto>>
{
    /// <summary>
    /// Filter by active status (optional)
    /// </summary>
    public bool? IsActive { get; set; }
}
