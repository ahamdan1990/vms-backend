namespace VisitorManagementSystem.Api.Application.DTOs.Auth;

/// <summary>
/// Authentication result data transfer object
/// </summary>
public class AuthenticationResult
{
    /// <summary>
    /// Authentication success status
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if authentication failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Access token
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Refresh token
    /// </summary>
    public string? RefreshToken { get; set; }

    public string? DeviceFingerprint { get; set; }

    /// <summary>
    /// Access token expiry date
    /// </summary>
    public DateTime? AccessTokenExpiry { get; set; }

    /// <summary>
    /// Refresh token expiry date
    /// </summary>
    public DateTime? RefreshTokenExpiry { get; set; }

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
    /// List of authentication errors
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
