using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Application.DTOs.Auth;

namespace VisitorManagementSystem.Api.Middleware;

/// <summary>
/// Custom authentication middleware for cookie-based authentication
/// </summary>
public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for certain paths
        if (ShouldSkipAuthentication(context.Request.Path))
        {
            await _next(context);
            return;
        }

        try
        {
            await ProcessAuthenticationAsync(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in authentication middleware");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Authentication processing error");
            return;
        }

        await _next(context);
    }

    private async Task ProcessAuthenticationAsync(HttpContext context)
    {
        var authService = context.RequestServices.GetRequiredService<IAuthService>();

        // Extract tokens from cookies
        var tokenInfo = authService.ExtractTokensFromCookies(context.Request);

        if (tokenInfo?.AccessToken != null)
        {
            // Validate the access token
            var validation = await authService.ValidateTokenAsync(tokenInfo.AccessToken);

            if (validation.IsValid && validation.UserId.HasValue)
            {
                // Token is valid, set user context
                SetUserContext(context, validation);
                _logger.LogDebug("User authenticated via cookie: {UserId}", validation.UserId);
            }
            else if (tokenInfo.RefreshToken != null)
            {
                // Access token invalid/expired, try refresh
                await TryRefreshTokenAsync(context, authService, tokenInfo.RefreshToken);
            }
            else
            {
                // Invalid token and no refresh token, clear cookies
                authService.ClearAuthenticationCookies(context.Response);
                _logger.LogDebug("Invalid access token, cookies cleared");
            }
        }
    }

    private async Task TryRefreshTokenAsync(HttpContext context, IAuthService authService, string refreshToken)
    {
        try
        {
            var refreshResult = await authService.RefreshTokenAsync(
                new VisitorManagementSystem.Api.Application.DTOs.Auth.RefreshTokenRequestDto
                {
                    RefreshToken = refreshToken
                },
                GetClientIpAddress(context),
                context.Request.Headers["User-Agent"].FirstOrDefault());

            if (refreshResult.IsSuccess)
            {
                // Update cookies with new tokens
                authService.SetAuthenticationCookies(context.Response, refreshResult, context.Request.IsHttps);

                // Set user context
                if (refreshResult.User != null)
                {
                    var validation = await authService.ValidateTokenAsync(refreshResult.AccessToken!);
                    if (validation.IsValid)
                    {
                        SetUserContext(context, validation);
                        _logger.LogDebug("Token refreshed for user: {UserId}", refreshResult.User.Id);
                    }
                }
            }
            else
            {
                // Refresh failed, clear cookies
                authService.ClearAuthenticationCookies(context.Response);
                _logger.LogDebug("Token refresh failed, cookies cleared");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during token refresh");
            authService.ClearAuthenticationCookies(context.Response);
        }
    }

    private static void SetUserContext(HttpContext context, TokenValidationResult validation)
    {
        var claims = new List<System.Security.Claims.Claim>();

        if (validation.UserId.HasValue)
            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, validation.UserId.Value.ToString()));

        if (!string.IsNullOrEmpty(validation.UserEmail))
            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, validation.UserEmail));

        // Add roles
        foreach (var role in validation.Roles)
        {
            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));
        }

        // Add permissions (now fetched from database in AuthService.ValidateTokenAsync)
        foreach (var permission in validation.Permissions)
        {
            claims.Add(new System.Security.Claims.Claim("permission", permission));
        }

        if (!string.IsNullOrEmpty(validation.SecurityStamp))
            claims.Add(new System.Security.Claims.Claim("security_stamp", validation.SecurityStamp));

        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Cookie");
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);
    }

    private static bool ShouldSkipAuthentication(PathString path)
    {
        var skipPaths = new[]
        {
            "/health",
            "/swagger",
            "/api/auth/login",
            "/api/auth/refresh",
            "/api/auth/forgot-password",
            "/api/auth/reset-password",
            "/api/auth/validate-token"
        };

        return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
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
