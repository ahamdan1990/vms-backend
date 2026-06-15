using Microsoft.AspNetCore.Http;

namespace VisitorManagementSystem.Api.Application.Services.Common;

/// <summary>
/// Service for resolving dynamic URLs based on current HTTP context
/// This ensures the application works on any server without hardcoded IPs
/// </summary>
public class UrlResolverService : IUrlResolverService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UrlResolverService> _logger;

    public UrlResolverService(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<UrlResolverService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Gets the base URL of the current application (e.g., "https://vms.example.com")
    /// Dynamically resolved from HttpContext
    /// </summary>
    public string GetBaseUrl()
    {
        // Try to get from HttpContext first (most accurate, works with any IP/domain)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var request = httpContext.Request;
            var scheme = request.Scheme; // http or https
            var host = request.Host.Value; // hostname:port

            var baseUrl = $"{scheme}://{host}";
            _logger.LogDebug("Resolved base URL from HttpContext: {BaseUrl}", baseUrl);
            return baseUrl;
        }

        // Fallback to configuration if HttpContext is not available
        // This happens in background services or during startup
        var configuredBaseUrl = _configuration["BaseUrl"];
        if (!string.IsNullOrEmpty(configuredBaseUrl))
        {
            _logger.LogDebug("Using configured base URL: {BaseUrl}", configuredBaseUrl);
            return configuredBaseUrl;
        }

        // Last resort fallback - localhost
        var fallbackUrl = "https://localhost:7000";
        _logger.LogWarning("No HttpContext or configured BaseUrl available, using fallback: {BaseUrl}", fallbackUrl);
        return fallbackUrl;
    }

    /// <summary>
    /// Converts a relative path to an absolute URL using the current server's base URL
    /// </summary>
    /// <param name="relativePath">Relative path (e.g., "uploads/profiles/photo.jpg")</param>
    /// <returns>Absolute URL (e.g., "https://vms.example.com/uploads/profiles/photo.jpg")</returns>
    public string GetAbsoluteUrl(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return GetBaseUrl();
        }

        var baseUrl = GetBaseUrl();

        // Normalize the relative path - ensure it starts with /
        var normalizedPath = relativePath.TrimStart('/').Replace('\\', '/');

        return $"{baseUrl.TrimEnd('/')}/{normalizedPath}";
    }

    /// <summary>
    /// Gets the base URL for API endpoints
    /// </summary>
    /// <param name="relativePath">Optional relative path to append</param>
    /// <returns>Full API URL</returns>
    public string GetApiUrl(string? relativePath = null)
    {
        var baseUrl = GetBaseUrl();

        if (string.IsNullOrEmpty(relativePath))
        {
            return $"{baseUrl.TrimEnd('/')}/api";
        }

        var normalizedPath = relativePath.TrimStart('/').Replace('\\', '/');
        return $"{baseUrl.TrimEnd('/')}/api/{normalizedPath}";
    }
}
