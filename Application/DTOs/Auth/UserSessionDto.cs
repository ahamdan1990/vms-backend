namespace VisitorManagementSystem.Api.Application.DTOs.Auth;

/// <summary>
/// User session data transfer object
/// </summary>
public class UserSessionDto
{
    /// <summary>
    /// Session ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Device fingerprint
    /// </summary>
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    /// IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Device type
    /// </summary>
    public string DeviceType { get; set; } = "Unknown";

    /// <summary>
    /// Geographic location
    /// </summary>
    public string Location { get; set; } = "Unknown";

    /// <summary>
    /// Session creation date
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Session expiry date
    /// </summary>
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// Last activity date
    /// </summary>
    public DateTime? LastUsed { get; set; }

    /// <summary>
    /// Whether session is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether this is the current session
    /// </summary>
    public bool IsCurrent { get; set; } = false;
}