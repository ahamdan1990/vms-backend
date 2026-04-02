namespace VisitorManagementSystem.Api.Middleware;

/// <summary>
/// Normalizes incoming requests onto the configured public BaseUrl.
/// This keeps HTTP and alternate-host requests from splitting cookies,
/// SignalR connections, and SPA navigation across multiple origins.
/// </summary>
public sealed class CanonicalUrlRedirectMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CanonicalUrlRedirectMiddleware> _logger;
    private readonly Uri? _canonicalUri;

    public CanonicalUrlRedirectMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<CanonicalUrlRedirectMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        var configuredBaseUrl = configuration["BaseUrl"];
        if (Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var uri))
        {
            _canonicalUri = uri;
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_canonicalUri == null)
        {
            await _next(context);
            return;
        }

        if (!RequiresRedirect(context.Request, _canonicalUri))
        {
            await _next(context);
            return;
        }

        var redirectUri = BuildRedirectUri(context.Request, _canonicalUri);
        _logger.LogDebug("Redirecting request to canonical URL: {RedirectUri}", redirectUri);

        context.Response.StatusCode = StatusCodes.Status308PermanentRedirect;
        context.Response.Headers.Location = redirectUri;
    }

    private static bool RequiresRedirect(HttpRequest request, Uri canonicalUri)
    {
        if (!string.Equals(request.Scheme, canonicalUri.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.Equals(request.Host.Host, canonicalUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return GetEffectivePort(request.Scheme, request.Host.Port)
            != GetEffectivePort(canonicalUri.Scheme, canonicalUri.IsDefaultPort ? null : canonicalUri.Port);
    }

    private static string BuildRedirectUri(HttpRequest request, Uri canonicalUri)
    {
        var uriBuilder = new UriBuilder(canonicalUri.Scheme, canonicalUri.Host, canonicalUri.IsDefaultPort ? -1 : canonicalUri.Port)
        {
            Path = $"{request.PathBase}{request.Path}",
            Query = request.QueryString.HasValue ? request.QueryString.Value![1..] : string.Empty
        };

        return uriBuilder.Uri.AbsoluteUri;
    }

    private static int GetEffectivePort(string scheme, int? port)
    {
        if (port.HasValue)
        {
            return port.Value;
        }

        return string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ? 443 : 80;
    }
}
