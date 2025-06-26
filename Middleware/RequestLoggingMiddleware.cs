using System.Diagnostics;
using System.Text;

namespace VisitorManagementSystem.Api.Middleware;

/// <summary>
/// Request logging middleware
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = context.TraceIdentifier;

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

        // Log slow requests
        if (elapsedMs > 5000) // 5 seconds
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
}