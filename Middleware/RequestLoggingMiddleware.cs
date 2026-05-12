using System.Diagnostics;
using System.Text;
using Serilog.Context;

namespace VisitorManagementSystem.Api.Middleware;

/// <summary>
/// Request logging middleware
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private const string CorrelationIdItemKey = ResponseMetadataMiddleware.CorrelationIdItemKey;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = EnsureCorrelationId(context);

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey("X-Correlation-ID"))
            {
                context.Response.Headers["X-Correlation-ID"] = correlationId;
            }

            if (!context.Response.Headers.ContainsKey("X-Timestamp"))
            {
                context.Response.Headers["X-Timestamp"] = DateTime.UtcNow.ToString("o");
            }

            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Log request
            LogRequest(context, correlationId);

            // Capture response body if needed
            var originalResponseBody = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // Copy response back
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalResponseBody);
                context.Response.Body = originalResponseBody;

                // Log response
                LogResponse(context, correlationId, stopwatch.ElapsedMilliseconds, responseBodyStream);
            }
        }
    }

    private void LogRequest(HttpContext context, string correlationId)
    {
        var request = context.Request;
        var ipAddress = GetClientIpAddress(context);
        var userAgent = request.Headers["User-Agent"].FirstOrDefault();
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation(
            "HTTP Request: {Method} {Scheme}://{Host}{Path}{QueryString} - IP: {IpAddress} - User: {UserId} - CorrelationId: {CorrelationId} - UserAgent: {UserAgent}",
            request.Method,
            request.Scheme,
            request.Host,
            request.Path,
            request.QueryString,
            ipAddress,
            userId ?? "Anonymous",
            correlationId,
            userAgent);
    }

    private void LogResponse(HttpContext context, string correlationId, long elapsedMs, MemoryStream responseBody)
    {
        var response = context.Response;
        var request = context.Request;
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var logLevel = response.StatusCode >= 500 ? LogLevel.Error :
                      response.StatusCode >= 400 ? LogLevel.Warning :
                      LogLevel.Information;

        _logger.Log(logLevel,
            "HTTP Response: {Method} {Path} - Status: {StatusCode} - Duration: {Duration}ms - Size: {Size}B - User: {UserId} - CorrelationId: {CorrelationId}",
            request.Method,
            request.Path,
            response.StatusCode,
            elapsedMs,
            responseBody.Length,
            userId ?? "Anonymous",
            correlationId);

        // Log slow requests. SignalR/WebSocket requests are long-lived by design.
        if (elapsedMs > 5000 && !IsLongRunningConnection(request)) // 5 seconds
        {
            _logger.LogWarning(
                "Slow Request Detected: {Method} {Path} took {Duration}ms - CorrelationId: {CorrelationId}",
                request.Method,
                request.Path,
                elapsedMs,
                correlationId);
        }
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private static string EnsureCorrelationId(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();

        if (!IsValidCorrelationId(correlationId))
        {
            correlationId = context.Request.Headers["X-Request-ID"].FirstOrDefault();
        }

        if (!IsValidCorrelationId(correlationId))
        {
            correlationId = context.Items[CorrelationIdItemKey]?.ToString();
        }

        if (!IsValidCorrelationId(correlationId))
        {
            correlationId = context.Items["CorrelationId"]?.ToString();
        }

        if (!IsValidCorrelationId(correlationId))
        {
            correlationId = context.TraceIdentifier;
        }

        string resolvedCorrelationId;
        if (IsValidCorrelationId(correlationId))
        {
            resolvedCorrelationId = correlationId!;
        }
        else
        {
            resolvedCorrelationId = Guid.NewGuid().ToString("D");
        }

        context.TraceIdentifier = resolvedCorrelationId;
        context.Items[CorrelationIdItemKey] = resolvedCorrelationId;
        context.Items["CorrelationId"] = resolvedCorrelationId;
        return resolvedCorrelationId;
    }

    private static bool IsValidCorrelationId(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Length <= 128 &&
               !value.Any(char.IsControl);
    }

    private static bool IsLongRunningConnection(HttpRequest request)
    {
        if (request.Path.StartsWithSegments("/hubs"))
        {
            return true;
        }

        if (HttpMethods.IsConnect(request.Method))
        {
            return true;
        }

        var upgradeHeader = request.Headers.Upgrade.FirstOrDefault();
        if (string.Equals(upgradeHeader, "websocket", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return request.Headers.Connection.Any(value =>
            !string.IsNullOrWhiteSpace(value) &&
            value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Any(token => string.Equals(token, "Upgrade", StringComparison.OrdinalIgnoreCase)));
    }
}
