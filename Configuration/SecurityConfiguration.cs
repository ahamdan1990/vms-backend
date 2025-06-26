namespace VisitorManagementSystem.Api.Configuration;

/// <summary>
/// Security configuration settings
/// </summary>
public class SecurityConfiguration
{
    public const string SectionName = "Security";

    /// <summary>
    /// Password policy settings
    /// </summary>
    public PasswordPolicyConfiguration PasswordPolicy { get; set; } = new();

    /// <summary>
    /// Account lockout settings
    /// </summary>
    public LockoutConfiguration Lockout { get; set; } = new();

    /// <summary>
    /// Rate limiting settings
    /// </summary>
    public RateLimitingConfiguration RateLimiting { get; set; } = new();

    /// <summary>
    /// Encryption keys
    /// </summary>
    public EncryptionKeysConfiguration EncryptionKeys { get; set; } = new();

    /// <summary>
    /// Session security settings
    /// </summary>
    public SessionSecurityConfiguration SessionSecurity { get; set; } = new();

    /// <summary>
    /// CORS settings
    /// </summary>
    public CorsConfiguration Cors { get; set; } = new();

    /// <summary>
    /// HTTPS settings
    /// </summary>
    public HttpsConfiguration Https { get; set; } = new();

    /// <summary>
    /// Content Security Policy settings
    /// </summary>
    public ContentSecurityPolicyConfiguration ContentSecurityPolicy { get; set; } = new();
    public int PasswordResetTokenExpiryMinutes { get; set; } = 15;
}

/// <summary>
/// Password policy configuration
/// </summary>
public class PasswordPolicyConfiguration
{
    public bool RequireDigit { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; } = true;
    public int RequiredLength { get; set; } = 8;
    public int RequiredUniqueChars { get; set; } = 3;
    public int MaxLength { get; set; } = 128;
    public int PasswordHistoryLimit { get; set; } = 5;
    public int PasswordExpiryDays { get; set; } = 90;
    public bool PreventPasswordReuse { get; set; } = true;
    public List<string> CommonPasswords { get; set; } = new();
    public List<string> ForbiddenPasswords { get; set; } = new();
}

/// <summary>
/// Account lockout configuration
/// </summary>
public class LockoutConfiguration
{
    public TimeSpan DefaultLockoutTimeSpan { get; set; } = TimeSpan.FromMinutes(15);
    public int MaxFailedAccessAttempts { get; set; } = 5;
    public bool AllowedForNewUsers { get; set; } = true;
    public TimeSpan LockoutWindow { get; set; } = TimeSpan.FromMinutes(5);
    public bool AutoUnlockAfterLockoutPeriod { get; set; } = true;
    public bool NotifyOnLockout { get; set; } = true;
    public int MaxLockoutAttempts { get; set; } = 10;
    public TimeSpan ExtendedLockoutDuration { get; set; } = TimeSpan.FromHours(24);
}

/// <summary>
/// Rate limiting configuration
/// </summary>
public class RateLimitingConfiguration
{
    public ApiRateLimit LoginAttempts { get; set; } = new() { PermitLimit = 5, Window = TimeSpan.FromMinutes(5) };
    public ApiRateLimit GeneralApi { get; set; } = new() { PermitLimit = 100, Window = TimeSpan.FromMinutes(1) };
    public ApiRateLimit PasswordReset { get; set; } = new() { PermitLimit = 3, Window = TimeSpan.FromHours(1) };
    public ApiRateLimit TokenRefresh { get; set; } = new() { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) };
    public bool EnableGlobalRateLimit { get; set; } = true;
    public bool EnablePerUserRateLimit { get; set; } = true;
    public bool EnablePerIpRateLimit { get; set; } = true;
}

/// <summary>
/// API rate limit settings
/// </summary>
public class ApiRateLimit
{
    public int PermitLimit { get; set; }
    public TimeSpan Window { get; set; }
    public int QueueLimit { get; set; } = 0;
    public bool AutoReplenishment { get; set; } = true;
    public TimeSpan ReplenishmentPeriod { get; set; } = TimeSpan.FromSeconds(1);
}

/// <summary>
/// Encryption keys configuration
/// </summary>
public class EncryptionKeysConfiguration
{
    public string DataProtectionKey { get; set; } = string.Empty;
    public string DatabaseEncryptionKey { get; set; } = string.Empty;
    public string CookieEncryptionKey { get; set; } = string.Empty;
    public string FileEncryptionKey { get; set; } = string.Empty;
    public bool AutoRotateKeys { get; set; } = true;
    public int KeyRotationDays { get; set; } = 90;
}

/// <summary>
/// Session security configuration
/// </summary>
public class SessionSecurityConfiguration
{
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromMinutes(30);
    public bool SlidingExpiration { get; set; } = true;
    public bool RequireSecureCookies { get; set; } = true;
    public bool RequireHttpOnlyCookies { get; set; } = true;
    public SameSiteMode SameSiteMode { get; set; } = SameSiteMode.Strict;
    public string CookieDomain { get; set; } = string.Empty;
    public string CookiePath { get; set; } = "/";
    public bool EnableDeviceTracking { get; set; } = true;
    public bool EnableGeoLocationTracking { get; set; } = false;
}

/// <summary>
/// CORS configuration
/// </summary>
public class CorsConfiguration
{
    public List<string> AllowedOrigins { get; set; } = new();
    public List<string> AllowedMethods { get; set; } = new() { "GET", "POST", "PUT", "DELETE" };
    public List<string> AllowedHeaders { get; set; } = new() { "Content-Type", "Authorization" };
    public bool AllowCredentials { get; set; } = true;
    public int PreflightMaxAge { get; set; } = 86400;
}

/// <summary>
/// HTTPS configuration
/// </summary>
public class HttpsConfiguration
{
    public bool RequireHttps { get; set; } = true;
    public bool RedirectHttpToHttps { get; set; } = true;
    public int HttpsPort { get; set; } = 443;
    public bool EnableHsts { get; set; } = true;
    public TimeSpan HstsMaxAge { get; set; } = TimeSpan.FromDays(365);
    public bool HstsIncludeSubdomains { get; set; } = true;
    public bool HstsPreload { get; set; } = true;
}

/// <summary>
/// Content Security Policy configuration
/// </summary>
public class ContentSecurityPolicyConfiguration
{
    public bool Enabled { get; set; } = true;
    public bool ReportOnly { get; set; } = false;
    public string DefaultSrc { get; set; } = "'self'";
    public string ScriptSrc { get; set; } = "'self'";
    public string StyleSrc { get; set; } = "'self' 'unsafe-inline'";
    public string ImgSrc { get; set; } = "'self' data: https:";
    public string FontSrc { get; set; } = "'self'";
    public string ConnectSrc { get; set; } = "'self'";
    public string FrameSrc { get; set; } = "'none'";
    public string ObjectSrc { get; set; } = "'none'";
    public string ReportUri { get; set; } = string.Empty;
}