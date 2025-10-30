using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Permissions;

namespace VisitorManagementSystem.Api.Application.Queries.Permissions;

/// <summary>
/// Query to get all permissions
/// </summary>
public class GetAllPermissionsQuery : IRequest<List<PermissionDto>>
{
    /// <summary>
    /// Filter by category (optional)
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Filter by active status (optional)
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Search term for name/description (optional)
    /// </summary>
    public string? SearchTerm { get; set; }
}
