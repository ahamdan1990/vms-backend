// Middleware/SecurityHeadersMiddleware.cs
using Microsoft.Extensions.Options;

namespace VisitorManagementSystem.Api.Middleware;

/// <summary>
/// Middleware to add security headers to all responses
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityHeadersOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers BEFORE the request
        AddSecurityHeaders(context);

        // Execute the next middleware/endpoint
        await _next(context);

        // Allow endpoints to override CORP header for file serving (like photos)
        // This handles cases where endpoints need to serve cross-origin resources
        // The endpoint can set the header and we won't override it
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var response = context.Response;

        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["X-Frame-Options"] = _options.FrameOptions;
        response.Headers["X-XSS-Protection"] = "1; mode=block";
        response.Headers["Referrer-Policy"] = _options.ReferrerPolicy;

        if (!string.IsNullOrEmpty(_options.ContentSecurityPolicy))
            response.Headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;

        if (_options.UseHsts && context.Request.IsHttps)
        {
            var hstsValue = $"max-age={_options.HstsMaxAge}";
            if (_options.HstsIncludeSubdomains) hstsValue += "; includeSubDomains";
            if (_options.HstsPreload) hstsValue += "; preload";
            response.Headers["Strict-Transport-Security"] = hstsValue;
        }

        if (!string.IsNullOrEmpty(_options.PermissionsPolicy))
            response.Headers["Permissions-Policy"] = _options.PermissionsPolicy;

        response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
        response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
        response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";

        if (_options.RemoveServerHeader)
        {
            response.Headers.Remove("Server");
            response.Headers.Remove("X-Powered-By");
            response.Headers.Remove("X-AspNet-Version");
            response.Headers.Remove("X-AspNetMvc-Version");
        }

        foreach (var header in _options.CustomHeaders)
        {
            response.Headers[header.Key] = header.Value;
        }
    }
}

/// <summary>
/// Security headers configuration options
/// </summary>
public class SecurityHeadersOptions
{
    public string FrameOptions { get; set; } = "DENY";
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
    public string? ContentSecurityPolicy { get; set; }
    public bool UseHsts { get; set; } = true;
    public int HstsMaxAge { get; set; } = 31536000; // 1 year
    public bool HstsIncludeSubdomains { get; set; } = true;
    public bool HstsPreload { get; set; } = false;
    public string? PermissionsPolicy { get; set; }
    public bool RemoveServerHeader { get; set; } = true;
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
}