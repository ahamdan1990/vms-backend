using System.Text.Json;
using VisitorManagementSystem.Api.Application.Services.Configuration;

namespace VisitorManagementSystem.Api.Middleware;

public class MaintenanceModeMiddleware
{
    private static readonly string[] ExemptPrefixes =
    [
        "/api/auth/login",
        "/api/auth/refresh",
        "/api/license",
        "/health",
        "/swagger",
        "/hubs",
    ];

    private readonly RequestDelegate _next;
    private readonly ILogger<MaintenanceModeMiddleware> _logger;

    public MaintenanceModeMiddleware(RequestDelegate next, ILogger<MaintenanceModeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IDynamicConfigurationService config)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Static files and SPA pass through unconditionally
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Administrator users always bypass maintenance mode
        if (context.User.IsInRole("Administrator"))
        {
            await _next(context);
            return;
        }

        // Paths that must remain accessible during maintenance
        if (ExemptPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var maintenanceMode = await config.GetConfigurationAsync<bool>("Application", "MaintenanceMode", false);
        if (!maintenanceMode)
        {
            await _next(context);
            return;
        }

        var message = await config.GetConfigurationAsync<string>(
            "Application", "MaintenanceMessage",
            "The system is currently under maintenance. Please try again later.");

        _logger.LogDebug("Maintenance mode active — returning 503 for {Path}", path);

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(new
        {
            error = message,
            maintenanceMode = true,
        });

        await context.Response.WriteAsync(body);
    }
}
