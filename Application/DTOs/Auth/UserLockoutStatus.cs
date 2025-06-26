namespace VisitorManagementSystem.Api.Application.DTOs.Auth;

/// <summary>
/// User lockout status data transfer object
/// </summary>
public class UserLockoutStatus
{
    /// <summary>
    /// Whether user is locked out
    /// </summary>
    public bool IsLockedOut { get; set; }

    /// <summary>
    /// Lockout end date
    /// </summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>
    /// Number of failed attempts
    /// </summary>
    public int FailedAttempts { get; set; }

    /// <summary>
    /// Maximum allowed failed attempts
    /// </summary>
    public int MaxFailedAttempts { get; set; }

    /// <summary>
    /// Time remaining in lockout
    /// </summary>
    public TimeSpan? TimeRemaining { get; set; }

    /// <summary>
    /// Lockout reason
    /// </summary>
    public string? Reason { get; set; }
}
