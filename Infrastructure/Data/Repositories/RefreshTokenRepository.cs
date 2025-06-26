using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for RefreshToken entity operations
/// </summary>
public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    public async Task<RefreshToken?> GetByJwtIdAsync(string jwtId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.JwtId == jwtId, cancellationToken);
    }

    public async Task<List<RefreshToken>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rt => rt.UserId == userId)
            .OrderByDescending(rt => rt.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RefreshToken>> GetValidTokensByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(rt => rt.UserId == userId &&
                        rt.IsActive &&
                        !rt.IsUsed &&
                        !rt.IsRevoked &&
                        rt.ExpiryDate > now)
            .OrderByDescending(rt => rt.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RefreshToken>> GetExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(rt => rt.ExpiryDate <= now && rt.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RefreshToken>> GetRevokedTokensAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rt => rt.IsRevoked)
            .OrderByDescending(rt => rt.RevokedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RefreshToken>> GetUsedTokensAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rt => rt.IsUsed)
            .OrderByDescending(rt => rt.ModifiedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RefreshToken>> GetByDeviceFingerprintAsync(string deviceFingerprint, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rt => rt.DeviceFingerprint == deviceFingerprint)
            .OrderByDescending(rt => rt.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RefreshToken>> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rt => rt.CreatedByIp == ipAddress)
            .OrderByDescending(rt => rt.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RefreshToken>> GetTokensCreatedInPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rt => rt.CreatedOn >= startDate && rt.CreatedOn <= endDate)
            .OrderByDescending(rt => rt.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RefreshToken>> GetTokensExpiringWithinAsync(DateTime expiryThreshold, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rt => rt.ExpiryDate <= expiryThreshold && rt.IsActive && !rt.IsUsed && !rt.IsRevoked)
            .OrderBy(rt => rt.ExpiryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> RevokeAllTokensForUserAsync(int userId, string reason, string? revokedByIp = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(rt => rt.UserId == userId && rt.IsActive && !rt.IsRevoked)
            .ExecuteUpdateAsync(rt => rt
                .SetProperty(x => x.IsRevoked, true)
                .SetProperty(x => x.RevokedDate, now)
                .SetProperty(x => x.RevocationReason, reason)
                .SetProperty(x => x.RevokedByIp, revokedByIp)
                .SetProperty(x => x.IsActive, false)
                .SetProperty(x => x.ModifiedOn, now), cancellationToken);
    }

    public async Task<int> RevokeTokensByDeviceAsync(string deviceFingerprint, string reason, string? revokedByIp = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(rt => rt.DeviceFingerprint == deviceFingerprint && rt.IsActive && !rt.IsRevoked)
            .ExecuteUpdateAsync(rt => rt
                .SetProperty(x => x.IsRevoked, true)
                .SetProperty(x => x.RevokedDate, now)
                .SetProperty(x => x.RevocationReason, reason)
                .SetProperty(x => x.RevokedByIp, revokedByIp)
                .SetProperty(x => x.IsActive, false)
                .SetProperty(x => x.ModifiedOn, now), cancellationToken);
    }

    public async Task<int> RevokeExpiredTokensAsync(string reason = "Token expired", CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(rt => rt.ExpiryDate <= now && rt.IsActive && !rt.IsRevoked)
            .ExecuteUpdateAsync(rt => rt
                .SetProperty(x => x.IsRevoked, true)
                .SetProperty(x => x.RevokedDate, now)
                .SetProperty(x => x.RevocationReason, reason)
                .SetProperty(x => x.IsActive, false)
                .SetProperty(x => x.ModifiedOn, now), cancellationToken);
    }

    public async Task<int> DeleteOldTokensAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rt => rt.CreatedOn < cutoffDate && (rt.IsRevoked || rt.ExpiryDate < DateTime.UtcNow))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<bool> MarkTokenAsUsedAsync(int tokenId, string? usedByIp = null, CancellationToken cancellationToken = default)
    {
        var affectedRows = await _dbSet
            .Where(rt => rt.Id == tokenId && !rt.IsUsed)
            .ExecuteUpdateAsync(rt => rt
                .SetProperty(x => x.IsUsed, true)
                .SetProperty(x => x.ModifiedOn, DateTime.UtcNow), cancellationToken);

        return affectedRows > 0;
    }

    public async Task<RefreshTokenValidationResult> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var refreshToken = await GetByTokenAsync(token, cancellationToken);

        var result = new RefreshTokenValidationResult
        {
            Token = refreshToken
        };

        if (refreshToken == null)
        {
            result.IsValid = false;
            result.ErrorMessage = "Invalid token";
            result.ValidationErrors.Add("Token not found");
            return result;
        }

        // Check if token is active
        if (!refreshToken.IsActive)
        {
            result.IsValid = false;
            result.ErrorMessage = "Token is inactive";
            result.ValidationErrors.Add("Token has been deactivated");
        }

        // Check if token is expired
        if (refreshToken.IsExpired())
        {
            result.IsValid = false;
            result.IsExpired = true;
            result.ErrorMessage = "Token has expired";
            result.ValidationErrors.Add("Token expiry date has passed");
        }
        else
        {
            result.TimeUntilExpiry = refreshToken.GetRemainingTime();
        }

        // Check if token has been used
        if (refreshToken.IsUsed)
        {
            result.IsValid = false;
            result.IsUsed = true;
            result.ErrorMessage = "Token has already been used";
            result.ValidationErrors.Add("Token has been consumed");
        }

        // Check if token has been revoked
        if (refreshToken.IsRevoked)
        {
            result.IsValid = false;
            result.IsRevoked = true;
            result.ErrorMessage = "Token has been revoked";
            result.ValidationErrors.Add($"Token was revoked: {refreshToken.RevocationReason}");
        }

        result.IsValid = !result.ValidationErrors.Any();
        return result;
    }

    public async Task<RefreshTokenStatistics> GetTokenStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        var tokens = await _dbSet.ToListAsync(cancellationToken);

        var statistics = new RefreshTokenStatistics
        {
            TotalTokens = tokens.Count,
            ActiveTokens = tokens.Count(t => t.IsActive && !t.IsUsed && !t.IsRevoked && t.ExpiryDate > now),
            ExpiredTokens = tokens.Count(t => t.ExpiryDate <= now),
            RevokedTokens = tokens.Count(t => t.IsRevoked),
            UsedTokens = tokens.Count(t => t.IsUsed),
            TokensCreatedToday = tokens.Count(t => t.CreatedOn.Date == today),
            TokensExpiredToday = tokens.Count(t => t.ExpiryDate.Date == today && t.ExpiryDate <= now)
        };

        // Group by user agent
        statistics.TokensByUserAgent = tokens
            .Where(t => !string.IsNullOrEmpty(t.UserAgent))
            .GroupBy(t => t.UserAgent!)
            .ToDictionary(g => g.Key, g => g.Count());

        // Group by IP address
        statistics.TokensByIpAddress = tokens
            .Where(t => !string.IsNullOrEmpty(t.CreatedByIp))
            .GroupBy(t => t.CreatedByIp!)
            .ToDictionary(g => g.Key, g => g.Count());

        // Group by day (last 30 days)
        var last30Days = Enumerable.Range(0, 30)
            .Select(i => today.AddDays(-i))
            .ToList();

        statistics.TokensByDay = last30Days
            .ToDictionary(
                date => date.ToString("yyyy-MM-dd"),
                date => tokens.Count(t => t.CreatedOn.Date == date));

        // Calculate average token lifetime
        var expiredTokens = tokens.Where(t => t.ExpiryDate <= now).ToList();
        if (expiredTokens.Any())
        {
            statistics.AverageTokenLifetime = expiredTokens
                .Average(t => (t.ExpiryDate - t.CreatedOn).TotalHours);
        }

        return statistics;
    }

    public async Task<TokenUsageAnalytics> GetTokenUsageAnalyticsAsync(int? userId = null, int days = 30, CancellationToken cancellationToken = default)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        var query = _dbSet.Include(rt => rt.User).AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(rt => rt.UserId == userId.Value);
        }

        var tokens = await query
            .Where(rt => rt.CreatedOn >= startDate)
            .ToListAsync(cancellationToken);

        var user = userId.HasValue ? await _context.Set<User>().FindAsync(userId.Value) : null;

        var analytics = new TokenUsageAnalytics
        {
            UserId = userId ?? 0,
            UserName = user?.FullName ?? "All Users",
            TotalTokensCreated = tokens.Count,
            TokensUsed = tokens.Count(t => t.IsUsed),
            TokensRevoked = tokens.Count(t => t.IsRevoked),
            TokensExpired = tokens.Count(t => t.ExpiryDate <= DateTime.UtcNow),
            UniqueSessions = tokens.Select(t => t.JwtId).Distinct().Count(),
            UniqueDevices = tokens.Where(t => !string.IsNullOrEmpty(t.DeviceFingerprint))
                                 .Select(t => t.DeviceFingerprint).Distinct().Count(),
            IpAddresses = tokens.Where(t => !string.IsNullOrEmpty(t.CreatedByIp))
                               .Select(t => t.CreatedByIp!).Distinct().ToList(),
            UserAgents = tokens.Where(t => !string.IsNullOrEmpty(t.UserAgent))
                              .Select(t => t.UserAgent!).Distinct().ToList(),
            LastTokenCreated = tokens.Any() ? tokens.Max(t => t.CreatedOn) : null
        };

        // Activity by hour
        analytics.ActivityByHour = tokens
            .GroupBy(t => t.CreatedOn.Hour.ToString("D2"))
            .ToDictionary(g => g.Key, g => g.Count());

        return analytics;
    }

    public async Task<List<SuspiciousTokenActivity>> GetSuspiciousActivityAsync(int hours = 24, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow.AddHours(-hours);
        var suspiciousActivities = new List<SuspiciousTokenActivity>();

        // Multiple tokens from same IP in short time
        var ipGroups = await _dbSet
            .Where(rt => rt.CreatedOn >= startTime && !string.IsNullOrEmpty(rt.CreatedByIp))
            .GroupBy(rt => rt.CreatedByIp)
            .Where(g => g.Count() > 10)
            .Select(g => new { IpAddress = g.Key, Count = g.Count(), Tokens = g.ToList() })
            .ToListAsync(cancellationToken);

        foreach (var group in ipGroups)
        {
            var userIds = group.Tokens.Select(t => t.UserId).Distinct();
            foreach (var userId in userIds)
            {
                var user = await _context.Set<User>().FindAsync(userId);
                suspiciousActivities.Add(new SuspiciousTokenActivity
                {
                    UserId = userId,
                    UserName = user?.FullName ?? "Unknown",
                    ActivityType = "High Frequency Token Creation",
                    Description = $"{group.Count} tokens created from IP {group.IpAddress} in {hours} hours",
                    IpAddress = group.IpAddress,
                    Timestamp = group.Tokens.Max(t => t.CreatedOn),
                    SeverityLevel = group.Count > 20 ? 5 : 3,
                    Metadata = new Dictionary<string, object> { ["TokenCount"] = group.Count }
                });
            }
        }

        // Tokens from multiple devices for same user
        var userDeviceGroups = await _dbSet
            .Where(rt => rt.CreatedOn >= startTime && !string.IsNullOrEmpty(rt.DeviceFingerprint))
            .GroupBy(rt => rt.UserId)
            .Where(g => g.Select(rt => rt.DeviceFingerprint).Distinct().Count() > 5)
            .ToListAsync(cancellationToken);

        foreach (var group in userDeviceGroups)
        {
            var user = await _context.Set<User>().FindAsync(group.Key);
            var deviceCount = group.Select(rt => rt.DeviceFingerprint).Distinct().Count();

            suspiciousActivities.Add(new SuspiciousTokenActivity
            {
                UserId = group.Key,
                UserName = user?.FullName ?? "Unknown",
                ActivityType = "Multiple Device Access",
                Description = $"Tokens created from {deviceCount} different devices in {hours} hours",
                Timestamp = group.Max(t => t.CreatedOn),
                SeverityLevel = deviceCount > 10 ? 4 : 2,
                Metadata = new Dictionary<string, object> { ["DeviceCount"] = deviceCount }
            });
        }

        return suspiciousActivities.OrderByDescending(sa => sa.SeverityLevel).ThenByDescending(sa => sa.Timestamp).ToList();
    }

    public async Task<TokenChainInfo> GetTokenChainAsync(int tokenId, CancellationToken cancellationToken = default)
    {
        var token = await _dbSet.FindAsync(tokenId);
        if (token == null)
        {
            return new TokenChainInfo { TokenId = tokenId };
        }

        var chain = new List<RefreshToken>();
        var current = token;

        // Go backwards to find the root
        while (current?.ReplacesToken != null)
        {
            current = await _dbSet
                .Include(rt => rt.ReplacesToken)
                .FirstOrDefaultAsync(rt => rt.Id == current.ReplacesToken.Id, cancellationToken);
        }

        // Now go forward to build the complete chain
        if (current != null)
        {
            chain.Add(current);

            while (current?.ReplacedByToken != null)
            {
                current = await _dbSet
                    .Include(rt => rt.ReplacedByToken)
                    .FirstOrDefaultAsync(rt => rt.Id == current.ReplacedByToken.Id, cancellationToken);

                if (current != null)
                {
                    chain.Add(current);
                }
            }
        }

        var chainInfo = new TokenChainInfo
        {
            TokenId = tokenId,
            TokenChain = chain,
            ChainLength = chain.Count,
            ChainStarted = chain.Any() ? chain.First().CreatedOn : DateTime.MinValue,
            IsActiveChain = chain.Any(t => t.IsActive && !t.IsRevoked && !t.IsUsed && t.ExpiryDate > DateTime.UtcNow)
        };

        if (!chainInfo.IsActiveChain && chain.Any())
        {
            var lastToken = chain.Last();
            chainInfo.ChainEnded = lastToken.ModifiedOn ?? lastToken.CreatedOn;
            chainInfo.TerminationReason = lastToken.IsRevoked ? lastToken.RevocationReason :
                                         lastToken.IsUsed ? "Token used" :
                                         lastToken.ExpiryDate <= DateTime.UtcNow ? "Token expired" : "Unknown";
        }

        return chainInfo;
    }

    public async Task<List<UserSession>> GetConcurrentSessionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var activeSessions = await _dbSet
            .Where(rt => rt.UserId == userId &&
                        rt.IsActive &&
                        !rt.IsUsed &&
                        !rt.IsRevoked &&
                        rt.ExpiryDate > now)
            .Select(rt => new UserSession
            {
                TokenId = rt.Id,
                DeviceFingerprint = rt.DeviceFingerprint,
                IpAddress = rt.CreatedByIp,
                UserAgent = rt.UserAgent,
                CreatedOn = rt.CreatedOn,
                ExpiryDate = rt.ExpiryDate,
                IsActive = true,
                DeviceType = ExtractDeviceType(rt.UserAgent),
                Location = "Unknown" // Would need GeoIP service
            })
            .ToListAsync(cancellationToken);

        return activeSessions;
    }

    public async Task<int> LimitConcurrentSessionsAsync(int userId, int maxSessions, string reason, CancellationToken cancellationToken = default)
    {
        var sessions = await GetConcurrentSessionsAsync(userId, cancellationToken);

        if (sessions.Count <= maxSessions)
        {
            return 0;
        }

        // Keep the most recent sessions, revoke the oldest
        var sessionsToRevoke = sessions
            .OrderByDescending(s => s.CreatedOn)
            .Skip(maxSessions)
            .Select(s => s.TokenId)
            .ToList();

        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(rt => sessionsToRevoke.Contains(rt.Id))
            .ExecuteUpdateAsync(rt => rt
                .SetProperty(x => x.IsRevoked, true)
                .SetProperty(x => x.RevokedDate, now)
                .SetProperty(x => x.RevocationReason, reason)
                .SetProperty(x => x.IsActive, false)
                .SetProperty(x => x.ModifiedOn, now), cancellationToken);
    }

    public async Task<TokenCleanupResult> PerformCleanupAsync(int retentionDays = 30, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new TokenCleanupResult
        {
            CleanupTimestamp = startTime
        };

        try
        {
            // Revoke expired tokens
            result.ExpiredTokensRevoked = await RevokeExpiredTokensAsync("Automatic cleanup - expired", cancellationToken);

            // Delete old tokens
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            result.OldTokensDeleted = await DeleteOldTokensAsync(cutoffDate, cancellationToken);

            // Remove orphaned tokens (user deleted)
            result.OrphanedTokensRemoved = await _dbSet
                .Where(rt => rt.User == null)
                .ExecuteDeleteAsync(cancellationToken);

            // Remove invalid tokens (malformed data)
            result.InvalidTokensRemoved = await _dbSet
                .Where(rt => string.IsNullOrEmpty(rt.Token) || string.IsNullOrEmpty(rt.JwtId))
                .ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Cleanup error: {ex.Message}");
        }

        result.CleanupDuration = DateTime.UtcNow - startTime;
        return result;
    }

    private static string ExtractDeviceType(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown";

        userAgent = userAgent.ToLowerInvariant();

        if (userAgent.Contains("mobile") || userAgent.Contains("android") || userAgent.Contains("iphone"))
            return "Mobile";

        if (userAgent.Contains("tablet") || userAgent.Contains("ipad"))
            return "Tablet";

        if (userAgent.Contains("windows") || userAgent.Contains("mac") || userAgent.Contains("linux"))
            return "Desktop";

        return "Unknown";
    }

    public void Delete(RefreshToken token, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(token);
    }
}