using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Auth;
using VisitorManagementSystem.Api.Application.Services.Auth;

namespace VisitorManagementSystem.Api.Application.Commands.Auth;

/// <summary>
/// Command for user logout
/// </summary>
public class LogoutCommand : IRequest<LogoutResult>
{
    public int UserId { get; set; }
    public string? RefreshToken { get; set; }
    public string? IpAddress { get; set; }
    public bool LogoutFromAllDevices { get; set; } = false;
}

