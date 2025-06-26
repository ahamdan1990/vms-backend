using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Auth;

/// <summary>
/// Refresh token request data transfer object
/// </summary>
public class RefreshTokenRequestDto
{
    /// <summary>
    /// Refresh token value
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Device fingerprint for security validation
    /// </summary>
    public string? DeviceFingerprint { get; set; }
}