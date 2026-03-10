namespace VisitorManagementSystem.Api.Application.Services.Common;

/// <summary>
/// Service for resolving dynamic URLs based on current HTTP context
/// This ensures the application works on any server without hardcoded IPs
/// </summary>
public interface IUrlResolverService
{
    /// <summary>
    /// Gets the base URL of the current application (e.g., "https://192.168.0.59:7000")
    /// Dynamically resolved from HttpContext
    /// </summary>
    string GetBaseUrl();

    /// <summary>
    /// Converts a relative path to an absolute URL using the current server's base URL
    /// </summary>
    /// <param name="relativePath">Relative path (e.g., "uploads/profiles/photo.jpg")</param>
    /// <returns>Absolute URL (e.g., "https://192.168.0.59:7000/uploads/profiles/photo.jpg")</returns>
    string GetAbsoluteUrl(string relativePath);

    /// <summary>
    /// Gets the base URL for API endpoints
    /// </summary>
    /// <param name="relativePath">Optional relative path to append</param>
    /// <returns>Full API URL</returns>
    string GetApiUrl(string? relativePath = null);
}
