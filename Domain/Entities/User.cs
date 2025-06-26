using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents a user in the system with authentication and authorization capabilities
/// </summary>
public class User : SoftDeleteEntity
{
    /// <summary>
    /// User's first name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User's email address (unique identifier)
    /// </summary>
    [Required]
    public Email Email { get; set; } = null!;

    /// <summary>
    /// Normalized email for case-insensitive comparisons
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string NormalizedEmail { get; set; } = string.Empty;

    /// <summary>
    /// Hashed password
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Salt used for password hashing
    /// </summary>
    [Required]
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>
    /// User's phone number
    /// </summary>
    public PhoneNumber? PhoneNumber { get; set; }

    /// <summary>
    /// User's role in the system
    /// </summary>
    [Required]
    public UserRole Role { get; set; }

    /// <summary>
    /// User's current status
    /// </summary>
    [Required]
    public UserStatus Status { get; set; } = UserStatus.Active;

    /// <summary>
    /// Department the user belongs to
    /// </summary>
    [MaxLength(100)]
    public string? Department { get; set; }

    /// <summary>
    /// Job title of the user
    /// </summary>
    [MaxLength(100)]
    public string? JobTitle { get; set; }

    /// <summary>
    /// Employee ID or badge number
    /// </summary>
    [MaxLength(50)]
    public string? EmployeeId { get; set; }

    /// <summary>
    /// User's profile photo path
    /// </summary>
    [MaxLength(500)]
    public string? ProfilePhotoPath { get; set; }

    /// <summary>
    /// Date when the user last logged in
    /// </summary>
    public DateTime? LastLoginDate { get; set; }

    /// <summary>
    /// Number of failed login attempts
    /// </summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// Date and time when the account is locked out until
    /// </summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>
    /// Indicates whether the user account is locked out
    /// </summary>
    public bool IsLockedOut { get; set; } = false;

    /// <summary>
    /// Indicates whether the user must change password on next login
    /// </summary>
    public bool MustChangePassword { get; set; } = false;

    /// <summary>
    /// Date when the password was last changed
    /// </summary>
    public DateTime? PasswordChangedDate { get; set; }

    /// <summary>
    /// Security stamp for invalidating tokens
    /// </summary>
    [Required]
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User's timezone
    /// </summary>
    [MaxLength(50)]
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// User's preferred language
    /// </summary>
    [MaxLength(10)]
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// User's theme preference
    /// </summary>
    [MaxLength(20)]
    public string Theme { get; set; } = "light";

    /// <summary>
    /// Navigation property for refresh tokens
    /// </summary>
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    /// <summary>
    /// Navigation property for audit logs where this user is the creator
    /// </summary>
    public virtual ICollection<AuditLog> CreatedAuditLogs { get; set; } = new List<AuditLog>();

    /// <summary>
    /// Gets the user's full name
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Gets the user's display name (full name or email if name is empty)
    /// </summary>
    public string DisplayName => !string.IsNullOrWhiteSpace(FullName) ? FullName : Email.Value;

    public Address? Address { get; internal set; }

    /// <summary>
    /// Checks if the user account is currently locked out
    /// </summary>
    /// <returns>True if the account is locked out</returns>
    public bool IsCurrentlyLockedOut()
    {
        return IsLockedOut && LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// Increments failed login attempts and locks account if threshold is reached
    /// </summary>
    /// <param name="maxAttempts">Maximum allowed failed attempts before lockout</param>
    /// <param name="lockoutDuration">Duration of lockout</param>
    public void IncrementFailedLoginAttempts(int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= maxAttempts)
        {
            LockAccount(lockoutDuration);
        }

        UpdateModifiedOn();
    }

    /// <summary>
    /// Resets failed login attempts after successful login
    /// </summary>
    public void ResetFailedLoginAttempts()
    {
        FailedLoginAttempts = 0;
        IsLockedOut = false;
        LockoutEnd = null;
        LastLoginDate = DateTime.UtcNow;
        UpdateModifiedOn();
    }

    /// <summary>
    /// Locks the user account for the specified duration
    /// </summary>
    /// <param name="lockoutDuration">Duration of the lockout</param>
    public void LockAccount(TimeSpan lockoutDuration)
    {
        IsLockedOut = true;
        LockoutEnd = DateTime.UtcNow.Add(lockoutDuration);
        UpdateModifiedOn();
    }

    /// <summary>
    /// Unlocks the user account
    /// </summary>
    public void UnlockAccount()
    {
        IsLockedOut = false;
        LockoutEnd = null;
        FailedLoginAttempts = 0;
        UpdateModifiedOn();
    }

    /// <summary>
    /// Changes the user's password
    /// </summary>
    /// <param name="newPasswordHash">New password hash</param>
    /// <param name="newPasswordSalt">New password salt</param>
    public void ChangePassword(string newPasswordHash, string newPasswordSalt)
    {
        PasswordHash = newPasswordHash;
        PasswordSalt = newPasswordSalt;
        PasswordChangedDate = DateTime.UtcNow;
        MustChangePassword = false;
        SecurityStamp = Guid.NewGuid().ToString(); // Invalidate existing tokens
        UpdateModifiedOn();
    }

    /// <summary>
    /// Updates the user's security stamp to invalidate tokens
    /// </summary>
    public void UpdateSecurityStamp()
    {
        SecurityStamp = Guid.NewGuid().ToString();
        UpdateModifiedOn();
    }

    /// <summary>
    /// Checks if the user has the specified role
    /// </summary>
    /// <param name="role">Role to check</param>
    /// <returns>True if the user has the role</returns>
    public bool HasRole(UserRole role)
    {
        return Role == role;
    }

    /// <summary>
    /// Checks if the user is an administrator
    /// </summary>
    /// <returns>True if the user is an administrator</returns>
    public bool IsAdministrator()
    {
        return Role == UserRole.Administrator;
    }

    /// <summary>
    /// Checks if the user is staff
    /// </summary>
    /// <returns>True if the user is staff</returns>
    public bool IsStaff()
    {
        return Role == UserRole.Staff;
    }

    /// <summary>
    /// Checks if the user is an operator
    /// </summary>
    /// <returns>True if the user is an operator</returns>
    public bool IsOperator()
    {
        return Role == UserRole.Operator;
    }

    /// <summary>
    /// Validates the user's current state
    /// </summary>
    /// <returns>True if the user is valid for authentication</returns>
    public bool IsValidForAuthentication()
    {
        return IsActive &&
               !IsDeleted &&
               Status == UserStatus.Active &&
               !IsCurrentlyLockedOut();
    }

    /// <summary>
    /// Sets user preferences
    /// </summary>
    /// <param name="timeZone">User's timezone</param>
    /// <param name="language">User's preferred language</param>
    /// <param name="theme">User's theme preference</param>
    public void UpdatePreferences(string? timeZone = null, string? language = null, string? theme = null)
    {
        if (!string.IsNullOrWhiteSpace(timeZone))
            TimeZone = timeZone;

        if (!string.IsNullOrWhiteSpace(language))
            Language = language;

        if (!string.IsNullOrWhiteSpace(theme))
            Theme = theme;

        UpdateModifiedOn();
    }
}