using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Auth;

namespace VisitorManagementSystem.Api.Application.Commands.Auth;

/// <summary>
/// Command for refreshing authentication tokens
/// </summary>
public class RefreshTokenCommand : IRequest<AuthenticationResult>
{
    /// <summary>
    /// The refresh token to use
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// IP address of the client
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the client
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// ✅ FIXED: Device fingerprint from client (required for validation)
    /// </summary>
    public string? DeviceFingerprint { get; set; }
}