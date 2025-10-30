using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Permissions;

namespace VisitorManagementSystem.Api.Application.Commands.Permissions;

/// <summary>
/// Command to revoke permissions from a role
/// </summary>
public class RevokePermissionsCommand : IRequest<int>
{
    public int RoleId { get; set; }
    public RevokePermissionsDto RevokePermissionsDto { get; set; }

    public RevokePermissionsCommand(int roleId, RevokePermissionsDto revokePermissionsDto)
    {
        RoleId = roleId;
        RevokePermissionsDto = revokePermissionsDto;
    }
}
