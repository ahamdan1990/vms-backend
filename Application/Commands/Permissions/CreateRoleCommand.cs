using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Permissions;

namespace VisitorManagementSystem.Api.Application.Commands.Permissions;

/// <summary>
/// Command to create a new role
/// </summary>
public class CreateRoleCommand : IRequest<RoleDto>
{
    public CreateRoleDto CreateRoleDto { get; set; }

    public CreateRoleCommand(CreateRoleDto createRoleDto)
    {
        CreateRoleDto = createRoleDto;
    }
}
