namespace VisitorManagementSystem.Api.Application.DTOs.Auth;

/// <summary>
/// Logout result data transfer object
/// </summary>
public class LogoutResult
{
    /// <summary>
    /// Logout success status
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Result message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Number of tokens revoked
    /// </summary>
    public int TokensRevoked { get; set; }

    /// <summary>
    /// Error message if logout failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}
