using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for RefreshToken entity operations
/// </summary>
public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
{
    /// <summary>
    /// Gets a refresh token by token value
    /// </summary>
    /// <param name="token">Token value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Refresh token if found, null otherwise</returns>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a refresh token by JWT ID
    /// </summary>
    /// <param name="jwtId">JWT ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Refresh token if found, null otherwise</returns>
    Task<RefreshToken?> GetByJwtIdAsync(string jwtId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all refresh tokens for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of refresh tokens for the user</returns>
    Task<List<RefreshToken>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets valid refresh tokens for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of valid refresh tokens for the user</returns>
    Task<List<RefreshToken>> GetValidTokensByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expired refresh tokens
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of expired refresh tokens</returns>
    Task<List<RefreshToken>> GetExpiredTokensAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets revoked refresh tokens
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of revoked refresh tokens</returns>
    Task<List<RefreshToken>> GetRevokedTokensAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets used refresh tokens
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of used refresh tokens</returns>
    Task<List<RefreshToken>> GetUsedTokensAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets refresh tokens by device fingerprint
    /// </summary>
    /// <param name="deviceFingerprint">Device fingerprint</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of refresh tokens for the device</returns>
    Task<List<RefreshToken>> GetByDeviceFingerprintAsync(string deviceFingerprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets refresh tokens by IP address
    /// </summary>
    /// <param name="ipAddress">IP address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of refresh tokens from the IP address</returns>
    Task<List<RefreshToken>> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets refresh tokens created within a date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of refresh tokens created in the date range</returns>
    Task<List<RefreshToken>> GetTokensCreatedInPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets refresh tokens expiring within a certain period
    /// </summary>
    /// <param name="expiryThreshold">Expiry threshold date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of refresh tokens expiring soon</returns>
    Task<List<RefreshToken>> GetTokensExpiringWithinAsync(DateTime expiryThreshold, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="reason">Revocation reason</param>
    /// <param name="revokedByIp">IP address performing revocation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of tokens revoked</returns>
    Task<int> RevokeAllTokensForUserAsync(int userId, string reason, string? revokedByIp = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes refresh tokens by device fingerprint
    /// </summary>
    /// <param name="deviceFingerprint">Device fingerprint</param>
    /// <param name="reason">Revocation reason</param>
    /// <param name="revokedByIp">IP address performing revocation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of tokens revoked</returns>
    Task<int> RevokeTokensByDeviceAsync(string deviceFingerprint, string reason, string? revokedByIp = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes expired refresh tokens
    /// </summary>
    /// <param name="reason">Revocation reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of tokens revoked</returns>
    Task<int> RevokeExpiredTokensAsync(string reason = "Token expired", CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old refresh tokens
    /// </summary>
    /// <param name="cutoffDate">Cutoff date for deletion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of tokens deleted</returns>
    Task<int> DeleteOldTokensAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a refresh token as used
    /// </summary>
    /// <param name="tokenId">Token ID</param>
    /// <param name="usedByIp">IP address where token was used</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if token was successfully marked as used</returns>
    Task<bool> MarkTokenAsUsedAsync(int tokenId, string? usedByIp = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a refresh token
    /// </summary>
    /// <param name="token">Token value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with token details</returns>
    Task<RefreshTokenValidationResult> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets refresh token statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token statistics</returns>
    Task<RefreshTokenStatistics> GetTokenStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets token usage analytics
    /// </summary>
    /// <param name="userId">Optional user ID filter</param>
    /// <param name="days">Number of days to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token usage analytics</returns>
    Task<TokenUsageAnalytics> GetTokenUsageAnalyticsAsync(int? userId = null, int days = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets suspicious token activity
    /// </summary>
    /// <param name="hours">Number of hours to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of suspicious activities</returns>
    Task<List<SuspiciousTokenActivity>> GetSuspiciousActivityAsync(int hours = 24, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a token is part of a token chain (rotation)
    /// </summary>
    /// <param name="tokenId">Token ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token chain information</returns>
    Task<TokenChainInfo> GetTokenChainAsync(int tokenId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets concurrent sessions for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of concurrent sessions</returns>
    Task<List<UserSession>> GetConcurrentSessionsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Limits concurrent sessions for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="maxSessions">Maximum allowed sessions</param>
    /// <param name="reason">Revocation reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of sessions revoked</returns>
    Task<int> LimitConcurrentSessionsAsync(int userId, int maxSessions, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs token cleanup maintenance
    /// </summary>
    /// <param name="retentionDays">Number of days to retain old tokens</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cleanup result</returns>
    Task<TokenCleanupResult> PerformCleanupAsync(int retentionDays = 30, CancellationToken cancellationToken = default);

    void Delete(RefreshToken token, CancellationToken cancellationToken = default);
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
    public TimeSpan? TimeUntilExpiry { get; set; }
}

/// <summary>
/// Refresh token statistics
/// </summary>
public class RefreshTokenStatistics
{
    public int TotalTokens { get; set; }
    public int ActiveTokens { get; set; }
    public int ExpiredTokens { get; set; }
    public int RevokedTokens { get; set; }
    public int UsedTokens { get; set; }
    public int TokensCreatedToday { get; set; }
    public int TokensExpiredToday { get; set; }
    public Dictionary<string, int> TokensByUserAgent { get; set; } = new();
    public Dictionary<string, int> TokensByIpAddress { get; set; } = new();
    public Dictionary<string, int> TokensByDay { get; set; } = new();
    public double AverageTokenLifetime { get; set; }
}

/// <summary>
/// Token usage analytics
/// </summary>
public class TokenUsageAnalytics
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int TotalTokensCreated { get; set; }
    public int TokensUsed { get; set; }
    public int TokensRevoked { get; set; }
    public int TokensExpired { get; set; }
    public int UniqueSessions { get; set; }
    public int UniqueDevices { get; set; }
    public List<string> IpAddresses { get; set; } = new();
    public List<string> UserAgents { get; set; } = new();
    public Dictionary<string, int> ActivityByHour { get; set; } = new();
    public DateTime? LastTokenCreated { get; set; }
    public DateTime? LastTokenUsed { get; set; }
}

/// <summary>
/// Suspicious token activity
/// </summary>
public class SuspiciousTokenActivity
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
    public int SeverityLevel { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Token chain information
/// </summary>
public class TokenChainInfo
{
    public int TokenId { get; set; }
    public List<RefreshToken> TokenChain { get; set; } = new();
    public int ChainLength { get; set; }
    public DateTime ChainStarted { get; set; }
    public DateTime? ChainEnded { get; set; }
    public bool IsActiveChain { get; set; }
    public string? TerminationReason { get; set; }
}

/// <summary>
/// User session information
/// </summary>
public class UserSession
{
    public int TokenId { get; set; }
    public string? DeviceFingerprint { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime? LastUsed { get; set; }
    public bool IsActive { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

/// <summary>
/// Token cleanup result
/// </summary>
public class TokenCleanupResult
{
    public int ExpiredTokensRevoked { get; set; }
    public int OldTokensDeleted { get; set; }
    public int OrphanedTokensRemoved { get; set; }
    public int InvalidTokensRemoved { get; set; }
    public TimeSpan CleanupDuration { get; set; }
    public DateTime CleanupTimestamp { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool WasSuccessful => !Errors.Any();
}