using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace VisitorManagementSystem.Api.Infrastructure.Security.Authentication;

/// <summary>
/// API Key authentication handler for external integrations
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
{
    private readonly ILogger<ApiKeyAuthenticationHandler> _logger;
    private readonly Dictionary<string, ApiKeyInfo> _apiKeys;
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _logger = logger.CreateLogger<ApiKeyAuthenticationHandler>();
        _configuration = configuration;
        _apiKeys = LoadApiKeys();
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            // Check for API key in header
            if (!Request.Headers.TryGetValue(Options.ApiKeyHeaderName, out var apiKeyValues))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var apiKey = apiKeyValues.FirstOrDefault();
            if (string.IsNullOrEmpty(apiKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing API key"));
            }

            // Validate API key
            if (!_apiKeys.TryGetValue(apiKey, out var apiKeyInfo))
            {
                _logger.LogWarning("Invalid API key attempted from {IP}", GetClientIpAddress());
                return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
            }

            // Check if API key is active
            if (!apiKeyInfo.IsActive)
            {
                _logger.LogWarning("Inactive API key attempted: {Name}", apiKeyInfo.Name);
                return Task.FromResult(AuthenticateResult.Fail("API key is inactive"));
            }

            // Check expiration
            if (apiKeyInfo.ExpiryDate.HasValue && apiKeyInfo.ExpiryDate.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("Expired API key attempted: {Name}", apiKeyInfo.Name);
                return Task.FromResult(AuthenticateResult.Fail("API key has expired"));
            }

            // Check IP restrictions
            if (apiKeyInfo.AllowedIPs.Any())
            {
                var clientIp = GetClientIpAddress();
                if (!string.IsNullOrEmpty(clientIp) && !apiKeyInfo.AllowedIPs.Contains(clientIp))
                {
                    _logger.LogWarning("API key used from unauthorized IP: {IP}, Key: {Name}", clientIp, apiKeyInfo.Name);
                    return Task.FromResult(AuthenticateResult.Fail("API key not authorized for this IP address"));
                }
            }

            // Create claims principal
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, apiKeyInfo.Name),
                new(ClaimTypes.NameIdentifier, apiKeyInfo.Id),
                new("api_key_id", apiKeyInfo.Id),
                new("authentication_method", "api_key")
            };

            // Add scope claims
            foreach (var scope in apiKeyInfo.Scopes)
            {
                claims.Add(new Claim("scope", scope));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);

            // Log successful authentication
            _logger.LogInformation("API key authentication successful: {Name}", apiKeyInfo.Name);

            // Update last used timestamp
            apiKeyInfo.LastUsed = DateTime.UtcNow;

            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during API key authentication");
            return Task.FromResult(AuthenticateResult.Fail("Authentication failed"));
        }
    }

    private string? GetClientIpAddress()
    {
        // Try to get real IP from various headers
        var headers = new[] { "X-Forwarded-For", "X-Real-IP", "CF-Connecting-IP" };

        foreach (var header in headers)
        {
            if (Request.Headers.TryGetValue(header, out var values))
            {
                var ip = values.FirstOrDefault()?.Split(',')[0].Trim();
                if (!string.IsNullOrEmpty(ip))
                {
                    return ip;
                }
            }
        }

        return Request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private Dictionary<string, ApiKeyInfo> LoadApiKeys()
    {
        var result = new Dictionary<string, ApiKeyInfo>(StringComparer.Ordinal);
        foreach (var entry in _configuration.GetSection("ApiKeys:Integrations").GetChildren())
        {
            var key = entry["Key"];
            if (string.IsNullOrWhiteSpace(key))
                continue;

            result[key] = new ApiKeyInfo
            {
                Id = entry["Id"] ?? entry.Key,
                Name = entry["Name"] ?? entry.Key,
                IsActive = entry.GetValue("IsActive", true),
                CreatedDate = entry.GetValue<DateTime?>("CreatedDate") ?? DateTime.UtcNow,
                ExpiryDate = entry.GetValue<DateTime?>("ExpiryDate"),
                Scopes = entry.GetSection("Scopes").Get<string[]>() ?? Array.Empty<string>(),
                AllowedIPs = entry.GetSection("AllowedIPs").Get<List<string>>() ?? new List<string>()
            };
        }

        return result;
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.ContentType = "application/json";

        var response = new
        {
            error = "unauthorized",
            message = "Valid API key required"
        };

        await Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
}

/// <summary>
/// API Key authentication scheme options
/// </summary>
public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public string ApiKeyHeaderName { get; set; } = "X-API-Key";
}

/// <summary>
/// API Key information
/// </summary>
public class ApiKeyInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? LastUsed { get; set; }
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public List<string> AllowedIPs { get; set; } = new();
}
