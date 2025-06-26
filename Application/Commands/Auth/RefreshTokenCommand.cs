using MediatR;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Application.DTOs.Auth;

namespace VisitorManagementSystem.Api.Application.Commands.Auth;

/// <summary>
/// Command for refreshing authentication token
/// </summary>
public class RefreshTokenCommand : IRequest<AuthenticationResult>
{
    public string RefreshToken { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceFingerprint { get; set; }
}
