namespace VisitorManagementSystem.Api.Application.DTOs.Auth;

/// <summary>
/// Token validation result data transfer object
/// </summary>
public class TokenValidationResult
{
    /// <summary>
    /// Token validation success status
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// User ID from token
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// User email from token
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// User roles from token
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// User permissions from token
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// Token expiry date
    /// </summary>
    public DateTime? Expiry { get; set; }

    /// <summary>
    /// Security stamp from token
    /// </summary>
    public string? SecurityStamp { get; set; }

    /// <summary>
    /// Error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether token is expired
    /// </summary>
    public bool? IsExpired { get; set; }

    /// <summary>
    /// Whether token is near expiry
    /// </summary>
    public bool? IsNearExpiry { get; set; }
}
