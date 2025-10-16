namespace VisitorManagementSystem.Api.Application.DTOs.Auth;

/// <summary>
/// Password reset result data transfer object
/// </summary>
public class PasswordResetResultDto
{
    /// <summary>
    /// Password reset success status
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if password reset failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Reset token (for development only)
    /// </summary>
    public string? ResetToken { get; set; }

    /// <summary>
    /// Reset token expiry date
    /// </summary>
    public DateTime? ResetTokenExpiry { get; set; }

    /// <summary>
    /// Whether email was sent
    /// </summary>
    public bool EmailSent { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    public string? TemporaryPassword { get; set; }
}