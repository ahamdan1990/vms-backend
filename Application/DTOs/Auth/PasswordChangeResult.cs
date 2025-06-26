namespace VisitorManagementSystem.Api.Application.DTOs.Auth;

/// <summary>
/// Password change result data transfer object
/// </summary>
public class PasswordChangeResult
{
    /// <summary>
    /// Password change success status
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if password change failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether re-authentication is required
    /// </summary>
    public bool RequiresReauthentication { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();
}