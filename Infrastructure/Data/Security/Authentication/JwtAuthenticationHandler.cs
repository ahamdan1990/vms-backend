using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using VisitorManagementSystem.Api.Configuration;

namespace VisitorManagementSystem.Api.Infrastructure.Security.Authentication;

/// <summary>
/// Custom JWT authentication handler
/// </summary>
public class JwtAuthenticationHandler : AuthenticationHandler<JwtAuthenticationSchemeOptions>
{
    private readonly JwtConfiguration _jwtConfig;
    private readonly ILogger<JwtAuthenticationHandler> _logger;

    public JwtAuthenticationHandler(
        IOptionsMonitor<JwtAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IOptions<JwtConfiguration> jwtConfig)
        : base(options, logger, encoder, clock)
    {
        _jwtConfig = jwtConfig.Value;
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

    private async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtConfig.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtConfig.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtConfig.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RequireExpirationTime = true
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