namespace VisitorManagementSystem.Api.Configuration;

/// <summary>
/// JWT configuration settings
/// </summary>
public class JwtConfiguration
{
    public const string SectionName = "JWT";

    /// <summary>
    /// Secret key for signing JWT tokens (must be at least 256 bits)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiry time in minutes
    /// </summary>
    public int ExpiryInMinutes { get; set; } = 15;

    /// <summary>
    /// Refresh token expiry time in days
    /// </summary>
    public int RefreshTokenExpiryInDays { get; set; } = 7;

    /// <summary>
    /// Algorithm used for signing tokens
    /// </summary>
    public string Algorithm { get; set; } = "HS256";

    /// <summary>
    /// Whether to validate the issuer signing key
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;

    /// <summary>
    /// Whether to validate the issuer
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Whether to validate the audience
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Whether to validate the token lifetime
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Whether to require expiration time
    /// </summary>
    public bool RequireExpirationTime { get; set; } = true;

    /// <summary>
    /// Clock skew tolerance in minutes
    /// </summary>
    public int ClockSkewMinutes { get; set; } = 0;

    /// <summary>
    /// Password reset token expiry in minutes
    /// </summary>
    public int PasswordResetTokenExpiryMinutes { get; set; } = 30;

    /// <summary>
    /// Email confirmation token expiry in hours
    /// </summary>
    public int EmailConfirmationTokenExpiryHours { get; set; } = 24;

    /// <summary>
    /// Two-factor authentication token expiry in minutes
    /// </summary>
    public int TwoFactorTokenExpiryMinutes { get; set; } = 5;

    /// <summary>
    /// Whether to allow multiple concurrent sessions per user
    /// </summary>
    public bool AllowConcurrentSessions { get; set; } = true;

    /// <summary>
    /// Maximum number of concurrent sessions per user
    /// </summary>
    public int MaxConcurrentSessions { get; set; } = 5;

    /// <summary>
    /// Whether to rotate refresh tokens on usage
    /// </summary>
    public bool RotateRefreshTokens { get; set; } = true;

    /// <summary>
    /// Whether to revoke refresh token families on suspicious activity
    /// </summary>
    public bool RevokeFamilyOnSuspiciousActivity { get; set; } = true;
}