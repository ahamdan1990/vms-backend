using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Permissions;

namespace VisitorManagementSystem.Api.Application.Queries.Permissions;

/// <summary>
/// Query to get a role with its permissions
/// </summary>
public class GetRoleWithPermissionsQuery : IRequest<RoleWithPermissionsDto?>
{
    /// <summary>
    /// Role ID
    /// </summary>
    public int RoleId { get; set; }

    public GetRoleWithPermissionsQuery(int roleId)
    {
        RoleId = roleId;
    }
}
