using System.Security.Claims;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Application.Services.Auth;

/// <summary>
/// JWT token service interface for generating and validating JWT tokens
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates an access token for the user
    /// </summary>
    /// <param name="user">User entity</param>
    /// <param name="permissions">User permissions</param>
    /// <param name="additionalClaims">Additional claims to include</param>
    /// <returns>JWT token information</returns>
    Task<JwtTokenResult> GenerateAccessTokenAsync(User user, List<string> permissions,
        Dictionary<string, object>? additionalClaims = null);

    /// <summary>
    /// Generates a refresh token for the user
    /// </summary>
    /// <param name="user">User entity</param>
    /// <param name="jwtId">JWT ID from access token</param>
    /// <param name="deviceFingerprint">Device fingerprint</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <returns>Refresh token entity</returns>
    Task<RefreshToken> GenerateRefreshTokenAsync(User user, string jwtId, string? deviceFingerprint = null,
        string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Validates and parses a JWT token
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <param name="validateLifetime">Whether to validate token expiration</param>
    /// <returns>Token validation result with claims</returns>
    Task<JwtValidationResult> ValidateTokenAsync(string token, bool validateLifetime = true);

    /// <summary>
    /// Extracts claims from a JWT token without validation
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Claims principal</returns>
    ClaimsPrincipal? GetClaimsFromToken(string token);

    /// <summary>
    /// Gets the expiration time of a JWT token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Expiration time or null if invalid</returns>
    DateTime? GetTokenExpiration(string token);

    /// <summary>
    /// Gets the JWT ID from a token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>JWT ID or null if not found</returns>
    string? GetJwtId(string token);

    /// <summary>
    /// Gets the user ID from a token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User ID or null if not found</returns>
    int? GetUserId(string token);

    /// <summary>
    /// Gets the security stamp from a token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Security stamp or null if not found</returns>
    string? GetSecurityStamp(string token);

    /// <summary>
    /// Checks if a token is expired
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>True if token is expired</returns>
    bool IsTokenExpired(string token);

    /// <summary>
    /// Checks if a token is close to expiration
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <param name="thresholdMinutes">Threshold in minutes</param>
    /// <returns>True if token expires within threshold</returns>
    bool IsTokenNearExpiry(string token, int thresholdMinutes = 5);

    /// <summary>
    /// Generates a password reset token
    /// </summary>
    /// <param name="user">User entity</param>
    /// <param name="purpose">Token purpose</param>
    /// <returns>Reset token</returns>
    string GeneratePasswordResetToken(User user, string purpose = "password_reset");

    /// <summary>
    /// Validates a password reset token
    /// </summary>
    /// <param name="token">Reset token</param>
    /// <param name="user">User entity</param>
    /// <param name="purpose">Expected token purpose</param>
    /// <returns>True if token is valid</returns>
    bool ValidatePasswordResetToken(string token, User user, string purpose = "password_reset");

    /// <summary>
    /// Generates an email confirmation token
    /// </summary>
    /// <param name="user">User entity</param>
    /// <returns>Email confirmation token</returns>
    string GenerateEmailConfirmationToken(User user);

    /// <summary>
    /// Validates an email confirmation token
    /// </summary>
    /// <param name="token">Confirmation token</param>
    /// <param name="user">User entity</param>
    /// <returns>True if token is valid</returns>
    bool ValidateEmailConfirmationToken(string token, User user);

    /// <summary>
    /// Generates a two-factor authentication token
    /// </summary>
    /// <param name="user">User entity</param>
    /// <returns>2FA token</returns>
    string GenerateTwoFactorToken(User user);

    /// <summary>
    /// Validates a two-factor authentication token
    /// </summary>
    /// <param name="token">2FA token</param>
    /// <param name="user">User entity</param>
    /// <returns>True if token is valid</returns>
    bool ValidateTwoFactorToken(string token, User user);

    /// <summary>
    /// Creates claims for a user
    /// </summary>
    /// <param name="user">User entity</param>
    /// <param name="permissions">User permissions</param>
    /// <param name="additionalClaims">Additional claims</param>
    /// <returns>List of claims</returns>
    List<Claim> CreateUserClaims(User user, List<string> permissions, Dictionary<string, object>? additionalClaims = null);

    /// <summary>
    /// Revokes all tokens for a user by updating security stamp
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>New security stamp</returns>
    Task<string> RevokeAllUserTokensAsync(int userId);
}

/// <summary>
/// JWT token generation result
/// </summary>
public class JwtTokenResult
{
    public string Token { get; set; } = string.Empty;
    public string JwtId { get; set; } = string.Empty;
    public DateTime Expiry { get; set; }
    public DateTime IssuedAt { get; set; }
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public List<Claim> Claims { get; set; } = new();
}

/// <summary>
/// JWT validation result
/// </summary>
public class JwtValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public ClaimsPrincipal? Principal { get; set; }
    public string? JwtId { get; set; }
    public int? UserId { get; set; }
    public string? UserEmail { get; set; }
    public DateTime? Expiry { get; set; }
    public DateTime? IssuedAt { get; set; }
    public string? SecurityStamp { get; set; }
    public bool IsExpired { get; set; }
    public bool IsNearExpiry { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}

