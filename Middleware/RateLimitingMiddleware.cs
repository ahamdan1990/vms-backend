using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net;

namespace VisitorManagementSystem.Api.Middleware;

/// <summary>
/// Rate limiting middleware
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly RateLimitOptions _options;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        IOptions<RateLimitOptions> options,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        var endpoint = GetEndpointKey(context);

        if (await IsRateLimitExceededAsync(clientId, endpoint, context))
        {
            await HandleRateLimitExceeded(context, clientId, endpoint);
            return;
        }

        await _next(context);
    }

    private async Task<bool> IsRateLimitExceededAsync(string clientId, string endpoint, HttpContext context)
    {
        var rule = GetApplicableRule(endpoint, context);
        if (rule == null) return false;

        var key = $"rate_limit:{clientId}:{endpoint}";
        var requestCount = _cache.Get<RequestCounter>(key) ?? new RequestCounter();

        // Clean up old requests
        var cutoff = DateTime.UtcNow.Subtract(rule.Window);
        requestCount.Requests.RemoveAll(r => r < cutoff);

        // Check if limit exceeded
        if (requestCount.Requests.Count >= rule.Limit)
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Endpoint}. Count: {Count}, Limit: {Limit}",
                clientId, endpoint, requestCount.Requests.Count, rule.Limit);
            return true;
        }

        // Add current request
        requestCount.Requests.Add(DateTime.UtcNow);

        // Update cache
        _cache.Set(key, requestCount, rule.Window);

        return false;
    }

    private async Task HandleRateLimitExceeded(HttpContext context, string clientId, string endpoint)
    {
        var rule = GetApplicableRule(endpoint, context);

        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";

        // Add rate limit headers
        context.Response.Headers.Add("X-RateLimit-Limit", rule?.Limit.ToString() ?? "0");
        context.Response.Headers.Add("X-RateLimit-Remaining", "0");
        context.Response.Headers.Add("X-RateLimit-Reset", DateTimeOffset.UtcNow.Add(rule?.Window ?? TimeSpan.Zero).ToUnixTimeSeconds().ToString());
        context.Response.Headers.Add("Retry-After", ((int)(rule?.Window.TotalSeconds ?? 60)).ToString());

        var response = new
        {
            success = false,
            message = "Rate limit exceeded. Please try again later.",
            error = "too_many_requests",
            retryAfter = rule?.Window.TotalSeconds ?? 60,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get user ID first
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // Fall back to IP address
        var ipAddress = GetClientIpAddress(context);
        return $"ip:{ipAddress ?? "unknown"}";
    }

    private string GetEndpointKey(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "";

        // Normalize path (remove IDs, etc.)
        foreach (var pattern in _options.EndpointPatterns)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch($"{method} {path}", pattern.Key))
            {
                return pattern.Value;
            }
        }

        return $"{method} {path}";
    }

    private RateLimitRule? GetApplicableRule(string endpoint, HttpContext context)
    {
        // Check for specific endpoint rules first
        if (_options.Rules.TryGetValue(endpoint, out var specificRule))
        {
            return specificRule;
        }

        // Check for authentication-based rules
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            return _options.AuthenticatedUserRule;
        }

        // Default to anonymous user rule
        return _options.AnonymousUserRule;
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

    private class RequestCounter
    {
        public List<DateTime> Requests { get; set; } = new();
    }
}

/// <summary>
/// Rate limiting options
/// </summary>
public class RateLimitOptions
{
    public bool Enabled { get; set; } = true;
    public Dictionary<string, RateLimitRule> Rules { get; set; } = new();
    public RateLimitRule? AuthenticatedUserRule { get; set; }
    public RateLimitRule? AnonymousUserRule { get; set; }
    public Dictionary<string, string> EndpointPatterns { get; set; } = new();
}

/// <summary>
/// Rate limiting rule
/// </summary>
public class RateLimitRule
{
    public int Limit { get; set; }
    public TimeSpan Window { get; set; }
}