// Application/Services/Auth/RefreshTokenService.cs
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Infrastructure.Utilities;

namespace VisitorManagementSystem.Api.Application.Services.Auth;

/// <summary>
/// Refresh token service implementation for managing token lifecycle
/// </summary>
public class RefreshTokenService : IRefreshTokenService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefreshTokenService> _logger;
    private const int DefaultTokenLength = 64;
    private const int MaxConcurrentSessions = 5;

    public RefreshTokenService(
        IUnitOfWork unitOfWork,
        ILogger<RefreshTokenService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(int userId, string jwtId, string? deviceFingerprint = null,
        string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = GenerateSecureTokenValue();
            var expiryDate = DateTime.UtcNow.AddDays(7); // Default 7 days

            var refreshToken = new RefreshToken
            {
                Token = token,
                JwtId = jwtId,
                UserId = userId,
                ExpiryDate = expiryDate,
                CreatedByIp = ipAddress,
                UserAgent = userAgent,
                DeviceFingerprint = deviceFingerprint,
                IsUsed = false,
                IsRevoked = false,
                IsActive = true
            };

            await _unitOfWork.RefreshTokens.AddAsync(refreshToken, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Refresh token created for user: {UserId}, JTI: {JwtId}", userId, jwtId);

            // REMOVED: Cleanup old tokens to prevent concurrency issues during login
            // The TokenCleanupBackgroundService handles this every 6 hours
            // _ = Task.Run(async () => await CleanupOldTokensForUserAsync(userId, cancellationToken));

            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refresh token for user: {UserId}", userId);
            throw;
        }
    }
    public async Task<RefreshTokenValidationResult> ValidateRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var result = new RefreshTokenValidationResult();

        try
        {
            if (string.IsNullOrEmpty(token))
            {
                result.ErrorMessage = "Token is required";
                result.ValidationErrors.Add("Token cannot be null or empty");
                return result;
            }

            if (!IsValidTokenFormat(token))
            {
                result.ErrorMessage = "Invalid token format";
                result.ValidationErrors.Add("Token format is invalid");
                return result;
            }

            var refreshToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(token, cancellationToken);
            if (refreshToken == null)
            {
                result.ErrorMessage = "Token not found";
                result.ValidationErrors.Add("Refresh token does not exist");
                return result;
            }

            result.Token = refreshToken;

            // Check if token is used
            if (refreshToken.IsUsed)
            {
                result.IsUsed = true;
                result.ErrorMessage = "Token has already been used";
                result.ValidationErrors.Add("This refresh token has already been used");

                // Security concern: token reuse might indicate token theft
                await HandleSuspiciousActivityAsync(refreshToken, "Token reuse detected", cancellationToken);
                return result;
            }

            // Check if token is revoked
            if (refreshToken.IsRevoked)
            {
                result.IsRevoked = true;
                result.ErrorMessage = "Token has been revoked";
                result.ValidationErrors.Add($"Token was revoked: {refreshToken.RevocationReason}");
                return result;
            }

            // Check if token is expired
            if (refreshToken.IsExpired())
            {
                result.IsExpired = true;
                result.ErrorMessage = "Token has expired";
                result.ValidationErrors.Add("Refresh token has expired");
                return result;
            }

            // Check if token is active
            if (!refreshToken.IsActive)
            {
                result.ErrorMessage = "Token is inactive";
                result.ValidationErrors.Add("Refresh token is not active");
                return result;
            }

            // Get user information
            var user = await _unitOfWork.Users.GetByIdAsync(refreshToken.UserId, cancellationToken);
            if (user == null)
            {
                result.ErrorMessage = "User not found";
                result.ValidationErrors.Add("Associated user account not found");
                return result;
            }

            if (!user.IsValidForAuthentication())
            {
                result.ErrorMessage = "User account is not valid";
                result.ValidationErrors.Add("User account is not valid for authentication");
                return result;
            }

            result.User = user;
            result.TimeUntilExpiry = refreshToken.GetRemainingTime();
            result.IsValid = true;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token");
            result.ErrorMessage = "Token validation failed";
            result.ValidationErrors.Add("An error occurred during token validation");
            return result;
        }
    }

    public async Task<RefreshTokenUsageResult> UseRefreshTokenAsync(string token, string newJwtId, string? ipAddress = null,
        string? userAgent = null, string? deviceFingerprint = null, CancellationToken cancellationToken = default)
    {
        var result = new RefreshTokenUsageResult();

        try
        {
            var validation = await ValidateRefreshTokenAsync(token, cancellationToken);
            if (!validation.IsValid || validation.Token == null)
            {
                result.ErrorMessage = validation.ErrorMessage;
                result.Errors = validation.ValidationErrors;
                return result;
            }

            var oldToken = validation.Token;


            // Validate device consistency if fingerprint is provided

            if (!string.IsNullOrEmpty(deviceFingerprint) && !oldToken.IsFromSameDevice(deviceFingerprint))
            {
                result.RequiresDeviceVerification = true;
                result.SuspiciousActivity = true;

                await HandleSuspiciousActivityAsync(oldToken, "Device fingerprint mismatch", cancellationToken);

                result.ErrorMessage = "Device verification required";
                result.Errors.Add("Token is being used from a different device");
                return result;
            }


            // Create new refresh token
            var newToken = oldToken.CreateReplacementToken(
    GenerateSecureTokenValue(),
    newJwtId,
    DateTime.UtcNow.AddDays(7),
    ipAddress,
    userAgent,
    deviceFingerprint);

// Add new token and update old token
await _unitOfWork.RefreshTokens.AddAsync(newToken, cancellationToken);
_unitOfWork.RefreshTokens.Update(oldToken);
await _unitOfWork.SaveChangesAsync(cancellationToken);

result.IsSuccess = true;
result.NewToken = newToken;
result.UsedToken = oldToken;

_logger.LogDebug("Refresh token rotated for user: {UserId}, Old JTI: {OldJwtId}, New JTI: {NewJwtId}",
    oldToken.UserId, oldToken.JwtId, newJwtId);

return result;
}
catch (Exception ex)
{
_logger.LogError(ex, "Error using refresh token");
result.ErrorMessage = "Token usage failed";
result.Errors.Add("An error occurred while using the refresh token");
return result;
}
}

    public async Task<bool> RevokeRefreshTokenAsync(string token, string reason, string? revokedByIp = null,
    CancellationToken cancellationToken = default)
    {
    try
    {
    var refreshToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(token, cancellationToken);
    if (refreshToken == null)
    {
        _logger.LogWarning("Attempted to revoke non-existent token");
        return false;
    }

    refreshToken.Revoke(reason, revokedByIp);
    _unitOfWork.RefreshTokens.Update(refreshToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    _logger.LogInformation("Refresh token revoked for user: {UserId}, Reason: {Reason}",
        refreshToken.UserId, reason);

    return true;
    }
    catch (Exception ex)
    {
    _logger.LogError(ex, "Error revoking refresh token");
    return false;
    }
    }

    public async Task<bool> RevokeRefreshTokenAsync(int sessionID, string reason, string? revokedByIp = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var refreshToken = await _unitOfWork.RefreshTokens.GetByIdAsync(sessionID, cancellationToken);
            if (refreshToken == null)
            {
                _logger.LogWarning("Attempted to revoke non-existent token");
                return false;
            }

            // Revoke the refresh token
            refreshToken.Revoke(reason, revokedByIp);
            _unitOfWork.RefreshTokens.Update(refreshToken);

            // 🔐 Invalidate any associated access tokens by rotating the user's security stamp
            var user = await _unitOfWork.Users.GetByIdAsync(refreshToken.UserId, cancellationToken);
            if (user != null)
            {
                user.SecurityStamp = Guid.NewGuid().ToString();
                _unitOfWork.Users.Update(user);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Security stamp updated for user: {UserId}, NewStamp: {Stamp}", user.Id, user.SecurityStamp);

            _logger.LogInformation("Refresh token revoked for user: {UserId}, Reason: {Reason}",
                refreshToken.UserId, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token");
            return false;
        }
    }


    public async Task<int> RevokeAllUserTokensAsync(int userId, string reason, string? revokedByIp = null,
    CancellationToken cancellationToken = default)
    {
    try
    {
    var activeTokens = await _unitOfWork.RefreshTokens.GetValidTokensByUserIdAsync(userId, cancellationToken);

    foreach (var token in activeTokens)
    {
        token.Revoke(reason, revokedByIp);
        _unitOfWork.RefreshTokens.Update(token);
    }

    await _unitOfWork.SaveChangesAsync(cancellationToken);

    _logger.LogInformation("All refresh tokens revoked for user: {UserId}, Count: {Count}, Reason: {Reason}",
        userId, activeTokens.Count, reason);

    return activeTokens.Count;
    }
    catch (Exception ex)
    {
    _logger.LogError(ex, "Error revoking all tokens for user: {UserId}", userId);
    return 0;
    }
    }

    public async Task<int> RevokeTokensByDeviceAsync(string deviceFingerprint, string reason, string? revokedByIp = null,
    CancellationToken cancellationToken = default)
    {
    try
    {
    var tokens = await _unitOfWork.RefreshTokens.GetByDeviceFingerprintAsync(deviceFingerprint, cancellationToken);
    var activeTokens = tokens.Where(t => t.IsValid()).ToList();

    foreach (var token in activeTokens)
    {
        token.Revoke(reason, revokedByIp);
        _unitOfWork.RefreshTokens.Update(token);
    }

    await _unitOfWork.SaveChangesAsync(cancellationToken);

    _logger.LogInformation("Tokens revoked for device: {DeviceFingerprint}, Count: {Count}, Reason: {Reason}",
        deviceFingerprint, activeTokens.Count, reason);

    return activeTokens.Count;
    }
    catch (Exception ex)
    {
    _logger.LogError(ex, "Error revoking tokens for device: {DeviceFingerprint}", deviceFingerprint);
    return 0;
    }
    }

    public async Task<List<RefreshTokenInfo>> GetActiveTokensAsync(int userId, CancellationToken cancellationToken = default)
    {
    try
    {
    var tokens = await _unitOfWork.RefreshTokens.GetValidTokensByUserIdAsync(userId, cancellationToken);

    return tokens.Select(t => new RefreshTokenInfo
    {
        TokenId = t.Id.ToString(),
        DeviceFingerprint = t.DeviceFingerprint,
        IpAddress = t.CreatedByIp,
        UserAgent = t.UserAgent,
        CreatedOn = t.CreatedOn,
        ExpiryDate = t.ExpiryDate,
        LastUsed = t.ModifiedOn,
        IsActive = t.IsValid(),
        DeviceType = DetermineDeviceType(t.UserAgent),
        Location = DetermineLocation(t.CreatedByIp),
        IsCurrent = false // Would need additional context to determine current token
    }).ToList();
    }
    catch (Exception ex)
    {
    _logger.LogError(ex, "Error getting active tokens for user: {UserId}", userId);
    return new List<RefreshTokenInfo>();
    }
    }

    public async Task<RefreshToken?> GetTokenByJwtIdAsync(string jwtId, CancellationToken cancellationToken = default)
    {
    try
    {
    return await _unitOfWork.RefreshTokens.GetByJwtIdAsync(jwtId, cancellationToken);
    }
    catch (Exception ex)
    {
    _logger.LogError(ex, "Error getting token by JWT ID: {JwtId}", jwtId);
    return null;
    }
    }

    public async Task<bool> IsTokenValidAsync(string token, CancellationToken cancellationToken = default)
    {
    var validation = await ValidateRefreshTokenAsync(token, cancellationToken);
    return validation.IsValid;
    }

    public async Task<DateTime?> GetTokenExpirationAsync(string token, CancellationToken cancellationToken = default)
    {
    try
    {
    var refreshToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(token, cancellationToken);
    return refreshToken?.ExpiryDate;
    }
    catch (Exception ex)
    {
    _logger.LogError(ex, "Error getting token expiration");
    return null;
    }
    }

    public async Task<bool> ExtendTokenExpirationAsync(string token, TimeSpan extensionPeriod, CancellationToken cancellationToken = default)
    {
    try
    {
    var refreshToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(token, cancellationToken);
    if (refreshToken == null || !refreshToken.IsValid())
        return false;

    refreshToken.ExpiryDate = refreshToken.ExpiryDate.Add(extensionPeriod);
    _unitOfWork.RefreshTokens.Update(refreshToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    _logger.LogInformation("Token expiration extended for user: {UserId}, Extension: {Extension}",
        refreshToken.UserId, extensionPeriod);

    return true;
    }
    catch (Exception ex)
    {
    _logger.LogError(ex, "Error extending token expiration");
    return false;
    }
    }

    public async Task<TokenCleanupResult> CleanupExpiredTokensAsync(TimeSpan? retentionPeriod = null, CancellationToken cancellationToken = default)
    {
    var result = new TokenCleanupResult
    {
    CleanupTimestamp = DateTime.UtcNow
    };

    var startTime = DateTime.UtcNow;

    try
    {
    retentionPeriod ??= TimeSpan.FromDays(30); // Default 30 days retention
    var cutoffDate = DateTime.UtcNow.Subtract(retentionPeriod.Value);

    // Revoke expired tokens
    var expiredTokens = await _unitOfWork.RefreshTokens.GetExpiredTokensAsync(cancellationToken);
    var activeExpiredTokens = expiredTokens.Where(t => t.IsActive && !t.IsRevoked).ToList();

    foreach (var token in activeExpiredTokens)
    {
        token.Revoke("Expired during cleanup");
        _unitOfWork.RefreshTokens.Update(token);
    }

    result.ExpiredTokensRevoked = activeExpiredTokens.Count;

    // Delete old tokens beyond retention period
    var oldTokens = await _unitOfWork.RefreshTokens.GetTokensCreatedInPeriodAsync(DateTime.MinValue, cutoffDate, cancellationToken);
    var tokensToDelete = oldTokens.Where(t => t.IsRevoked || t.IsUsed || t.IsExpired()).ToList();

    foreach (var token in tokensToDelete)
    {
        _unitOfWork.RefreshTokens.Delete(token);
    }

    result.OldTokensDeleted = tokensToDelete.Count;

    await _unitOfWork.SaveChangesAsync(cancellationToken);

    result.CleanupDuration = DateTime.UtcNow - startTime;

    _logger.LogInformation("Token cleanup completed. Expired revoked: {ExpiredRevoked}, Old deleted: {OldDeleted}",
        result.ExpiredTokensRevoked, result.OldTokensDeleted);

    return result;
    }
    catch (Exception ex)
    {
    _logger.LogError(ex, "Error during token cleanup");
    result.Errors.Add($"Cleanup failed: {ex.Message}");
    result.CleanupDuration = DateTime.UtcNow - startTime;
    return result;
    }
    }

    public async Task<TokenUsageStatistics> GetTokenUsageStatisticsAsync(int? userId = null, int days = 30,
    CancellationToken cancellationToken = default)
    {
    try
    {
    var startDate = DateTime.UtcNow.AddDays(-days);
    var endDate = DateTime.UtcNow;

    var tokens = userId.HasValue
        ? await _unitOfWork.RefreshTokens.GetByUserIdAsync(userId.Value, cancellationToken)
        : await _unitOfWork.RefreshTokens.GetTokensCreatedInPeriodAsync(startDate, endDate, cancellationToken);

    var tokensInPeriod = tokens.Where(t => t.CreatedOn >= startDate).ToList();

    var statistics = new TokenUsageStatistics
    {
        TotalTokensCreated = tokensInPeriod.Count,
        ActiveTokens = tokensInPeriod.Count(t => t.IsValid()),
        ExpiredTokens = tokensInPeriod.Count(t => t.IsExpired()),
        RevokedTokens = tokensInPeriod.Count(t => t.IsRevoked),
        UsedTokens = tokensInPeriod.Count(t => t.IsUsed),
        UniqueUsers = tokensInPeriod.Select(t => t.UserId).Distinct().Count(),
        UniqueDevices = tokensInPeriod.Where(t => !string.IsNullOrEmpty(t.DeviceFingerprint))
            .Select(t => t.DeviceFingerprint).Distinct().Count(),
        AnalysisPeriodStart = startDate,
        AnalysisPeriodEnd = endDate
    };

    // Group by day
    statistics.TokensByDay = tokensInPeriod
        .GroupBy(t => t.CreatedOn.Date.ToString("yyyy-MM-dd"))
        .ToDictionary(g => g.Key, g => g.Count());

    // Group by hour
    statistics.TokensByHour = tokensInPeriod
        .GroupBy(t => t.CreatedOn.Hour.ToString("D2"))
        .ToDictionary(g => g.Key, g => g.Count());

    // Group by user agent
    statistics.TokensByUserAgent = tokensInPeriod
        .Where(t => !string.IsNullOrEmpty(t.UserAgent))
        .GroupBy(t => DetermineDeviceType(t.UserAgent))
        .ToDictionary(g => g.Key, g => g.Count());

    // Calculate average session duration
    var validTokens = tokensInPeriod.Where(t => t.ModifiedOn.HasValue).ToList();
    if (validTokens.Any())
    {
        var durations = validTokens.Select(t => (t.ModifiedOn!.Value - t.CreatedOn).TotalMinutes);
        statistics.AverageSessionDuration = durations.Average();
    }

    return statistics;
    }
    catch (Exception ex)
    {
    _logger.LogError(ex, "Error getting token usage statistics");
    return new TokenUsageStatistics();
    }
    }

    public async Task<List<SuspiciousTokenActivity>> DetectSuspiciousActivityAsync(int hours = 24, CancellationToken cancellationToken = default)
    {
    var activities = new List<SuspiciousTokenActivity>();
    var startTime = DateTime.UtcNow.AddHours(-hours);

    try
    {
    var recentTokens = await _unitOfWork.RefreshTokens.GetTokensCreatedInPeriodAsync(startTime, DateTime.UtcNow, cancellationToken);

    // Detect multiple device usage
    var multiDeviceUsers = recentTokens
        .Where(t => !string.IsNullOrEmpty(t.DeviceFingerprint))
        .GroupBy(t => t.UserId)
        .Where(g => g.Select(t => t.DeviceFingerprint).Distinct().Count() > 3)
        .ToList();

    foreach (var userGroup in multiDeviceUsers)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userGroup.Key, cancellationToken);
        activities.Add(new SuspiciousTokenActivity
        {
            UserId = userGroup.Key,
            UserName = user?.FullName ?? "Unknown",
            ActivityType = "MultipleDevices",
            Description = $"User accessed from {userGroup.Select(t => t.DeviceFingerprint).Distinct().Count()} different devices",
            Timestamp = DateTime.UtcNow,
            SeverityLevel = 2
        });
    }

    // Detect rapid token creation
    var rapidTokenUsers = recentTokens
        .GroupBy(t => t.UserId)
        .Where(g => g.Count() > 10) // More than 10 tokens in the time period
        .ToList();

    foreach (var userGroup in rapidTokenUsers)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userGroup.Key, cancellationToken);
        activities.Add(new SuspiciousTokenActivity
        {
            UserId = userGroup.Key,
            UserName = user?.FullName ?? "Unknown",
            ActivityType = "RapidTokenCreation",
            Description = $"Created {userGroup.Count()} tokens in {hours} hours",
            Timestamp = DateTime.UtcNow,
            SeverityLevel = 3
        });
    }

    return activities;
    }
    catch (Exception ex)
    {
    _logger.LogError(ex, "Error detecting suspicious token activity");
    return activities;
    }
    }

    public async Task<List<UserSessionInfo>> GetConcurrentSessionsAsync(int userId, CancellationToken cancellationToken = default)
    {
    try
    {
    var validTokens = await _unitOfWork.RefreshTokens.GetValidTokensByUserIdAsync(userId, cancellationToken);

    return validTokens.Select(t => new UserSessionInfo
    {
        SessionId = t.Id.ToString(),
        DeviceFingerprint = t.DeviceFingerprint,
        IpAddress = t.CreatedByIp,
        UserAgent = t.UserAgent,
        CreatedOn = t.CreatedOn,
        ExpiryDate = t.ExpiryDate,
        LastUsed = t.ModifiedOn,
        IsActive = t.IsValid(),
        DeviceType = DetermineDeviceType(t.UserAgent),
        Browser = DetermineBrowser(t.UserAgent),
        OperatingSystem = DetermineOperatingSystem(t.UserAgent),
        Location = DetermineLocation(t.CreatedByIp),
        IsCurrent = false // Would need additional context
    }).ToList();
    }
    catch (Exception ex)
    {
    _logger.LogError(ex, "Error getting concurrent sessions for user: {UserId}", userId);
    return new List<UserSessionInfo>();
    }
    }

    public async Task<int> EnforceSessionLimitsAsync(int userId, int maxSessions, CancellationToken cancellationToken = default)
    {
    try
    {
    var activeSessions = await GetConcurrentSessionsAsync(userId, cancellationToken);

    if (activeSessions.Count <= maxSessions)
        return 0;

    // Revoke oldest sessions
    var sessionsToRevoke = activeSessions
        .OrderBy(s => s.LastUsed ?? s.CreatedOn)
        .Take(activeSessions.Count - maxSessions)
        .ToList();

    int revokedCount = 0;
    foreach (var session in sessionsToRevoke)
    {
        if (await RevokeRefreshTokenAsync(session.SessionId, $"Session limit exceeded (max: {maxSessions})", cancellationToken: cancellationToken))
        {
            revokedCount++;
        }
    }

    _logger.LogInformation("Session limit enforced for user: {UserId}, Sessions revoked: {Count}",
        userId, revokedCount);

    return revokedCount;
    }
    catch (Exception ex)
    {
    _logger.LogError(ex, "Error enforcing session limits for user: {UserId}", userId);
    return 0;
    }
    }

    public async Task<DeviceValidationResult> ValidateDeviceConsistencyAsync(string token, string currentDeviceFingerprint,
    CancellationToken cancellationToken = default)
    {
    var result = new DeviceValidationResult();

    try
    {
    var refreshToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(token, cancellationToken);
    if (refreshToken == null)
    {
        result.RecommendedAction = "Token not found";
        return result;
    }

    result.IsKnownDevice = !string.IsNullOrEmpty(refreshToken.DeviceFingerprint);
    result.IsValid = refreshToken.IsFromSameDevice(currentDeviceFingerprint);

    if (!result.IsValid)
    {
        result.RequiresVerification = true;
        result.ThreatLevel = "Medium";
        result.SecurityFlags.Add("Device fingerprint mismatch");
        result.RecommendedAction = "Require additional verification";
    }
    else
    {
        result.ThreatLevel = "Low";
        result.RecommendedAction = "Allow access";
    }

    result.LastSeenOn = refreshToken.CreatedOn;
    result.TrustScore = CalculateTrustScore(refreshToken);

    return result;
    }
    catch (Exception ex)
    {
    _logger.LogError(ex, "Error validating device consistency");
    result.RecommendedAction = "Validation failed";
    return result;
    }
    }

    public async Task<TokenFamilyInfo> GetTokenFamilyAsync(string token, CancellationToken cancellationToken = default)
    {
    try
    {
    var refreshToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(token, cancellationToken);
    if (refreshToken == null)
        return new TokenFamilyInfo();

    // Build token chain
    var tokenChain = new List<RefreshTokenInfo>();
    var currentToken = refreshToken;

    // Go back to root token
    while (currentToken.ReplacesToken != null)
    {
        currentToken = currentToken.ReplacesToken;
    }

    // Build forward chain
    while (currentToken != null)
    {
        tokenChain.Add(new RefreshTokenInfo
        {
            TokenId = currentToken.Id.ToString(),
            CreatedOn = currentToken.CreatedOn,
            ExpiryDate = currentToken.ExpiryDate,
            IsActive = currentToken.IsValid(),
            DeviceFingerprint = currentToken.DeviceFingerprint
        });

        currentToken = currentToken.ReplacedByToken;
    }

    return new TokenFamilyInfo
    {
        FamilyId = refreshToken.JwtId,
        TokenChain = tokenChain,
        ChainLength = tokenChain.Count,
        ChainStarted = tokenChain.FirstOrDefault()?.CreatedOn ?? DateTime.UtcNow,
        ChainEnded = refreshToken.IsRevoked ? refreshToken.RevokedDate : null,
        IsActiveChain = tokenChain.Any(t => t.IsActive),
        CurrentToken = tokenChain.LastOrDefault(),
        RootToken = tokenChain.FirstOrDefault()
    };
    }
    catch (Exception ex)
    {
    _logger.LogError(ex, "Error getting token family");
    return new TokenFamilyInfo();
    }
    }

    public async Task<RefreshTokenUsageResult> RotateTokenAsync(string oldToken, string newJwtId, string? ipAddress = null,
    string? userAgent = null, string? deviceFingerprint = null, CancellationToken cancellationToken = default)
    {
    return await UseRefreshTokenAsync(oldToken, newJwtId, ipAddress, userAgent, deviceFingerprint, cancellationToken);
    }

    public string GenerateSecureTokenValue()
    {
    var tokenBytes = new byte[DefaultTokenLength];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(tokenBytes);
    return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    public bool IsValidTokenFormat(string token)
    {
    if (string.IsNullOrEmpty(token))
    return false;

    // Check length (base64 encoded 64 bytes should be around 88 characters)
    if (token.Length < 40 || token.Length > 200)
    return false;

    // Check for valid base64url characters
    return token.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
    }



    #region Private Methods

    private async Task CleanupOldTokensForUserAsync(int userId, CancellationToken cancellationToken)
    {
    try
    {
    var userTokens = await _unitOfWork.RefreshTokens.GetByUserIdAsync(userId, cancellationToken);
    var validTokens = userTokens.Where(t => t.IsValid()).OrderByDescending(t => t.CreatedOn).ToList();

    if (validTokens.Count > MaxConcurrentSessions)
    {
        var tokensToRevoke = validTokens.Skip(MaxConcurrentSessions);
        foreach (var token in tokensToRevoke)
        {
            token.Revoke("Exceeded concurrent session limit");
            _unitOfWork.RefreshTokens.Update(token);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    }
    catch (Exception ex)
    {
    _logger.LogError(ex, "Error cleaning up old tokens for user: {UserId}", userId);
    }
    }

    private async Task HandleSuspiciousActivityAsync(RefreshToken token, string reason, CancellationToken cancellationToken)
    {
    try
    {
    _logger.LogWarning("Suspicious token activity detected for user: {UserId}, Reason: {Reason}, Token: {TokenId}",
        token.UserId, reason, token.Id);

    // In a real implementation, you might:
    // 1. Send security alert to user
    // 2. Trigger additional verification
    // 3. Revoke related tokens
    // 4. Log security event

    await Task.CompletedTask;
    }
    catch (Exception ex)
    {
    _logger.LogError(ex, "Error handling suspicious activity");
    }
    }

    private string DetermineDeviceType(string? userAgent)
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

    private string DetermineBrowser(string? userAgent)
    {
    if (string.IsNullOrEmpty(userAgent))
    return "Unknown";

    userAgent = userAgent.ToLowerInvariant();

    if (userAgent.Contains("chrome") && !userAgent.Contains("edge"))
    return "Chrome";
    if (userAgent.Contains("firefox"))
    return "Firefox";
    if (userAgent.Contains("safari") && !userAgent.Contains("chrome"))
    return "Safari";
    if (userAgent.Contains("edge"))
    return "Edge";

    return "Other";
    }

    private string DetermineOperatingSystem(string? userAgent)
    {
    if (string.IsNullOrEmpty(userAgent))
    return "Unknown";

    userAgent = userAgent.ToLowerInvariant();

    if (userAgent.Contains("windows"))
    return "Windows";
    if (userAgent.Contains("mac"))
    return "macOS";
    if (userAgent.Contains("linux"))
    return "Linux";
    if (userAgent.Contains("android"))
    return "Android";
    if (userAgent.Contains("ios") || userAgent.Contains("iphone") || userAgent.Contains("ipad"))
    return "iOS";

    return "Unknown";
    }

    private string DetermineLocation(string? ipAddress)
    {
    // In a real implementation, you would use a GeoIP service
    return string.IsNullOrEmpty(ipAddress) ? "Unknown" : "Unknown Location";
    }

    private int CalculateTrustScore(RefreshToken token)
    {
    var score = 50; // Base score

    // Age of token increases trust
    var age = DateTime.UtcNow - token.CreatedOn;
    if (age.TotalDays > 1) score += 10;
    if (age.TotalDays > 7) score += 10;

    // Consistent device usage increases trust
    if (!string.IsNullOrEmpty(token.DeviceFingerprint)) score += 20;

    // Not being revoked or used inappropriately
    if (!token.IsRevoked && !token.IsUsed) score += 10;

    return Math.Min(100, Math.Max(0, score));
    }

    #endregion
    }