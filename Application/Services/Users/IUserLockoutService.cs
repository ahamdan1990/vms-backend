using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Application.Services.Users;

/// <summary>
/// User lockout service interface for managing account lockouts and failed login attempts
/// </summary>
public interface IUserLockoutService
{
    /// <summary>
    /// Records a failed login attempt for user
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <param name="failureReason">Reason for failure</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lockout result</returns>
    Task<LockoutResult> RecordFailedLoginAttemptAsync(string email, string? ipAddress = null,
        string? userAgent = null, string? failureReason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records successful login event only (no database changes)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="userAgent">User agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task RecordSuccessfulLoginEventAsync(int userId, string? ipAddress = null, string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records successful login without calling SaveChanges (for transaction coordination)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="userAgent">User agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task RecordSuccessfulLoginWithoutSaveAsync(int userId, string? ipAddress = null, string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a successful login and resets failed attempts
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task RecordSuccessfulLoginAsync(int userId, string? ipAddress = null, string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if user account is currently locked out
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lockout status</returns>
    Task<LockoutStatus> GetLockoutStatusAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually locks user account
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="reason">Lockout reason</param>
    /// <param name="duration">Lockout duration (null for indefinite)</param>
    /// <param name="lockedBy">ID of user performing the lockout</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lockout result</returns>
    Task<LockoutResult> LockUserAccountAsync(int userId, string reason, TimeSpan? duration = null,
        int? lockedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually unlocks user account
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="reason">Unlock reason</param>
    /// <param name="unlockedBy">ID of user performing the unlock</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if unlocked successfully</returns>
    Task<bool> UnlockUserAccountAsync(int userId, string reason, int unlockedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed login attempts for user within time period
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="timeWindow">Time window to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of failed attempts</returns>
    Task<int> GetFailedLoginAttemptsAsync(string email, TimeSpan timeWindow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed login attempts by IP address
    /// </summary>
    /// <param name="ipAddress">IP address</param>
    /// <param name="timeWindow">Time window to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of failed attempts from IP</returns>
    Task<int> GetFailedLoginAttemptsByIpAsync(string ipAddress, TimeSpan timeWindow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if IP address is rate limited
    /// </summary>
    /// <param name="ipAddress">IP address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rate limit status</returns>
    Task<RateLimitStatus> CheckIpRateLimitAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Temporarily blocks IP address
    /// </summary>
    /// <param name="ipAddress">IP address to block</param>
    /// <param name="reason">Block reason</param>
    /// <param name="duration">Block duration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if blocked successfully</returns>
    Task<bool> BlockIpAddressAsync(string ipAddress, string reason, TimeSpan duration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unblocks IP address
    /// </summary>
    /// <param name="ipAddress">IP address to unblock</param>
    /// <param name="reason">Unblock reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if unblocked successfully</returns>
    Task<bool> UnblockIpAddressAsync(string ipAddress, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets lockout configuration
    /// </summary>
    /// <returns>Lockout configuration</returns>
    LockoutConfiguration GetLockoutConfiguration();

    /// <summary>
    /// Updates lockout configuration
    /// </summary>
    /// <param name="configuration">New configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task UpdateLockoutConfigurationAsync(LockoutConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets security events for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="days">Number of days to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of security events</returns>
    Task<List<SecurityEvent>> GetUserSecurityEventsAsync(int userId, int days = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets system-wide security events
    /// </summary>
    /// <param name="hours">Hours to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of security events</returns>
    Task<List<SecurityEvent>> GetSystemSecurityEventsAsync(int hours = 24, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes login patterns for anomalies
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="currentAttempt">Current login attempt details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Anomaly detection result</returns>
    Task<AnomalyDetectionResult> AnalyzeLoginPatternAsync(int userId, LoginAttempt currentAttempt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets locked users that require attention
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of locked users</returns>
    Task<List<LockedUserInfo>> GetLockedUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users close to lockout threshold
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users at risk</returns>
    Task<List<UserAtRiskInfo>> GetUsersAtRiskAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates lockout report
    /// </summary>
    /// <param name="startDate">Report start date</param>
    /// <param name="endDate">Report end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lockout report</returns>
    Task<LockoutReport> GenerateLockoutReportAsync(DateTime startDate, DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs automated lockout cleanup
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cleanup result</returns>
    Task<LockoutCleanupResult> PerformAutomatedCleanupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends lockout notifications
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="lockoutResult">Lockout result</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SendLockoutNotificationAsync(int userId, LockoutResult lockoutResult, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates lockout bypass attempt
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="bypassCode">Bypass code</param>
    /// <param name="requestedBy">User requesting bypass</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bypass validation result</returns>
    Task<BypassValidationResult> ValidateLockoutBypassAsync(int userId, string bypassCode, int requestedBy,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Lockout result
/// </summary>
public class LockoutResult
{
    public bool IsLockedOut { get; set; }
    public bool WasAlreadyLocked { get; set; }
    public int FailedAttempts { get; set; }
    public int MaxFailedAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public TimeSpan? LockoutDuration { get; set; }
    public string? Reason { get; set; }
    public bool RequiresAdminIntervention { get; set; }
    public string? SecurityEventId { get; set; }
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Lockout status
/// </summary>
public class LockoutStatus
{
    public bool IsLockedOut { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public TimeSpan? TimeRemaining { get; set; }
    public int FailedAttempts { get; set; }
    public int MaxFailedAttempts { get; set; }
    public int RemainingAttempts { get; set; }
    public string? LockoutReason { get; set; }
    public DateTime? LastFailedAttempt { get; set; }
    public bool CanRetryNow { get; set; }
    public DateTime? NextRetryTime { get; set; }
}

/// <summary>
/// Rate limit status
/// </summary>
public class RateLimitStatus
{
    public bool IsRateLimited { get; set; }
    public int CurrentAttempts { get; set; }
    public int MaxAttempts { get; set; }
    public TimeSpan WindowDuration { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime? NextAllowedTime { get; set; }
    public TimeSpan? RetryAfter { get; set; }
}

/// <summary>
/// Lockout configuration
/// </summary>
public class LockoutConfiguration
{
    public int MaxFailedAttempts { get; set; } = 5;
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);
    public bool EnableProgressiveLockout { get; set; } = true;
    public List<TimeSpan> LockoutProgression { get; set; } = new()
    {
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(24)
    };
    public TimeSpan FailedAttemptWindow { get; set; } = TimeSpan.FromMinutes(15);
    public bool ResetAttemptsOnSuccess { get; set; } = true;
    public bool EnableIpBlocking { get; set; } = true;
    public int MaxFailedAttemptsPerIp { get; set; } = 10;
    public TimeSpan IpBlockDuration { get; set; } = TimeSpan.FromMinutes(30);
    public bool NotifyOnLockout { get; set; } = true;
    public bool NotifyAdminOnLockout { get; set; } = true;
    public bool EnableAnomalyDetection { get; set; } = true;
    public List<string> TrustedIpRanges { get; set; } = new();
    public List<string> BlockedIpRanges { get; set; } = new();
}

/// <summary>
/// Security event
/// </summary>
public class SecurityEvent
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string? Action { get; set; }
    public string? Result { get; set; }
    public bool RequiresInvestigation { get; set; }
}

/// <summary>
/// Login attempt details
/// </summary>
public class LoginAttempt
{
    public string Email { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Location { get; set; }
    public string? DeviceFingerprint { get; set; }
    public bool IsSuccess { get; set; }
    public string? FailureReason { get; set; }
}

/// <summary>
/// Anomaly detection result
/// </summary>
public class AnomalyDetectionResult
{
    public bool IsAnomalous { get; set; }
    public double AnomalyScore { get; set; }
    public List<string> AnomalyTypes { get; set; } = new();
    public string? RecommendedAction { get; set; }
    public bool RequiresAdditionalVerification { get; set; }
    public List<string> SuspiciousFactors { get; set; } = new();
}

/// <summary>
/// Locked user information
/// </summary>
public class LockedUserInfo
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime LockedAt { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public string LockoutReason { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public TimeSpan? TimeRemaining { get; set; }
    public bool RequiresAdminAction { get; set; }
    public string? LastAttemptIp { get; set; }
}

/// <summary>
/// User at risk information
/// </summary>
public class UserAtRiskInfo
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public int MaxFailedAttempts { get; set; }
    public int RemainingAttempts { get; set; }
    public DateTime? LastFailedAttempt { get; set; }
    public string? LastAttemptIp { get; set; }
    public string? RiskLevel { get; set; }
}

/// <summary>
/// Lockout report
/// </summary>
public class LockoutReport
{
    public DateTime ReportPeriodStart { get; set; }
    public DateTime ReportPeriodEnd { get; set; }
    public int TotalLockouts { get; set; }
    public int UniqueLockoutUsers { get; set; }
    public int AutomaticLockouts { get; set; }
    public int ManualLockouts { get; set; }
    public int ResolvedLockouts { get; set; }
    public int PendingLockouts { get; set; }
    public Dictionary<string, int> LockoutsByDay { get; set; } = new();
    public Dictionary<string, int> LockoutsByHour { get; set; } = new();
    public Dictionary<string, int> LockoutsByReason { get; set; } = new();
    public List<string> TopLockedUsers { get; set; } = new();
    public List<string> TopSourceIps { get; set; } = new();
    public TimeSpan AverageLockoutDuration { get; set; }
}

/// <summary>
/// Lockout cleanup result
/// </summary>
public class LockoutCleanupResult
{
    public int ExpiredLockoutsCleared { get; set; }
    public int OldSecurityEventsArchived { get; set; }
    public int BlockedIpsCleared { get; set; }
    public int FailedAttemptsReset { get; set; }
    public TimeSpan CleanupDuration { get; set; }
    public DateTime CleanupTimestamp { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool WasSuccessful => !Errors.Any();
}

/// <summary>
/// Bypass validation result
/// </summary>
public class BypassValidationResult
{
    public bool IsValid { get; set; }
    public bool BypassGranted { get; set; }
    public string? Reason { get; set; }
    public DateTime? BypassExpiresAt { get; set; }
    public string? BypassCode { get; set; }
    public int? AuthorizedBy { get; set; }
    public bool RequiresSecondApproval { get; set; }
    public List<string> Conditions { get; set; } = new();
}