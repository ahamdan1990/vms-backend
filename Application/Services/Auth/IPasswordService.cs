using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Application.Services.Auth;

/// <summary>
/// Password service interface for password hashing, validation, and policy enforcement
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Hashes a password with a new salt
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Password hash result with hash and salt</returns>
    PasswordHashResult HashPassword(string password);

    /// <summary>
    /// Verifies a password against its hash
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <param name="hash">Stored password hash</param>
    /// <param name="salt">Password salt</param>
    /// <returns>True if password is correct</returns>
    bool VerifyPassword(string password, string hash, string salt);

    /// <summary>
    /// Validates password against security policy
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <param name="user">User entity (for context-specific validation)</param>
    /// <returns>Password validation result</returns>
    PasswordValidationResult ValidatePassword(string password, User? user = null);

    /// <summary>
    /// Generates a secure random password
    /// </summary>
    /// <param name="length">Password length</param>
    /// <param name="includeSpecialCharacters">Include special characters</param>
    /// <param name="excludeSimilarCharacters">Exclude visually similar characters</param>
    /// <returns>Generated password</returns>
    string GeneratePassword(int length = 12, bool includeSpecialCharacters = true,
        bool excludeSimilarCharacters = true);

    /// <summary>
    /// Calculates password strength score
    /// </summary>
    /// <param name="password">Password to analyze</param>
    /// <returns>Password strength result</returns>
    PasswordStrengthResult CalculateStrength(string password);

    /// <summary>
    /// Checks if password meets minimum requirements
    /// </summary>
    /// <param name="password">Password to check</param>
    /// <returns>True if password meets requirements</returns>
    bool MeetsMinimumRequirements(string password);

    /// <summary>
    /// Checks if password is commonly used or compromised
    /// </summary>
    /// <param name="password">Password to check</param>
    /// <returns>True if password is compromised</returns>
    Task<bool> IsPasswordCompromisedAsync(string password);

    /// <summary>
    /// Checks if password has been used recently by user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="password">New password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if password was used recently</returns>
    Task<bool> IsPasswordRecentlyUsedAsync(int userId, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves password to user's password history
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="passwordHash">Password hash</param>
    /// <param name="passwordSalt">Password salt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SavePasswordHistoryAsync(int userId, string passwordHash, string passwordSalt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if password is expired for user
    /// </summary>
    /// <param name="user">User entity</param>
    /// <returns>True if password is expired</returns>
    bool IsPasswordExpired(User user);

    /// <summary>
    /// Gets password expiry date for user
    /// </summary>
    /// <param name="user">User entity</param>
    /// <returns>Password expiry date</returns>
    DateTime? GetPasswordExpiryDate(User user);

    /// <summary>
    /// Gets days until password expires
    /// </summary>
    /// <param name="user">User entity</param>
    /// <returns>Days until expiry, or null if no expiry</returns>
    int? GetDaysUntilPasswordExpiry(User user);

    /// <summary>
    /// Generates password reset instructions
    /// </summary>
    /// <param name="user">User entity</param>
    /// <returns>Password reset instructions</returns>
    PasswordResetInstructions GenerateResetInstructions(User user);

    /// <summary>
    /// Validates new password change request
    /// </summary>
    /// <param name="user">User entity</param>
    /// <param name="currentPassword">Current password</param>
    /// <param name="newPassword">New password</param>
    /// <returns>Validation result</returns>
    Task<PasswordChangeValidationResult> ValidatePasswordChangeAsync(User user, string currentPassword,
        string newPassword);

    /// <summary>
    /// Gets password policy configuration
    /// </summary>
    /// <returns>Password policy</returns>
    PasswordPolicy GetPasswordPolicy();

    /// <summary>
    /// Updates password policy
    /// </summary>
    /// <param name="policy">New password policy</param>
    /// <returns>Task</returns>
    Task UpdatePasswordPolicyAsync(PasswordPolicy policy);

    /// <summary>
    /// Gets password complexity requirements as user-friendly text
    /// </summary>
    /// <returns>List of requirements</returns>
    List<string> GetPasswordRequirements();

    /// <summary>
    /// Checks if password contains user's personal information
    /// </summary>
    /// <param name="password">Password to check</param>
    /// <param name="user">User entity</param>
    /// <returns>True if password contains personal info</returns>
    bool ContainsPersonalInformation(string password, User user);

    /// <summary>
    /// Generates password reset token for user
    /// </summary>
    /// <param name="user">User entity</param>
    /// <returns>Reset token</returns>
    string GeneratePasswordResetToken(User user);

    /// <summary>
    /// Validates password reset token
    /// </summary>
    /// <param name="user">User entity</param>
    /// <param name="token">Reset token</param>
    /// <returns>True if token is valid</returns>
    bool ValidatePasswordResetToken(User user, string token);

    /// <summary>
    /// Clears password history for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task ClearPasswordHistoryAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a temporary password for administrative password resets
    /// </summary>
    /// <returns>Generated temporary password</returns>
    string GenerateTemporaryPassword();
}

/// <summary>
/// Password hash result
/// </summary>
public class PasswordHashResult
{
    public string Hash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public int Iterations { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Password validation result
/// </summary>
public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public PasswordStrengthResult? StrengthResult { get; set; }
    public bool IsCompromised { get; set; }
    public bool ContainsPersonalInfo { get; set; }
    public bool IsRecentlyUsed { get; set; }
}

/// <summary>
/// Password strength result
/// </summary>
public class PasswordStrengthResult
{
    public int Score { get; set; } // 0-100
    public PasswordStrength Strength { get; set; }
    public List<string> Feedback { get; set; } = new();
    public TimeSpan EstimatedCrackTime { get; set; }
    public bool HasLowercase { get; set; }
    public bool HasUppercase { get; set; }
    public bool HasDigits { get; set; }
    public bool HasSpecialCharacters { get; set; }
    public bool HasRepeatingPatterns { get; set; }
    public bool HasSequentialCharacters { get; set; }
    public bool HasCommonWords { get; set; }
    public int Length { get; set; }
    public int UniqueCharacters { get; set; }
    public double Entropy { get; set; }
}

/// <summary>
/// Password strength levels
/// </summary>
public enum PasswordStrength
{
    VeryWeak = 0,
    Weak = 1,
    Fair = 2,
    Good = 3,
    Strong = 4,
    VeryStrong = 5
}

/// <summary>
/// Password change validation result
/// </summary>
public class PasswordChangeValidationResult
{
    public bool IsValid { get; set; }
    public bool IsCurrentPasswordCorrect { get; set; }
    public PasswordValidationResult? NewPasswordValidation { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool RequiresSecurityQuestions { get; set; }
    public bool RequiresTwoFactor { get; set; }
}

/// <summary>
/// Password reset instructions
/// </summary>
public class PasswordResetInstructions
{
    public string Instructions { get; set; } = string.Empty;
    public List<string> Requirements { get; set; } = new();
    public DateTime ValidUntil { get; set; }
    public string? ContactInfo { get; set; }
    public List<string> SecurityTips { get; set; } = new();
}

/// <summary>
/// Password policy configuration
/// </summary>
public class PasswordPolicy
{
    public int MinimumLength { get; set; } = 8;
    public int MaximumLength { get; set; } = 128;
    public bool RequireDigit { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; } = true;
    public int RequiredUniqueChars { get; set; } = 3;
    public int PasswordHistoryCount { get; set; } = 5;
    public int PasswordExpiryDays { get; set; } = 90;
    public int PasswordExpiryWarningDays { get; set; } = 14;
    public bool PreventPersonalInfo { get; set; } = true;
    public bool CheckCompromisedPasswords { get; set; } = true;
    public bool RequirePeriodicChange { get; set; } = true;
    public int MinimumAge { get; set; } = 1; // Days before password can be changed again
    public int MaximumAge { get; set; } = 90; // Days before password must be changed
    public List<string> ForbiddenPasswords { get; set; } = new();
    public List<string> ForbiddenPatterns { get; set; } = new();
    public bool AllowPasswordReuse { get; set; } = false;
    public int MinimumScoreRequired { get; set; } = 60; // Minimum strength score
}

/// <summary>
/// Password history entry
/// </summary>
public class PasswordHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public bool IsActive { get; set; }
}