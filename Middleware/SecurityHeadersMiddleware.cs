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
        // Add security headers
        AddSecurityHeaders(context);

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var response = context.Response; // For convenience

        // Prevent MIME-type confusion attacks
        if (!response.Headers.ContainsKey("X-Content-Type-Options"))
            response.Headers.Add("X-Content-Type-Options", "nosniff");

        // Prevent clickjacking attacks
        if (!response.Headers.ContainsKey("X-Frame-Options"))
            response.Headers.Add("X-Frame-Options", _options.FrameOptions);

        // XSS Protection (legacy but still useful)
        if (!response.Headers.ContainsKey("X-XSS-Protection"))
            response.Headers.Add("X-XSS-Protection", "1; mode=block");

        // Control referrer information
        if (!response.Headers.ContainsKey("Referrer-Policy"))
            response.Headers.Add("Referrer-Policy", _options.ReferrerPolicy);

        // Content Security Policy
        if (!string.IsNullOrEmpty(_options.ContentSecurityPolicy) &&
            !response.Headers.ContainsKey("Content-Security-Policy"))
            response.Headers.Add("Content-Security-Policy", _options.ContentSecurityPolicy);

        // Strict Transport Security (HSTS)
        if (_options.UseHsts && context.Request.IsHttps && // This line is now fixed
            !response.Headers.ContainsKey("Strict-Transport-Security"))
        {
            var hstsValue = $"max-age={_options.HstsMaxAge}";
            if (_options.HstsIncludeSubdomains)
                hstsValue += "; includeSubDomains";
            if (_options.HstsPreload)
                hstsValue += "; preload";

            response.Headers.Add("Strict-Transport-Security", hstsValue);
        }

        // Permissions Policy (formerly Feature Policy)
        if (!string.IsNullOrEmpty(_options.PermissionsPolicy) &&
            !response.Headers.ContainsKey("Permissions-Policy"))
            response.Headers.Add("Permissions-Policy", _options.PermissionsPolicy);

        // Cross-Origin policies for enhanced security
        if (!response.Headers.ContainsKey("Cross-Origin-Embedder-Policy"))
            response.Headers.Add("Cross-Origin-Embedder-Policy", "require-corp");

        if (!response.Headers.ContainsKey("Cross-Origin-Opener-Policy"))
            response.Headers.Add("Cross-Origin-Opener-Policy", "same-origin");

        if (!response.Headers.ContainsKey("Cross-Origin-Resource-Policy"))
            response.Headers.Add("Cross-Origin-Resource-Policy", "same-origin");

        // Remove server information
        if (_options.RemoveServerHeader)
        {
            response.Headers.Remove("Server");
            response.Headers.Remove("X-Powered-By");
            response.Headers.Remove("X-AspNet-Version");
            response.Headers.Remove("X-AspNetMvc-Version");
        }

        // Add custom headers
        foreach (var header in _options.CustomHeaders)
        {
            if (!response.Headers.ContainsKey(header.Key))
                response.Headers.Add(header.Key, header.Value);
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