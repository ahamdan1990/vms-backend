using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Permissions;

namespace VisitorManagementSystem.Api.Application.Commands.Permissions;

/// <summary>
/// Command to grant permissions to a role
/// </summary>
public class GrantPermissionsCommand : IRequest<int>
{
    public int RoleId { get; set; }
    public GrantPermissionsDto GrantPermissionsDto { get; set; }

    public GrantPermissionsCommand(int roleId, GrantPermissionsDto grantPermissionsDto)
    {
        RoleId = roleId;
        GrantPermissionsDto = grantPermissionsDto;
    }
}
