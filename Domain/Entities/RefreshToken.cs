using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents a refresh token for JWT authentication
/// </summary>
public class RefreshToken : BaseEntity
{
    /// <summary>
    /// The refresh token value (encrypted)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The JTI (JWT ID) of the associated access token
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string JwtId { get; set; } = string.Empty;

    /// <summary>
    /// When the refresh token expires
    /// </summary>
    [Required]
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// Whether the token has been used (one-time use)
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// Whether the token has been revoked
    /// </summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// Date when the token was revoked
    /// </summary>
    public DateTime? RevokedDate { get; set; }

    /// <summary>
    /// Reason for revocation
    /// </summary>
    [MaxLength(200)]
    public string? RevocationReason { get; set; }

    /// <summary>
    /// IP address where the token was created
    /// </summary>
    [MaxLength(45)]
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// IP address where the token was revoked
    /// </summary>
    [MaxLength(45)]
    public string? RevokedByIp { get; set; }

    /// <summary>
    /// User agent of the client that created the token
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Device fingerprint for additional security
    /// </summary>
    [MaxLength(100)]
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    /// ID of the user this token belongs to
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// ID of the token that replaced this one (for token rotation)
    /// </summary>
    public int? ReplacedByTokenId { get; set; }

    /// <summary>
    /// Navigation property to the token that replaced this one
    /// </summary>
    public virtual RefreshToken? ReplacedByToken { get; set; }

    /// <summary>
    /// Navigation property to the token this one replaced
    /// </summary>
    public virtual RefreshToken? ReplacesToken { get; set; }

    /// <summary>
    /// Checks if the refresh token is valid (not expired, used, or revoked)
    /// </summary>
    /// <returns>True if the token is valid</returns>
    public bool IsValid()
    {
        return IsActive &&
               !IsUsed &&
               !IsRevoked &&
               ExpiryDate > DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the refresh token is expired
    /// </summary>
    /// <returns>True if the token is expired</returns>
    public bool IsExpired()
    {
        return ExpiryDate <= DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the token as used
    /// </summary>
    /// <param name="usedByIp">IP address where the token was used</param>
    public void MarkAsUsed(string? usedByIp = null)
    {
        IsUsed = true;
        UpdateModifiedOn();
    }

    /// <summary>
    /// Revokes the refresh token
    /// </summary>
    /// <param name="reason">Reason for revocation</param>
    /// <param name="revokedByIp">IP address where the token was revoked</param>
    public void Revoke(string reason, string? revokedByIp = null)
    {
        IsRevoked = true;
        RevokedDate = DateTime.UtcNow;
        RevocationReason = reason;
        RevokedByIp = revokedByIp;
        IsActive = false;
        UpdateModifiedOn();
    }

    /// <summary>
    /// Creates a replacement token for token rotation
    /// </summary>
    /// <param name="newToken">New token value</param>
    /// <param name="newJwtId">New JWT ID</param>
    /// <param name="expiryDate">Expiry date for the new token</param>
    /// <param name="createdByIp">IP address creating the new token</param>
    /// <param name="userAgent">User agent creating the new token</param>
    /// <param name="deviceFingerprint">Device fingerprint</param>
    /// <returns>New refresh token</returns>
    public RefreshToken CreateReplacementToken(
        string newToken,
        string newJwtId,
        DateTime expiryDate,
        string? createdByIp = null,
        string? userAgent = null,
        string? deviceFingerprint = null)
    {
        var replacementToken = new RefreshToken
        {
            Token = newToken,
            JwtId = newJwtId,
            ExpiryDate = expiryDate,
            UserId = UserId,
            CreatedByIp = createdByIp,
            UserAgent = userAgent,
            DeviceFingerprint = deviceFingerprint,
            ReplacesToken = this
        };

        ReplacedByToken = replacementToken;
        Revoke("Replaced by new token", createdByIp);

        return replacementToken;
    }

    /// <summary>
    /// Gets the remaining time until expiry
    /// </summary>
    /// <returns>TimeSpan until expiry, or null if expired</returns>
    public TimeSpan? GetRemainingTime()
    {
        if (IsExpired())
            return null;

        return ExpiryDate - DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the token is from the same device
    /// </summary>
    /// <param name="deviceFingerprint">Device fingerprint to compare</param>
    /// <returns>True if from the same device</returns>
    public bool IsFromSameDevice(string? deviceFingerprint)
    {
        if (string.IsNullOrWhiteSpace(DeviceFingerprint) || string.IsNullOrWhiteSpace(deviceFingerprint))
            return false;

        return DeviceFingerprint.Equals(deviceFingerprint, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the token's age
    /// </summary>
    /// <returns>Time since token creation</returns>
    public TimeSpan GetAge()
    {
        return DateTime.UtcNow - CreatedOn;
    }
}