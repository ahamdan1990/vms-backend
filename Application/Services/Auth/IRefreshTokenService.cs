using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Services.Auth;

/// <summary>
/// Refresh token service interface for managing refresh token lifecycle
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Creates a new refresh token for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="jwtId">Associated JWT ID</param>
    /// <param name="deviceFingerprint">Device fingerprint</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New refresh token</returns>
    Task<RefreshToken> CreateRefreshTokenAsync(int userId, string jwtId, string? deviceFingerprint = null,
        string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and retrieves refresh token
    /// </summary>
    /// <param name="token">Token value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Refresh token validation result</returns>
    Task<RefreshTokenValidationResult> ValidateRefreshTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uses refresh token (marks as used and creates replacement)
    /// </summary>
    /// <param name="token">Token value</param>
    /// <param name="newJwtId">New JWT ID for replacement</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <param name="deviceFingerprint">Device fingerprint</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token usage result with new token</returns>
    Task<RefreshTokenUsageResult> UseRefreshTokenAsync(string token, string newJwtId, string? ipAddress = null,
        string? userAgent = null, string? deviceFingerprint = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific refresh token
    /// </summary>
    /// <param name="token">Token value</param>
    /// <param name="reason">Revocation reason</param>
    /// <param name="revokedByIp">IP address performing revocation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if token was revoked</returns>
    Task<bool> RevokeRefreshTokenAsync(string token, string reason, string? revokedByIp = null,
        CancellationToken cancellationToken = default);
    Task<bool> RevokeRefreshTokenAsync(int sessionID, string reason, string? revokedByIp = null,
CancellationToken cancellationToken = default);
    /// <summary>
    /// Revokes all refresh tokens for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="reason">Revocation reason</param>
    /// <param name="revokedByIp">IP address performing revocation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of tokens revoked</returns>
    Task<int> RevokeAllUserTokensAsync(int userId, string reason, string? revokedByIp = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes tokens by device fingerprint
    /// </summary>
    /// <param name="deviceFingerprint">Device fingerprint</param>
    /// <param name="reason">Revocation reason</param>
    /// <param name="revokedByIp">IP address performing revocation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of tokens revoked</returns>
    Task<int> RevokeTokensByDeviceAsync(string deviceFingerprint, string reason, string? revokedByIp = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active refresh tokens for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active refresh tokens</returns>
    Task<List<RefreshTokenInfo>> GetActiveTokensAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets refresh token by JWT ID
    /// </summary>
    /// <param name="jwtId">JWT ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Refresh token if found</returns>
    Task<RefreshToken?> GetTokenByJwtIdAsync(string jwtId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if token is valid and not expired
    /// </summary>
    /// <param name="token">Token value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if token is valid</returns>
    Task<bool> IsTokenValidAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets token expiration time
    /// </summary>
    /// <param name="token">Token value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Expiration time or null if token not found</returns>
    Task<DateTime?> GetTokenExpirationAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extends token expiration
    /// </summary>
    /// <param name="token">Token value</param>
    /// <param name="extensionPeriod">Extension period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if extended successfully</returns>
    Task<bool> ExtendTokenExpirationAsync(string token, TimeSpan extensionPeriod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs token cleanup (removes expired tokens)
    /// </summary>
    /// <param name="retentionPeriod">How long to keep expired tokens</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cleanup result</returns>
    Task<TokenCleanupResult> CleanupExpiredTokensAsync(TimeSpan? retentionPeriod = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets token usage statistics
    /// </summary>
    /// <param name="userId">Optional user ID filter</param>
    /// <param name="days">Number of days to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token usage statistics</returns>
    Task<TokenUsageStatistics> GetTokenUsageStatisticsAsync(int? userId = null, int days = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects suspicious token activity
    /// </summary>
    /// <param name="hours">Hours to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of suspicious activities</returns>
    Task<List<SuspiciousTokenActivity>> DetectSuspiciousActivityAsync(int hours = 24, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets concurrent sessions for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of concurrent sessions</returns>
    Task<List<UserSessionInfo>> GetConcurrentSessionsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enforces concurrent session limits
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="maxSessions">Maximum allowed sessions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of sessions terminated</returns>
    Task<int> EnforceSessionLimitsAsync(int userId, int maxSessions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates device fingerprint consistency
    /// </summary>
    /// <param name="token">Token value</param>
    /// <param name="currentDeviceFingerprint">Current device fingerprint</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Device validation result</returns>
    Task<DeviceValidationResult> ValidateDeviceConsistencyAsync(string token, string currentDeviceFingerprint,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets token family/chain information
    /// </summary>
    /// <param name="token">Token value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token family information</returns>
    Task<TokenFamilyInfo> GetTokenFamilyAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates refresh token (security best practice)
    /// </summary>
    /// <param name="oldToken">Current token</param>
    /// <param name="newJwtId">New JWT ID</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <param name="deviceFingerprint">Device fingerprint</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token rotation result</returns>
    Task<RefreshTokenUsageResult> RotateTokenAsync(string oldToken, string newJwtId, string? ipAddress = null,
        string? userAgent = null, string? deviceFingerprint = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates secure refresh token value
    /// </summary>
    /// <returns>Secure token value</returns>
    string GenerateSecureTokenValue();

    /// <summary>
    /// Validates token format and structure
    /// </summary>
    /// <param name="token">Token value</param>
    /// <returns>True if token format is valid</returns>
    bool IsValidTokenFormat(string token);
}

/// <summary>
/// Refresh token validation result
/// </summary>
public class RefreshTokenValidationResult
{
    public bool IsValid { get; set; }
    public RefreshToken? Token { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public bool IsExpired { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsUsed { get; set; }
    public bool IsFromSameDevice { get; set; }
    public TimeSpan? TimeUntilExpiry { get; set; }
    public User? User { get; set; }
}

/// <summary>
/// Refresh token usage result
/// </summary>
public class RefreshTokenUsageResult
{
    public bool IsSuccess { get; set; }
    public RefreshToken? NewToken { get; set; }
    public RefreshToken? UsedToken { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool RequiresDeviceVerification { get; set; }
    public bool SuspiciousActivity { get; set; }
}

/// <summary>
/// Refresh token information for display
/// </summary>
public class RefreshTokenInfo
{
    public string TokenId { get; set; } = string.Empty;
    public string? DeviceFingerprint { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime? LastUsed { get; set; }
    public bool IsActive { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
}

/// <summary>
/// Token usage statistics
/// </summary>
public class TokenUsageStatistics
{
    public int TotalTokensCreated { get; set; }
    public int ActiveTokens { get; set; }
    public int ExpiredTokens { get; set; }
    public int RevokedTokens { get; set; }
    public int UsedTokens { get; set; }
    public int UniqueUsers { get; set; }
    public int UniqueDevices { get; set; }
    public Dictionary<string, int> TokensByDay { get; set; } = new();
    public Dictionary<string, int> TokensByHour { get; set; } = new();
    public Dictionary<string, int> TokensByUserAgent { get; set; } = new();
    public Dictionary<string, int> TokensByLocation { get; set; } = new();
    public double AverageSessionDuration { get; set; }
    public DateTime AnalysisPeriodStart { get; set; }
    public DateTime AnalysisPeriodEnd { get; set; }
}

/// <summary>
/// User session information
/// </summary>
public class UserSessionInfo
{
    public string SessionId { get; set; } = string.Empty;
    public string? DeviceFingerprint { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime? LastActivity { get; set; }
    public DateTime ExpiryTime { get; set; }
    public bool IsActive { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsCurrent { get; set; }
    public int ActivityCount { get; set; }
    public DateTime CreatedOn { get; internal set; }
    public DateTime ExpiryDate { get; internal set; }
    public DateTime? LastUsed { get; internal set; }
}

/// <summary>
/// Device validation result
/// </summary>
public class DeviceValidationResult
{
    public bool IsValid { get; set; }
    public bool IsKnownDevice { get; set; }
    public bool RequiresVerification { get; set; }
    public string? ThreatLevel { get; set; }
    public List<string> SecurityFlags { get; set; } = new();
    public string? RecommendedAction { get; set; }
    public DateTime LastSeenOn { get; set; }
    public int TrustScore { get; set; }
}

/// <summary>
/// Token family/chain information
/// </summary>
public class TokenFamilyInfo
{
    public string FamilyId { get; set; } = string.Empty;
    public List<RefreshTokenInfo> TokenChain { get; set; } = new();
    public int ChainLength { get; set; }
    public DateTime ChainStarted { get; set; }
    public DateTime? ChainEnded { get; set; }
    public bool IsActiveChain { get; set; }
    public string? TerminationReason { get; set; }
    public RefreshTokenInfo? CurrentToken { get; set; }
    public RefreshTokenInfo? RootToken { get; set; }
}

/// <summary>
/// Token rotation result
/// </summary>
public class TokenRotationResult
{
    public bool IsSuccess { get; set; }
    public RefreshToken? NewToken { get; set; }
    public RefreshToken? OldToken { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool SecurityEventTriggered { get; set; }
    public string? SecurityEventReason { get; set; }
}