namespace VisitorManagementSystem.Api.Application.DTOs.Auth;

/// <summary>
/// Cookie token information data transfer object
/// </summary>
public class CookieTokenInfo
{
    /// <summary>
    /// Access token from cookie
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Refresh token from cookie
    /// </summary>
    public string? RefreshToken { get; set; }
    public bool HasValidTokens => !string.IsNullOrEmpty(AccessToken) && !string.IsNullOrEmpty(RefreshToken);
}