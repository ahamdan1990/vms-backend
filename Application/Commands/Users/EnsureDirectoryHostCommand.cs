using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Users;

namespace VisitorManagementSystem.Api.Application.Commands.Users;

/// <summary>
/// Command that ensures a host user exists by provisioning them from the corporate directory if necessary.
/// </summary>
public class EnsureDirectoryHostCommand : IRequest<UserDto>
{
    public string Identifier { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Role { get; set; }
}
