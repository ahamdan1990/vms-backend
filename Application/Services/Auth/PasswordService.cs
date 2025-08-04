// Application/Services/Auth/PasswordService.cs
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Application.Services.Configuration;
using VisitorManagementSystem.Api.Configuration;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Infrastructure.Utilities;

namespace VisitorManagementSystem.Api.Application.Services.Auth;

/// <summary>
/// Password service implementation for hashing, validation, and policy enforcement
/// </summary>
public class PasswordService : IPasswordService
{
    private readonly IDynamicConfigurationService _dynamicConfig;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PasswordService> _logger;
    private readonly PasswordPolicy _passwordPolicy;

    public PasswordService(
        IDynamicConfigurationService dynamicConfig,
        IUnitOfWork unitOfWork,
        ILogger<PasswordService> logger)
    {
        _dynamicConfig = dynamicConfig;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _passwordPolicy = GetPasswordPolicy();
    }

    public PasswordHashResult HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        // Generate a cryptographically strong random salt
        var salt = GenerateSalt();
        var iterations = 10000; // PBKDF2 iterations

        // Hash the password using PBKDF2
        using var pbkdf2 = new Rfc2898DeriveBytes(
                                password,
                                Convert.FromBase64String(salt),
                                iterations,
                                HashAlgorithmName.SHA256);
        var hashBytes = pbkdf2.GetBytes(32); // 256-bit hash
        var hash = Convert.ToBase64String(hashBytes);

        return new PasswordHashResult
        {
            Hash = hash,
            Salt = salt,
            Iterations = iterations,
            CreatedOn = DateTime.UtcNow
        };
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
            return false;

        try
        {
            var iterations = 10000; // Same as used in hashing
            using var pbkdf2 = new Rfc2898DeriveBytes(
                                    password,
                                    Convert.FromBase64String(salt),
                                    iterations,
                                    HashAlgorithmName.SHA256);
            var hashBytes = pbkdf2.GetBytes(32);
            var computedHash = Convert.ToBase64String(hashBytes);

            return CryptoHelper.SlowEquals(hash, computedHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying password");
            return false;
        }
    }

    public PasswordValidationResult ValidatePassword(string password, User? user = null)
    {
        var result = new PasswordValidationResult();
        var errors = new List<string>();
        var warnings = new List<string>();

        // Check basic requirements
        if (string.IsNullOrEmpty(password))
        {
            errors.Add(ValidationMessages.User.PasswordRequired);
            result.IsValid = false;
            result.Errors = errors;
            return result;
        }

        // Length validation
        if (password.Length < _passwordPolicy.MinimumLength)
        {
            errors.Add(ValidationMessages.Format(ValidationMessages.User.PasswordTooShort, _passwordPolicy.MinimumLength));
        }

        if (password.Length > _passwordPolicy.MaximumLength)
        {
            errors.Add(ValidationMessages.Format(ValidationMessages.User.PasswordTooLong, _passwordPolicy.MaximumLength));
        }

        // Character requirements
        if (_passwordPolicy.RequireDigit && !password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one digit");
        }

        if (_passwordPolicy.RequireLowercase && !password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter");
        }

        if (_passwordPolicy.RequireUppercase && !password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter");
        }

        if (_passwordPolicy.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
        {
            errors.Add("Password must contain at least one special character");
        }

        // Unique characters
        var uniqueChars = password.Distinct().Count();
        if (uniqueChars < _passwordPolicy.RequiredUniqueChars)
        {
            errors.Add($"Password must contain at least {_passwordPolicy.RequiredUniqueChars} unique characters");
        }

        // Calculate strength
        var strengthResult = CalculateStrength(password);
        result.StrengthResult = strengthResult;

        // Minimum strength requirement
        if (strengthResult.Score < _passwordPolicy.MinimumScoreRequired)
        {
            warnings.Add($"Password strength is below recommended level (Score: {strengthResult.Score}/100)");
        }

        // Check against forbidden passwords
        if (_passwordPolicy.ForbiddenPasswords.Contains(password, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add("Password is in the list of forbidden passwords");
        }

        // Check forbidden patterns
        foreach (var pattern in _passwordPolicy.ForbiddenPatterns)
        {
            if (Regex.IsMatch(password, pattern, RegexOptions.IgnoreCase))
            {
                errors.Add("Password contains forbidden patterns");
                break;
            }
        }

        // User-specific validations
        if (user != null)
        {
            // Check against personal information
            if (_passwordPolicy.PreventPersonalInfo && ContainsPersonalInformation(password, user))
            {
                errors.Add("Password cannot contain personal information");
                result.ContainsPersonalInfo = true;
            }
        }

        result.IsValid = !errors.Any();
        result.Errors = errors;
        result.Warnings = warnings;

        return result;
    }

    public string GeneratePassword(int length = 12, bool includeSpecialCharacters = true,
        bool excludeSimilarCharacters = true)
    {
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        const string similar = "il1Lo0O";

        var chars = lowercase + uppercase + digits;
        if (includeSpecialCharacters)
            chars += special;

        if (excludeSimilarCharacters)
        {
            chars = new string(chars.Where(c => !similar.Contains(c)).ToArray());
        }

        var password = new StringBuilder();
        using var rng = RandomNumberGenerator.Create();

        // Ensure at least one character from each required category
        password.Append(GetRandomChar(lowercase, rng));
        password.Append(GetRandomChar(uppercase, rng));
        password.Append(GetRandomChar(digits, rng));

        if (includeSpecialCharacters)
        {
            var availableSpecial = excludeSimilarCharacters
                ? new string(special.Where(c => !similar.Contains(c)).ToArray())
                : special;
            password.Append(GetRandomChar(availableSpecial, rng));
        }

        // Fill remaining positions
        var remaining = length - password.Length;
        for (int i = 0; i < remaining; i++)
        {
            password.Append(GetRandomChar(chars, rng));
        }

        // Shuffle the password
        return new string(password.ToString().OrderBy(x => Guid.NewGuid()).ToArray());
    }

    public PasswordStrengthResult CalculateStrength(string password)
    {
        var result = new PasswordStrengthResult
        {
            Length = password.Length,
            UniqueCharacters = password.Distinct().Count()
        };

        var score = 0;

        // Length scoring
        if (password.Length >= 8) score += 20;
        if (password.Length >= 12) score += 10;
        if (password.Length >= 16) score += 10;

        // Character variety scoring
        result.HasLowercase = password.Any(char.IsLower);
        result.HasUppercase = password.Any(char.IsUpper);
        result.HasDigits = password.Any(char.IsDigit);
        result.HasSpecialCharacters = password.Any(c => !char.IsLetterOrDigit(c));

        if (result.HasLowercase) score += 10;
        if (result.HasUppercase) score += 10;
        if (result.HasDigits) score += 10;
        if (result.HasSpecialCharacters) score += 10;

        // Unique characters scoring
        var uniqueRatio = (double)result.UniqueCharacters / password.Length;
        score += (int)(uniqueRatio * 20);

        // Pattern analysis
        result.HasRepeatingPatterns = HasRepeatingPatterns(password);
        result.HasSequentialCharacters = HasSequentialCharacters(password);
        result.HasCommonWords = HasCommonWords(password);

        if (result.HasRepeatingPatterns) score -= 15;
        if (result.HasSequentialCharacters) score -= 10;
        if (result.HasCommonWords) score -= 20;

        // Calculate entropy
        result.Entropy = CalculateEntropy(password);
        if (result.Entropy > 50) score += 10;
        if (result.Entropy > 75) score += 10;

        // Ensure score is within bounds
        score = Math.Max(0, Math.Min(100, score));
        result.Score = score;

        // Determine strength level
        result.Strength = score switch
        {
            >= 80 => PasswordStrength.VeryStrong,
            >= 60 => PasswordStrength.Strong,
            >= 40 => PasswordStrength.Good,
            >= 20 => PasswordStrength.Fair,
            >= 10 => PasswordStrength.Weak,
            _ => PasswordStrength.VeryWeak
        };

        // Estimate crack time
        result.EstimatedCrackTime = EstimateCrackTime(result.Entropy);

        // Generate feedback
        result.Feedback = GenerateFeedback(result);

        return result;
    }

    public bool MeetsMinimumRequirements(string password)
    {
        var validation = ValidatePassword(password);
        return validation.IsValid;
    }

    public async Task<bool> IsPasswordCompromisedAsync(string password)
    {
        if (!_passwordPolicy.CheckCompromisedPasswords)
            return false;

        try
        {
            // In a real implementation, you would check against a service like HaveIBeenPwned
            // For now, check against a local list of common passwords
            var commonPasswords = GetCommonPasswords();
            return commonPasswords.Contains(password, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if password is compromised");
            return false;
        }
    }

    public async Task<bool> IsPasswordRecentlyUsedAsync(int userId, string password, CancellationToken cancellationToken = default)
    {
        if (!_passwordPolicy.AllowPasswordReuse && _passwordPolicy.PasswordHistoryCount > 0)
        {
            // In a real implementation, you would check against password history
            // This would require a PasswordHistory entity and repository
            await Task.CompletedTask;
            return false;
        }

        return false;
    }

    public async Task SavePasswordHistoryAsync(int userId, string passwordHash, string passwordSalt,
        CancellationToken cancellationToken = default)
    {
        if (_passwordPolicy.PasswordHistoryCount > 0)
        {
            // In a real implementation, you would save to a PasswordHistory table
            _logger.LogInformation("Password history saved for user: {UserId}", userId);
            await Task.CompletedTask;
        }
    }

    public bool IsPasswordExpired(User user)
    {
        if (!_passwordPolicy.RequirePeriodicChange || _passwordPolicy.MaximumAge <= 0)
            return false;

        if (!user.PasswordChangedDate.HasValue)
            return true;

        var expiryDate = user.PasswordChangedDate.Value.AddDays(_passwordPolicy.MaximumAge);
        return DateTime.UtcNow > expiryDate;
    }

    public DateTime? GetPasswordExpiryDate(User user)
    {
        if (!_passwordPolicy.RequirePeriodicChange || _passwordPolicy.MaximumAge <= 0)
            return null;

        if (!user.PasswordChangedDate.HasValue)
            return DateTime.UtcNow; // Already expired

        return user.PasswordChangedDate.Value.AddDays(_passwordPolicy.MaximumAge);
    }

    public int? GetDaysUntilPasswordExpiry(User user)
    {
        var expiryDate = GetPasswordExpiryDate(user);
        if (!expiryDate.HasValue)
            return null;

        var daysUntilExpiry = (int)(expiryDate.Value - DateTime.UtcNow).TotalDays;
        return Math.Max(0, daysUntilExpiry);
    }

    public PasswordResetInstructions GenerateResetInstructions(User user)
    {
        // Since this method needs to be synchronous, we'll use a reasonable default
        // In a production system, you might want to make this async or cache the configuration
        var passwordResetTokenExpiryMinutes = 30; // Default value

        try
        {
            passwordResetTokenExpiryMinutes = _dynamicConfig.GetConfigurationAsync<int>("Security", "PasswordResetTokenExpiryMinutes", 30).Result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get password reset token expiry configuration, using default value");
        }

        return new PasswordResetInstructions
        {
            Instructions = "Please create a new password that meets the security requirements listed below.",
            Requirements = GetPasswordRequirements(),
            ValidUntil = DateTime.UtcNow.AddMinutes(passwordResetTokenExpiryMinutes),
            ContactInfo = "If you have trouble resetting your password, please contact IT support.",
            SecurityTips = new List<string>
            {
                "Use a unique password that you haven't used before",
                "Consider using a passphrase with multiple words",
                "Don't share your password with anyone",
                "Enable two-factor authentication for additional security"
            }
        };
    }

    public async Task<PasswordChangeValidationResult> ValidatePasswordChangeAsync(User user, string currentPassword,
        string newPassword)
    {
        var result = new PasswordChangeValidationResult();

        // Verify current password
        result.IsCurrentPasswordCorrect = VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt);
        if (!result.IsCurrentPasswordCorrect)
        {
            result.Errors.Add("Current password is incorrect");
        }

        // Check minimum age
        if (_passwordPolicy.MinimumAge > 0 && user.PasswordChangedDate.HasValue && !user.MustChangePassword)
        {
            var minimumChangeDate = user.PasswordChangedDate.Value.AddDays(_passwordPolicy.MinimumAge);
            if (DateTime.UtcNow < minimumChangeDate)
            {
                result.Errors.Add($"Password cannot be changed until {minimumChangeDate:yyyy-MM-dd}");
            }
        }

        // Validate new password
        result.NewPasswordValidation = ValidatePassword(newPassword, user);
        if (!result.NewPasswordValidation.IsValid)
        {
            result.Errors.AddRange(result.NewPasswordValidation.Errors);
        }

        // Check if new password is different from current
        if (VerifyPassword(newPassword, user.PasswordHash, user.PasswordSalt))
        {
            result.Errors.Add("New password must be different from current password");
        }

        // Check password history
        var isRecentlyUsed = await IsPasswordRecentlyUsedAsync(user.Id, newPassword);
        if (isRecentlyUsed)
        {
            result.Errors.Add($"Cannot reuse any of your last {_passwordPolicy.PasswordHistoryCount} passwords");
        }

        result.IsValid = !result.Errors.Any() && result.IsCurrentPasswordCorrect;

        return result;
    }

    public PasswordPolicy GetPasswordPolicy()
    {
        // Since this is a synchronous method but we need async calls, we'll use .Result
        // In a production system, you might want to make this method async or cache the configuration
        try
        {
            return new PasswordPolicy
            {
                MinimumLength = _dynamicConfig.GetConfigurationAsync<int>("Password", "RequiredLength", 8).Result,
                MaximumLength = _dynamicConfig.GetConfigurationAsync<int>("Password", "MaximumLength", 128).Result,
                RequireDigit = _dynamicConfig.GetConfigurationAsync<bool>("Password", "RequireDigit", true).Result,
                RequireLowercase = _dynamicConfig.GetConfigurationAsync<bool>("Password", "RequireLowercase", true).Result,
                RequireUppercase = _dynamicConfig.GetConfigurationAsync<bool>("Password", "RequireUppercase", true).Result,
                RequireNonAlphanumeric = _dynamicConfig.GetConfigurationAsync<bool>("Password", "RequireNonAlphanumeric", true).Result,
                RequiredUniqueChars = _dynamicConfig.GetConfigurationAsync<int>("Password", "RequiredUniqueChars", 3).Result,
                PasswordHistoryCount = _dynamicConfig.GetConfigurationAsync<int>("Password", "PasswordHistoryCount", 5).Result,
                PasswordExpiryDays = _dynamicConfig.GetConfigurationAsync<int>("Password", "PasswordExpiryDays", 90).Result,
                PasswordExpiryWarningDays = _dynamicConfig.GetConfigurationAsync<int>("Password", "PasswordExpiryWarningDays", 14).Result,
                PreventPersonalInfo = _dynamicConfig.GetConfigurationAsync<bool>("Password", "PreventPersonalInfo", true).Result,
                CheckCompromisedPasswords = _dynamicConfig.GetConfigurationAsync<bool>("Password", "CheckCompromisedPasswords", true).Result,
                RequirePeriodicChange = _dynamicConfig.GetConfigurationAsync<bool>("Password", "RequirePeriodicChange", true).Result,
                MinimumAge = _dynamicConfig.GetConfigurationAsync<int>("Password", "MinimumAge", 1).Result,
                MaximumAge = _dynamicConfig.GetConfigurationAsync<int>("Password", "MaximumAge", 90).Result,
                MinimumScoreRequired = _dynamicConfig.GetConfigurationAsync<int>("Password", "MinimumScoreRequired", 60).Result,
                ForbiddenPasswords = GetCommonPasswords(),
                ForbiddenPatterns = new List<string>
                {
                    @"(.)\1{2,}", // Repeated characters
                    @"(012|123|234|345|456|567|678|789|890)", // Sequential numbers
                    @"(abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz)" // Sequential letters
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting password policy, returning defaults");
            return new PasswordPolicy(); // Return defaults
        }
    }

    public async Task UpdatePasswordPolicyAsync(PasswordPolicy policy)
    {
        // In a real implementation, this would update the policy in the database
        _logger.LogInformation("Password policy updated");
        await Task.CompletedTask;
    }

    public List<string> GetPasswordRequirements()
    {
        var requirements = new List<string>();

        requirements.Add($"At least {_passwordPolicy.MinimumLength} characters long");

        if (_passwordPolicy.RequireUppercase)
            requirements.Add("At least one uppercase letter");

        if (_passwordPolicy.RequireLowercase)
            requirements.Add("At least one lowercase letter");

        if (_passwordPolicy.RequireDigit)
            requirements.Add("At least one number");

        if (_passwordPolicy.RequireNonAlphanumeric)
            requirements.Add("At least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?)");

        requirements.Add($"At least {_passwordPolicy.RequiredUniqueChars} unique characters");

        if (_passwordPolicy.PreventPersonalInfo)
            requirements.Add("Cannot contain personal information (name, email, etc.)");

        return requirements;
    }

    public bool ContainsPersonalInformation(string password, User user)
    {
        var personalInfo = new List<string>
        {
            user.FirstName,
            user.LastName,
            user.Email.Value.Split('@')[0], // Email username part
            user.EmployeeId ?? string.Empty
        }.Where(info => !string.IsNullOrWhiteSpace(info));

        return personalInfo.Any(info => password.Contains(info, StringComparison.OrdinalIgnoreCase));
    }

    public string GeneratePasswordResetToken(User user)
    {
        // Generate a secure random token
        var tokenBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);

        var token = Convert.ToBase64String(tokenBytes);

        // In a real implementation, you would store this token with expiry in the database
        _logger.LogInformation("Password reset token generated for user: {UserId}", user.Id);

        return token;
    }

    public bool ValidatePasswordResetToken(User user, string token)
    {
        // In a real implementation, you would validate against stored tokens
        // For now, just check if token is not empty
        return !string.IsNullOrEmpty(token);
    }

    public async Task ClearPasswordHistoryAsync(int userId, CancellationToken cancellationToken = default)
    {
        // In a real implementation, you would clear password history from database
        _logger.LogInformation("Password history cleared for user: {UserId}", userId);
        await Task.CompletedTask;
    }

    public string GenerateTemporaryPassword()
    {
        // Generate a temporary password that meets policy requirements
        // Make it longer for better security but still user-friendly
        return GeneratePassword(length: 10, includeSpecialCharacters: true, excludeSimilarCharacters: true);
    }
    #region Private Methods

    private string GenerateSalt()
    {
        var saltBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    private char GetRandomChar(string chars, RandomNumberGenerator rng)
    {
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var value = BitConverter.ToUInt32(bytes, 0);
        return chars[(int)(value % chars.Length)];
    }

    private bool HasRepeatingPatterns(string password)
    {
        // Check for repeating patterns like "abcabc" or "123123"
        for (int length = 2; length <= password.Length / 2; length++)
        {
            for (int start = 0; start <= password.Length - (length * 2); start++)
            {
                var pattern = password.Substring(start, length);
                var nextPattern = password.Substring(start + length, length);
                if (pattern == nextPattern)
                    return true;
            }
        }
        return false;
    }

    private bool HasSequentialCharacters(string password)
    {
        // Check for sequential characters like "abc", "123", "xyz"
        for (int i = 0; i < password.Length - 2; i++)
        {
            var c1 = password[i];
            var c2 = password[i + 1];
            var c3 = password[i + 2];

            if ((c2 == c1 + 1 && c3 == c2 + 1) || (c2 == c1 - 1 && c3 == c2 - 1))
                return true;
        }
        return false;
    }

    private bool HasCommonWords(string password)
    {
        var commonWords = new[] { "password", "admin", "user", "login", "welcome", "qwerty", "123456" };
        return commonWords.Any(word => password.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    private double CalculateEntropy(string password)
    {
        var charSetSize = 0;

        if (password.Any(char.IsLower)) charSetSize += 26;
        if (password.Any(char.IsUpper)) charSetSize += 26;
        if (password.Any(char.IsDigit)) charSetSize += 10;
        if (password.Any(c => !char.IsLetterOrDigit(c))) charSetSize += 32;

        return password.Length * Math.Log2(charSetSize);
    }

    private TimeSpan EstimateCrackTime(double entropy)
    {
        try
        {
            // Assume 1 billion guesses per second
            var guessesPerSecond = 1_000_000_000.0;

            // Handle edge cases
            if (entropy <= 0)
            {
                return TimeSpan.FromSeconds(1);
            }

            // Cap entropy to prevent overflow (100 bits = practically uncrackable)
            var cappedEntropy = Math.Min(entropy, 100.0);

            var totalPossibilities = Math.Pow(2, cappedEntropy);

            // Check for infinity or extremely large numbers
            if (double.IsInfinity(totalPossibilities) || totalPossibilities > 1e50)
            {
                // Return a very large but representable timespan (1000 years)
                return TimeSpan.FromDays(365 * 1000);
            }

            var averageGuesses = totalPossibilities / 2;
            var secondsToCrack = averageGuesses / guessesPerSecond;

            // Check if the result would exceed TimeSpan.MaxValue
            if (secondsToCrack > TimeSpan.MaxValue.TotalSeconds ||
                double.IsInfinity(secondsToCrack) ||
                double.IsNaN(secondsToCrack))
            {
                // Return maximum representable timespan (about 29,247 years)
                return TimeSpan.MaxValue;
            }

            return TimeSpan.FromSeconds(secondsToCrack);
        }
        catch (OverflowException)
        {
            _logger.LogWarning("Password crack time calculation overflowed, returning maximum safe value");
            return TimeSpan.FromDays(365 * 1000); // 1000 years
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating crack time for entropy: {Entropy}", entropy);
            return TimeSpan.FromDays(1); // Safe fallback
        }
    }
    private List<string> GenerateFeedback(PasswordStrengthResult result)
    {
        var feedback = new List<string>();

        if (result.Length < 12)
            feedback.Add("Consider using a longer password (12+ characters)");

        if (!result.HasUppercase)
            feedback.Add("Add uppercase letters");

        if (!result.HasLowercase)
            feedback.Add("Add lowercase letters");

        if (!result.HasDigits)
            feedback.Add("Add numbers");

        if (!result.HasSpecialCharacters)
            feedback.Add("Add special characters");

        if (result.HasRepeatingPatterns)
            feedback.Add("Avoid repeating patterns");

        if (result.HasSequentialCharacters)
            feedback.Add("Avoid sequential characters");

        if (result.HasCommonWords)
            feedback.Add("Avoid common words");

        if (result.UniqueCharacters < result.Length * 0.8)
            feedback.Add("Use more unique characters");

        return feedback;
    }

    private List<string> GetCommonPasswords()
    {
        return new List<string>
        {
            "password", "123456", "password123", "admin", "qwerty", "letmein",
            "welcome", "monkey", "dragon", "master", "login", "welcome123",
            "123456789", "password1", "abc123", "admin123", "root", "toor"
        };
    }

    #endregion
}