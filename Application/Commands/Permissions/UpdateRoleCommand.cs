using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Permissions;

namespace VisitorManagementSystem.Api.Application.Commands.Permissions;

/// <summary>
/// Command to update an existing role
/// </summary>
public class UpdateRoleCommand : IRequest<RoleDto>
{
    public int RoleId { get; set; }
    public UpdateRoleDto UpdateRoleDto { get; set; }

    public UpdateRoleCommand(int roleId, UpdateRoleDto updateRoleDto)
    {
        RoleId = roleId;
        UpdateRoleDto = updateRoleDto;
    }
}
