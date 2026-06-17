using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.Services.Licensing;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Middleware;

public class LicenseEnforcementMiddleware
{
    private static readonly string[] ExemptPrefixes =
    [
        "/api/license",
        "/api/auth",       // login and token refresh must work before activation
        "/health",
        "/hubs",
        "/swagger",
        "/uploads",
        "/face-snapshots"
    ];

    private readonly RequestDelegate _next;
    private readonly ILogger<LicenseEnforcementMiddleware> _logger;

    public LicenseEnforcementMiddleware(RequestDelegate next, ILogger<LicenseEnforcementMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ILicenseValidatorService licenseValidator)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Non-API paths (static files, SPA) are always allowed
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Exempt paths
        if (IsExempt(path))
        {
            await _next(context);
            return;
        }

        var result = licenseValidator.GetCachedResult();

        if (result.IsValid)
        {
            await _next(context);
            return;
        }

        _logger.LogDebug("Blocking request to {Path} — license status: {Status}", path, result.Status);

        context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
        context.Response.ContentType = "application/json";

        var body = new
        {
            error = "License required",
            status = result.Status.ToString(),
            reason = result.FailureReason,
            activationUrl = "/activate",
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }

    private static bool IsExempt(string path)
    {
        foreach (var prefix in ExemptPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}

public static class LicenseEnforcementMiddlewareExtensions
{
    public static IApplicationBuilder UseLicenseEnforcement(this IApplicationBuilder app)
        => app.UseMiddleware<LicenseEnforcementMiddleware>();
}
