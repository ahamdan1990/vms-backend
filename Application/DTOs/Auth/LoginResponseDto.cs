namespace VisitorManagementSystem.Api.Application.DTOs.Auth;

/// <summary>
/// Login response data transfer object
/// </summary>
public class LoginResponseDto
{
    /// <summary>
    /// Login success status
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if login failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Current user information
    /// </summary>
    public CurrentUserDto? User { get; set; }

    /// <summary>
    /// Whether user needs to change password
    /// </summary>
    public bool RequiresPasswordChange { get; set; }

    /// <summary>
    /// Whether two-factor authentication is required
    /// </summary>
    public bool RequiresTwoFactor { get; set; }

    /// <summary>
    /// Lockout time remaining if account is locked
    /// </summary>
    public TimeSpan? LockoutTimeRemaining { get; set; }

    /// <summary>
    /// Device fingerprint used for token validation
    /// Must be sent back with refresh token requests
    /// </summary>
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();
}