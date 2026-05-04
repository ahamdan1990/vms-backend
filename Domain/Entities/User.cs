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
    /// User's role in the system.
    /// </summary>
    /// <remarks>
    /// DEPRECATED: Migrate all usages to <see cref="RoleId"/> / <see cref="RoleEntity"/>.
    /// This column will be dropped in a future migration once all references are removed.
    /// </remarks>
    [Required]
    [Obsolete("Use RoleId and RoleEntity instead. This property will be removed in a future release.")]
    public UserRole Role { get; set; }

    /// <summary>
    /// Foreign key to Role table (new database-driven role system)
    /// Nullable during migration, will be required after data migration
    /// </summary>
    public int? RoleId { get; set; }

    /// <summary>
    /// Foreign key to Department (NEW - replacing string Department field)
    /// </summary>
    public int? DepartmentId { get; set; }

    /// <summary>
    /// Department navigation property
    /// </summary>
    public virtual Department? DepartmentEntity { get; set; }

    /// <summary>
    /// User's current status
    /// </summary>
    [Required]
    public UserStatus Status { get; set; } = UserStatus.Active;

    /// <summary>
    /// Department the user belongs to.
    /// </summary>
    /// <remarks>DEPRECATED: Use <see cref="DepartmentId"/> / <see cref="DepartmentEntity"/> instead.</remarks>
    [MaxLength(100)]
    [Obsolete("Use DepartmentId and DepartmentEntity instead. This property will be removed in a future release.")]
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
    /// Per-host invitation approval override.
    /// null  = follow the global "Invitations.RequireApprovalByDefault" system setting.
    /// true  = this host's invitations always require admin approval.
    /// false = this host's invitations are auto-approved (skip approval workflow).
    /// </summary>
    public bool? RequiresApprovalOverride { get; set; }

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
    /// Navigation property to the user's role (new database-driven system)
    /// </summary>
    public virtual Role? RoleEntity { get; set; }

    /// <summary>
    /// Email verification token for new signup accounts
    /// </summary>
    [MaxLength(500)]
    public string? EmailVerificationToken { get; set; }

    /// <summary>
    /// Email verification token expiry date
    /// </summary>
    public DateTime? EmailVerificationTokenExpiry { get; set; }

    /// <summary>
    /// Is email verified
    /// </summary>
    public bool IsEmailVerified { get; set; } = false;

    /// <summary>
    /// Date when email was verified
    /// </summary>
    public DateTime? EmailVerifiedOn { get; set; }

    /// <summary>
    /// Indicates if user was created via LDAP/AD authentication
    /// </summary>
    public bool IsLdapUser { get; set; } = false;

    /// <summary>
    /// LDAP distinguished name
    /// </summary>
    [MaxLength(500)]
    public string? LdapDistinguishedName { get; set; }

    /// <summary>
    /// Last sync with LDAP
    /// </summary>
    public DateTime? LastLdapSyncOn { get; set; }

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
    /// Checks if the user has the specified role.
    /// </summary>
    /// <remarks>DEPRECATED: Compare against <see cref="RoleEntity"/>.<see cref="Role.Name"/> instead.</remarks>
    [Obsolete("Use RoleEntity.Name comparison instead. This method will be removed in a future release.")]
    public bool HasRole(UserRole role) => Role == role;

    /// <summary>
    /// Checks if the user is an administrator.
    /// </summary>
    /// <remarks>DEPRECATED: Compare against <see cref="RoleEntity"/>.<see cref="Role.Name"/> instead.</remarks>
    [Obsolete("Use RoleEntity.Name == \"Administrator\" instead. This method will be removed in a future release.")]
    public bool IsAdministrator() => Role == UserRole.Administrator;

    /// <summary>
    /// Checks if the user is staff.
    /// </summary>
    /// <remarks>DEPRECATED: Compare against <see cref="RoleEntity"/>.<see cref="Role.Name"/> instead.</remarks>
    [Obsolete("Use RoleEntity.Name == \"Staff\" instead. This method will be removed in a future release.")]
    public bool IsStaff() => Role == UserRole.Staff;

    /// <summary>
    /// Checks if the user is a receptionist.
    /// </summary>
    /// <remarks>DEPRECATED: Compare against <see cref="RoleEntity"/>.<see cref="Role.Name"/> instead.</remarks>
    [Obsolete("Use RoleEntity.Name == \"Receptionist\" instead. This method will be removed in a future release.")]
    public bool IsReceptionist() => Role == UserRole.Receptionist;

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