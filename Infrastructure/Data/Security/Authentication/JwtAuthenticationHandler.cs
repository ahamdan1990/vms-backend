using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using VisitorManagementSystem.Api.Application.Services.Configuration;
using VisitorManagementSystem.Api.Configuration;

namespace VisitorManagementSystem.Api.Infrastructure.Security.Authentication;

/// <summary>
/// Custom JWT authentication handler
/// </summary>
public class JwtAuthenticationHandler : AuthenticationHandler<JwtAuthenticationSchemeOptions>
{
    private readonly IDynamicConfigurationService _dynamicConfig;
    private readonly ILogger<JwtAuthenticationHandler> _logger;
    private JwtConfiguration? _cachedConfig;
    private DateTime _lastConfigLoad = DateTime.MinValue;
    private readonly TimeSpan _configCacheExpiry = TimeSpan.FromMinutes(5);

    public JwtAuthenticationHandler(
        IOptionsMonitor<JwtAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IDynamicConfigurationService dynamicConfig)
        : base(options, logger, encoder, clock)
    {
        _dynamicConfig = dynamicConfig;
        _logger = logger.CreateLogger<JwtAuthenticationHandler>();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            // Check if Authorization header exists
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                return AuthenticateResult.NoResult();
            }

            var authHeader = Request.Headers["Authorization"].ToString();
            if (!authHeader.StartsWith("Bearer "))
            {
                return AuthenticateResult.Fail("Invalid authorization header format");
            }

            var token = authHeader["Bearer ".Length..].Trim();
            if (string.IsNullOrEmpty(token))
            {
                return AuthenticateResult.Fail("Missing JWT token");
            }

            // Validate token
            var principal = await ValidateTokenAsync(token);
            if (principal == null)
            {
                return AuthenticateResult.Fail("Invalid JWT token");
            }

            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during JWT authentication");
            return AuthenticateResult.Fail("Authentication failed");
        }
    }

    private async Task<JwtConfiguration> GetJwtConfigurationAsync()
    {
        // Check if we have cached config and it's not expired
        if (_cachedConfig != null && DateTime.UtcNow - _lastConfigLoad < _configCacheExpiry)
        {
            return _cachedConfig;
        }

        try
        {
            _cachedConfig = new JwtConfiguration
            {
                SecretKey = await _dynamicConfig.GetConfigurationAsync<string>("JWT", "SecretKey", ""),
                Issuer = await _dynamicConfig.GetConfigurationAsync<string>("JWT", "Issuer", ""),
                Audience = await _dynamicConfig.GetConfigurationAsync<string>("JWT", "Audience", ""),
                ExpiryInMinutes = await _dynamicConfig.GetConfigurationAsync<int>("JWT", "ExpiryInMinutes", 15),
                RefreshTokenExpiryInDays = await _dynamicConfig.GetConfigurationAsync<int>("JWT", "RefreshTokenExpiryInDays", 7),
                ValidateIssuerSigningKey = await _dynamicConfig.GetConfigurationAsync<bool>("JWT", "ValidateIssuerSigningKey", true),
                ValidateIssuer = await _dynamicConfig.GetConfigurationAsync<bool>("JWT", "ValidateIssuer", true),
                ValidateAudience = await _dynamicConfig.GetConfigurationAsync<bool>("JWT", "ValidateAudience", true),
                ValidateLifetime = await _dynamicConfig.GetConfigurationAsync<bool>("JWT", "ValidateLifetime", true),
                ClockSkewMinutes = await _dynamicConfig.GetConfigurationAsync<int>("JWT", "ClockSkewMinutes", 0),
                RequireExpirationTime = await _dynamicConfig.GetConfigurationAsync<bool>("JWT", "RequireExpirationTime", true)
            };

            _lastConfigLoad = DateTime.UtcNow;
            return _cachedConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load JWT configuration from dynamic config, using defaults");
            
            // Return default configuration if dynamic config fails
            return new JwtConfiguration
            {
                SecretKey = "default-secret-key-change-this",
                Issuer = "VMS",
                Audience = "VMS-Users",
                ExpiryInMinutes = 15,
                RefreshTokenExpiryInDays = 7,
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkewMinutes = 0,
                RequireExpirationTime = true
            };
        }
    }

    private async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            var jwtConfig = await GetJwtConfigurationAsync();
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(jwtConfig.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = jwtConfig.ValidateIssuerSigningKey,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = jwtConfig.ValidateIssuer,
                ValidIssuer = jwtConfig.Issuer,
                ValidateAudience = jwtConfig.ValidateAudience,
                ValidAudience = jwtConfig.Audience,
                ValidateLifetime = jwtConfig.ValidateLifetime,
                ClockSkew = TimeSpan.FromMinutes(jwtConfig.ClockSkewMinutes),
                RequireExpirationTime = jwtConfig.RequireExpirationTime
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            // Additional security checks
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            // Check token blacklist (if implemented)
            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (!string.IsNullOrEmpty(jti) && await IsTokenBlacklistedAsync(jti))
            {
                return null;
            }

            // Validate security stamp
            var securityStamp = principal.FindFirst("security_stamp")?.Value;
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(securityStamp))
            {
                if (!await ValidateSecurityStampAsync(userId, securityStamp))
                {
                    return null;
                }
            }

            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed for token: {Token}", token.Substring(0, Math.Min(token.Length, 20)) + "...");
            return null;
        }
    }

    private async Task<bool> IsTokenBlacklistedAsync(string jti)
    {
        // Implement token blacklist checking
        // This could check a cache or database for revoked tokens
        await Task.CompletedTask;
        return false;
    }

    private async Task<bool> ValidateSecurityStampAsync(string userId, string securityStamp)
    {
        // Implement security stamp validation
        // This should check against the user's current security stamp in the database
        await Task.CompletedTask;
        return true;
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.ContentType = "application/json";

        var response = new
        {
            error = "unauthorized",
            message = "Authentication required"
        };

        await Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        Response.ContentType = "application/json";

        var response = new
        {
            error = "forbidden",
            message = "Insufficient permissions"
        };

        await Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
}

/// <summary>
/// JWT authentication scheme options
/// </summary>
public class JwtAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "JWT";
    public string TokenHeaderName { get; set; } = "Authorization";
    public string TokenPrefix { get; set; } = "Bearer";
}