using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;

namespace VisitorManagementSystem.Api.Application.Services.Auth;

/// <summary>
/// User lockout service implementation for managing account lockouts and failed login attempts
/// </summary>
public class UserLockoutService : IUserLockoutService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserLockoutService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IOptions<LockoutConfiguration> _lockoutConfig;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);

    public UserLockoutService(
        IUnitOfWork unitOfWork,
        ILogger<UserLockoutService> logger,
        IMemoryCache cache,
        IOptions<LockoutConfiguration> lockoutConfig)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cache = cache;
        _lockoutConfig = lockoutConfig;
    }

    public async Task<LockoutResult> RecordFailedLoginAttemptAsync(string email, string? ipAddress = null,
        string? userAgent = null, string? failureReason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Failed login attempt for non-existent user: {Email} from IP: {IpAddress}", email, ipAddress);

                // Still record IP-based attempt for rate limiting
                await RecordIpFailedAttemptAsync(ipAddress, cancellationToken);

                return new LockoutResult
                {
                    IsLockedOut = false,
                    FailedAttempts = 0,
                    MaxFailedAttempts = _lockoutConfig.Value.MaxFailedAttempts
                };
            }

            // Record the security event
            await RecordSecurityEventAsync(user.Id, "FailedLogin", ipAddress, userAgent, failureReason, cancellationToken);

            // Check if user is already locked out
            if (user.IsCurrentlyLockedOut())
            {
                _logger.LogWarning("Login attempt for already locked user: {Email}", email);
                return new LockoutResult
                {
                    IsLockedOut = true,
                    WasAlreadyLocked = true,
                    FailedAttempts = user.FailedLoginAttempts,
                    MaxFailedAttempts = _lockoutConfig.Value.MaxFailedAttempts,
                    LockoutEnd = user.LockoutEnd,
                    LockoutDuration = user.LockoutEnd - DateTime.UtcNow,
                    Reason = "Account locked due to failed login attempts"
                };
            }

            // Increment failed attempts
            var config = _lockoutConfig.Value;
            user.IncrementFailedLoginAttempts(config.MaxFailedAttempts, GetLockoutDuration(user.FailedLoginAttempts + 1));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var result = new LockoutResult
            {
                IsLockedOut = user.IsCurrentlyLockedOut(),
                FailedAttempts = user.FailedLoginAttempts,
                MaxFailedAttempts = config.MaxFailedAttempts,
                LockoutEnd = user.LockoutEnd,
                LockoutDuration = user.LockoutEnd - DateTime.UtcNow,
                Reason = user.IsCurrentlyLockedOut() ? "Account locked due to failed login attempts" : null
            };

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked: {Email} after {Attempts} failed attempts", email, user.FailedLoginAttempts);
                await SendLockoutNotificationAsync(user.Id, result, cancellationToken);
            }

            // Also record IP-based attempt
            await RecordIpFailedAttemptAsync(ipAddress, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording failed login attempt for email: {Email}", email);
            return new LockoutResult
            {
                IsLockedOut = false,
                FailedAttempts = 0,
                MaxFailedAttempts = _lockoutConfig.Value.MaxFailedAttempts
            };
        }
    }

    /// <summary>
    /// Records successful login event only (no database changes)
    /// </summary>
    public async Task RecordSuccessfulLoginEventAsync(int userId, string? ipAddress = null, string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Just record the security event - no database entity changes
            _logger.LogInformation("Security event recorded: SuccessfulLogin for user: {UserId}", userId);

            // Clear any cached lockout data
            _cache.Remove($"lockout_status_{userId}");
            _cache.Remove($"failed_attempts_{userId}");

            // If you have a user email, also clear those caches
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
                if (user != null)
                {
                    _cache.Remove($"lockout_status_{user.Email}");
                    _cache.Remove($"failed_attempts_{user.Email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not clear user cache for user: {UserId}", userId);
            }

            // In production, you could save to a separate SecurityEvents table here
            // But avoid using the same DbContext as the main login operation
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording successful login event for user: {UserId}", userId);
            // Don't rethrow - this shouldn't fail the login process
        }
    }

    /// <summary>
    /// Records successful login without calling SaveChanges (for transaction coordination)
    /// </summary>
    public async Task RecordSuccessfulLoginWithoutSaveAsync(int userId, string? ipAddress = null, string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Record the security event without SaveChanges
            await RecordSecurityEventAsync(userId, "SuccessfulLogin", ipAddress, userAgent, null, cancellationToken);

            // Clear cached lockout data
            _cache.Remove($"lockout_status_{userId}");
            _cache.Remove($"failed_attempts_{userId}");

            _logger.LogDebug("Successful login recorded for user: {UserId} (without save)", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording successful login for user: {UserId}", userId);
        }
    }

    // Update the original method to be more explicit
    public async Task RecordSuccessfulLoginAsync(int userId, string? ipAddress = null, string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Successful login recorded for non-existent user: {UserId}", userId);
                return;
            }

            // Only update user if not already updated elsewhere
            if (user.FailedLoginAttempts > 0 || user.LockoutEnd.HasValue)
            {
                user.ResetFailedLoginAttempts();
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Record the security event
            await RecordSecurityEventAsync(userId, "SuccessfulLogin", ipAddress, userAgent, null, cancellationToken);

            // Clear cached lockout data
            _cache.Remove($"lockout_status_{user.Email}");
            _cache.Remove($"failed_attempts_{user.Email}");

            _logger.LogDebug("Successful login recorded for user: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording successful login for user: {UserId}", userId);
        }
    }

    public async Task<LockoutStatus> GetLockoutStatusAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"lockout_status_{email}";

            if (_cache.TryGetValue(cacheKey, out LockoutStatus? cachedStatus))
            {
                return cachedStatus ?? new LockoutStatus();
            }

            var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
            if (user == null)
            {
                return new LockoutStatus
                {
                    IsLockedOut = false,
                    FailedAttempts = 0,
                    MaxFailedAttempts = _lockoutConfig.Value.MaxFailedAttempts,
                    RemainingAttempts = _lockoutConfig.Value.MaxFailedAttempts,
                    CanRetryNow = true
                };
            }

            var status = new LockoutStatus
            {
                IsLockedOut = user.IsCurrentlyLockedOut(),
                LockoutEnd = user.LockoutEnd,
                TimeRemaining = user.IsCurrentlyLockedOut() ? user.LockoutEnd - DateTime.UtcNow : null,
                FailedAttempts = user.FailedLoginAttempts,
                MaxFailedAttempts = _lockoutConfig.Value.MaxFailedAttempts,
                RemainingAttempts = Math.Max(0, _lockoutConfig.Value.MaxFailedAttempts - user.FailedLoginAttempts),
                LockoutReason = user.IsCurrentlyLockedOut() ? "Exceeded maximum failed login attempts" : null,
                CanRetryNow = !user.IsCurrentlyLockedOut(),
                NextRetryTime = user.LockoutEnd
            };

            _cache.Set(cacheKey, status, _cacheExpiry);
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lockout status for email: {Email}", email);
            return new LockoutStatus();
        }
    }

    public async Task<LockoutResult> LockUserAccountAsync(int userId, string reason, TimeSpan? duration = null,
        int? lockedBy = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Attempted to lock non-existent user: {UserId}", userId);
                return new LockoutResult
                {
                    IsLockedOut = false,
                    Reason = "User not found"
                };
            }

            var lockoutDuration = duration ?? _lockoutConfig.Value.LockoutDuration;
            user.LockAccount(lockoutDuration);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Record security event
            await RecordSecurityEventAsync(userId, "ManualLockout", null, null, reason, cancellationToken);

            var result = new LockoutResult
            {
                IsLockedOut = true,
                WasAlreadyLocked = false,
                FailedAttempts = user.FailedLoginAttempts,
                MaxFailedAttempts = _lockoutConfig.Value.MaxFailedAttempts,
                LockoutEnd = user.LockoutEnd,
                LockoutDuration = lockoutDuration,
                Reason = reason,
                RequiresAdminIntervention = true
            };

            _logger.LogWarning("User account manually locked: {UserId} by {LockedBy} for reason: {Reason}",
                userId, lockedBy, reason);

            // Clear cache
            _cache.Remove($"lockout_status_{user.Email}");

            await SendLockoutNotificationAsync(userId, result, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking user account: {UserId}", userId);
            return new LockoutResult
            {
                IsLockedOut = false,
                Reason = "Lock operation failed"
            };
        }
    }

    public async Task<bool> UnlockUserAccountAsync(int userId, string reason, int unlockedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Attempted to unlock non-existent user: {UserId}", userId);
                return false;
            }

            user.UnlockAccount();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Record security event
            await RecordSecurityEventAsync(userId, "ManualUnlock", null, null, reason, cancellationToken);

            _logger.LogInformation("User account unlocked: {UserId} by {UnlockedBy} for reason: {Reason}",
                userId, unlockedBy, reason);

            // Clear cache
            _cache.Remove($"lockout_status_{user.Email}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking user account: {UserId}", userId);
            return false;
        }
    }

    public async Task<int> GetFailedLoginAttemptsAsync(string email, TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"failed_attempts_{email}_{timeWindow.TotalMinutes}";

            if (_cache.TryGetValue(cacheKey, out int cachedAttempts))
            {
                return cachedAttempts;
            }

            var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
            if (user == null)
            {
                return 0;
            }

            // For simplification, returning current failed attempts
            // In a production system, you'd query a security events table
            var attempts = user.FailedLoginAttempts;

            _cache.Set(cacheKey, attempts, TimeSpan.FromMinutes(5));
            return attempts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting failed login attempts for email: {Email}", email);
            return 0;
        }
    }

    public async Task<int> GetFailedLoginAttemptsByIpAsync(string ipAddress, TimeSpan timeWindow,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"ip_failed_attempts_{ipAddress}_{timeWindow.TotalMinutes}";

            if (_cache.TryGetValue(cacheKey, out int cachedAttempts))
            {
                return cachedAttempts;
            }

            // In a production system, this would query a dedicated table for IP-based attempts
            var attempts = 0; // Placeholder

            _cache.Set(cacheKey, attempts, TimeSpan.FromMinutes(5));
            return attempts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting failed login attempts for IP: {IpAddress}", ipAddress);
            return 0;
        }
    }

    public async Task<RateLimitStatus> CheckIpRateLimitAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = _lockoutConfig.Value;
            var attempts = await GetFailedLoginAttemptsByIpAsync(ipAddress, config.FailedAttemptWindow, cancellationToken);

            return new RateLimitStatus
            {
                IsRateLimited = attempts >= config.MaxFailedAttemptsPerIp,
                CurrentAttempts = attempts,
                MaxAttempts = config.MaxFailedAttemptsPerIp,
                WindowDuration = config.FailedAttemptWindow,
                WindowStart = DateTime.UtcNow - config.FailedAttemptWindow,
                NextAllowedTime = attempts >= config.MaxFailedAttemptsPerIp ?
                    DateTime.UtcNow.Add(config.IpBlockDuration) : null,
                RetryAfter = attempts >= config.MaxFailedAttemptsPerIp ?
                    config.IpBlockDuration : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking IP rate limit for: {IpAddress}", ipAddress);
            return new RateLimitStatus();
        }
    }

    public async Task<bool> BlockIpAddressAsync(string ipAddress, string reason, TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"blocked_ip_{ipAddress}";
            var blockInfo = new
            {
                IpAddress = ipAddress,
                Reason = reason,
                BlockedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(duration)
            };

            _cache.Set(cacheKey, blockInfo, duration);

            _logger.LogWarning("IP address blocked: {IpAddress} for {Duration} due to: {Reason}",
                ipAddress, duration, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking IP address: {IpAddress}", ipAddress);
            return false;
        }
    }

    public async Task<bool> UnblockIpAddressAsync(string ipAddress, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"blocked_ip_{ipAddress}";
            _cache.Remove(cacheKey);

            _logger.LogInformation("IP address unblocked: {IpAddress} for reason: {Reason}", ipAddress, reason);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unblocking IP address: {IpAddress}", ipAddress);
            return false;
        }
    }

    public LockoutConfiguration GetLockoutConfiguration()
    {
        return _lockoutConfig.Value;
    }

    public async Task UpdateLockoutConfigurationAsync(LockoutConfiguration configuration, CancellationToken cancellationToken = default)
    {
        try
        {
            // In a production system, this would update the configuration in database/storage
            _logger.LogInformation("Lockout configuration updated");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lockout configuration");
        }
    }

    public async Task<List<SecurityEvent>> GetUserSecurityEventsAsync(int userId, int days = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // In a production system, this would query a dedicated security events table
            await Task.CompletedTask;
            return new List<SecurityEvent>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security events for user: {UserId}", userId);
            return new List<SecurityEvent>();
        }
    }

    public async Task<List<SecurityEvent>> GetSystemSecurityEventsAsync(int hours = 24, CancellationToken cancellationToken = default)
    {
        try
        {
            // In a production system, this would query a dedicated security events table
            await Task.CompletedTask;
            return new List<SecurityEvent>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system security events");
            return new List<SecurityEvent>();
        }
    }

    public async Task<AnomalyDetectionResult> AnalyzeLoginPatternAsync(int userId, LoginAttempt currentAttempt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return new AnomalyDetectionResult
                {
                    IsAnomalous = false,
                    AnomalyScore = 0
                };
            }

            // Basic anomaly detection
            var anomalies = new List<string>();
            var score = 0.0;

            // Check for unusual time
            var currentHour = currentAttempt.Timestamp.Hour;
            if (currentHour < 6 || currentHour > 22)
            {
                anomalies.Add("Unusual login time");
                score += 0.3;
            }

            // Check for new device/location (simplified)
            if (!string.IsNullOrEmpty(currentAttempt.DeviceFingerprint))
            {
                anomalies.Add("New device");
                score += 0.4;
            }

            return new AnomalyDetectionResult
            {
                IsAnomalous = score > 0.5,
                AnomalyScore = score,
                AnomalyTypes = anomalies,
                RequiresAdditionalVerification = score > 0.7,
                SuspiciousFactors = anomalies,
                RecommendedAction = score > 0.7 ? "Require additional verification" :
                                 score > 0.5 ? "Monitor closely" : "Allow"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing login pattern for user: {UserId}", userId);
            return new AnomalyDetectionResult
            {
                IsAnomalous = false,
                AnomalyScore = 0
            };
        }
    }

    public async Task<List<LockedUserInfo>> GetLockedUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var lockedUsers = await _unitOfWork.Users.GetLockedOutUsersAsync(cancellationToken);

            return lockedUsers.Select(user => new LockedUserInfo
            {
                UserId = user.Id,
                Email = user.Email.Value,
                FullName = user.FullName,
                LockedAt = user.LockoutEnd?.Subtract(_lockoutConfig.Value.LockoutDuration) ?? DateTime.UtcNow,
                LockoutEnd = user.LockoutEnd,
                LockoutReason = "Failed login attempts",
                FailedAttempts = user.FailedLoginAttempts,
                TimeRemaining = user.LockoutEnd - DateTime.UtcNow,
                RequiresAdminAction = true
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting locked users");
            return new List<LockedUserInfo>();
        }
    }

    public async Task<List<UserAtRiskInfo>> GetUsersAtRiskAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var usersWithFailedAttempts = await _unitOfWork.Users
                .GetUsersWithFailedLoginAttemptsAsync(1, cancellationToken);

            return usersWithFailedAttempts
                .Where(u => !u.IsCurrentlyLockedOut())
                .Select(user => new UserAtRiskInfo
                {
                    UserId = user.Id,
                    Email = user.Email.Value,
                    FullName = user.FullName,
                    FailedAttempts = user.FailedLoginAttempts,
                    MaxFailedAttempts = _lockoutConfig.Value.MaxFailedAttempts,
                    RemainingAttempts = _lockoutConfig.Value.MaxFailedAttempts - user.FailedLoginAttempts,
                    RiskLevel = user.FailedLoginAttempts >= _lockoutConfig.Value.MaxFailedAttempts - 1 ? "High" : "Medium"
                }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users at risk");
            return new List<UserAtRiskInfo>();
        }
    }

    public async Task<LockoutReport> GenerateLockoutReportAsync(DateTime startDate, DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lockedUsers = await GetLockedUsersAsync(cancellationToken);

            return new LockoutReport
            {
                ReportPeriodStart = startDate,
                ReportPeriodEnd = endDate,
                TotalLockouts = lockedUsers.Count,
                UniqueLockoutUsers = lockedUsers.Count,
                AutomaticLockouts = lockedUsers.Count(u => u.LockoutReason == "Failed login attempts"),
                ManualLockouts = 0,
                ResolvedLockouts = 0,
                PendingLockouts = lockedUsers.Count(u => u.LockoutEnd > DateTime.UtcNow),
                LockoutsByReason = lockedUsers.GroupBy(u => u.LockoutReason)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AverageLockoutDuration = _lockoutConfig.Value.LockoutDuration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating lockout report");
            return new LockoutReport
            {
                ReportPeriodStart = startDate,
                ReportPeriodEnd = endDate
            };
        }
    }

    public async Task<LockoutCleanupResult> PerformAutomatedCleanupAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = new LockoutCleanupResult
            {
                CleanupTimestamp = DateTime.UtcNow
            };

            var startTime = DateTime.UtcNow;

            // Unlock expired lockouts
            var expiredLockedUsers = await _unitOfWork.Users.GetLockedOutUsersAsync(cancellationToken);
            var expiredCount = 0;

            foreach (var user in expiredLockedUsers.Where(u => u.LockoutEnd <= DateTime.UtcNow))
            {
                user.UnlockAccount();
                expiredCount++;
            }

            if (expiredCount > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            result.ExpiredLockoutsCleared = expiredCount;
            result.CleanupDuration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Automated lockout cleanup completed. Cleared {Count} expired lockouts", expiredCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing automated cleanup");
            return new LockoutCleanupResult
            {
                CleanupTimestamp = DateTime.UtcNow,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task SendLockoutNotificationAsync(int userId, LockoutResult lockoutResult, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null) return;

            // In a production system, this would send email/SMS notifications
            _logger.LogInformation("Lockout notification sent to user: {UserId}", userId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending lockout notification for user: {UserId}", userId);
        }
    }

    public async Task<BypassValidationResult> ValidateLockoutBypassAsync(int userId, string bypassCode, int requestedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // In a production system, this would validate bypass codes
            await Task.CompletedTask;

            return new BypassValidationResult
            {
                IsValid = false,
                BypassGranted = false,
                Reason = "Bypass codes not implemented"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating lockout bypass for user: {UserId}", userId);
            return new BypassValidationResult
            {
                IsValid = false,
                BypassGranted = false,
                Reason = "Validation failed"
            };
        }
    }

    private TimeSpan GetLockoutDuration(int failedAttempts)
    {
        var config = _lockoutConfig.Value;

        if (config.EnableProgressiveLockout && config.LockoutProgression.Any())
        {
            var index = Math.Min(failedAttempts - config.MaxFailedAttempts, config.LockoutProgression.Count - 1);
            return index >= 0 ? config.LockoutProgression[index] : config.LockoutDuration;
        }

        return config.LockoutDuration;
    }

    private async Task RecordSecurityEventAsync(int? userId, string eventType, string? ipAddress,
        string? userAgent, string? details, CancellationToken cancellationToken)
    {
        try
        {
            // In a production system, this would save to a security events table
            _logger.LogInformation("Security event recorded: {EventType} for user: {UserId}", eventType, userId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording security event: {EventType}", eventType);
        }
    }

    private async Task RecordIpFailedAttemptAsync(string? ipAddress, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(ipAddress)) return;

            var cacheKey = $"ip_attempts_{ipAddress}";
            var attempts = _cache.Get<int>(cacheKey);
            _cache.Set(cacheKey, attempts + 1, _lockoutConfig.Value.FailedAttemptWindow);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording IP failed attempt for: {IpAddress}", ipAddress);
        }
    }
}