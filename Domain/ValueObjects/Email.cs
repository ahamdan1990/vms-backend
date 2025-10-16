using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace VisitorManagementSystem.Api.Domain.ValueObjects;

/// <summary>
/// Value object representing an email address with validation
/// </summary>
public class Email : IEquatable<Email>
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// The email address value
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Value { get; private set; }

    /// <summary>
    /// The domain part of the email address
    /// </summary>
    public string Domain => Value.Split('@').LastOrDefault() ?? string.Empty;

    /// <summary>
    /// The local part of the email address (before @)
    /// </summary>
    public string LocalPart => Value.Split('@').FirstOrDefault() ?? string.Empty;

    /// <summary>
    /// Private constructor for Entity Framework
    /// </summary>
    private Email()
    {
        Value = string.Empty;
    }

    /// <summary>
    /// Creates a new Email value object
    /// </summary>
    /// <param name="value">The email address</param>
    /// <exception cref="ArgumentException">Thrown when email is invalid</exception>
    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email address cannot be null or empty.", nameof(value));

        value = value.Trim().ToLowerInvariant();

        if (!IsValidEmail(value))
            throw new ArgumentException($"Invalid email address: {value}", nameof(value));

        if (value.Length > 256)
            throw new ArgumentException("Email address cannot exceed 256 characters.", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Validates an email address format
    /// </summary>
    /// <param name="email">Email address to validate</param>
    /// <returns>True if the email format is valid</returns>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            // Basic regex validation
            if (!EmailRegex.IsMatch(email))
                return false;

            // Additional checks
            var parts = email.Split('@');
            if (parts.Length != 2)
                return false;

            var localPart = parts[0];
            var domainPart = parts[1];

            // Local part validation
            if (localPart.Length == 0 || localPart.Length > 64)
                return false;

            if (localPart.StartsWith('.') || localPart.EndsWith('.'))
                return false;

            if (localPart.Contains(".."))
                return false;

            // Domain part validation
            if (domainPart.Length == 0 || domainPart.Length > 253)
                return false;

            if (domainPart.StartsWith('.') || domainPart.EndsWith('.'))
                return false;

            if (domainPart.Contains(".."))
                return false;

            // Domain must have at least one dot
            if (!domainPart.Contains('.'))
                return false;

            // Check for valid characters in domain
            if (!Regex.IsMatch(domainPart, @"^[a-zA-Z0-9.-]+$"))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the email domain is from a common provider
    /// </summary>
    /// <returns>True if from a common email provider</returns>
    public bool IsCommonProvider()
    {
        var commonDomains = new[]
        {
            "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "live.com",
            "icloud.com", "aol.com", "protonmail.com", "mail.com", "zoho.com"
        };

        return commonDomains.Contains(Domain, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the email appears to be a business email
    /// </summary>
    /// <returns>True if appears to be a business email</returns>
    public bool IsBusinessEmail()
    {
        return !IsCommonProvider() && !IsDisposableEmail();
    }

    /// <summary>
    /// Checks if the email is from a known disposable email provider
    /// </summary>
    /// <returns>True if from a disposable email provider</returns>
    public bool IsDisposableEmail()
    {
        var disposableDomains = new[]
        {
            "10minutemail.com", "tempmail.org", "guerrillamail.com", "mailinator.com",
            "throwaway.email", "temp-mail.org", "fake-mail.org", "yopmail.com"
        };

        return disposableDomains.Contains(Domain, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the normalized version of the email (lowercase, trimmed)
    /// </summary>
    /// <returns>Normalized email address</returns>
    public string GetNormalized()
    {
        return Value.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Creates a masked version of the email for display purposes
    /// </summary>
    /// <returns>Masked email address (e.g., j***@example.com)</returns>
    public string GetMasked()
    {
        if (LocalPart.Length <= 2)
            return Value;

        var maskedLocal = LocalPart[0] + new string('*', LocalPart.Length - 2) + LocalPart[^1];
        return $"{maskedLocal}@{Domain}";
    }

    /// <summary>
    /// Gets the display name part of the email (local part)
    /// </summary>
    /// <returns>Display name from email</returns>
    public string GetDisplayName()
    {
        return LocalPart.Split('.', '+')[0];
    }

    /// <summary>
    /// Checks if this email matches another email (case-insensitive)
    /// </summary>
    /// <param name="other">Other email to compare</param>
    /// <returns>True if emails match</returns>
    public bool Matches(string other)
    {
        if (string.IsNullOrWhiteSpace(other))
            return false;

        return string.Equals(Value, other.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
    }

    #region Equality and Operators

    public bool Equals(Email? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is Email other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }

    public static bool operator ==(Email? left, Email? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Email? left, Email? right)
    {
        return !Equals(left, right);
    }

    public static implicit operator string(Email email)
    {
        return email.Value;
    }

    public static explicit operator Email(string email)
    {
        return new Email(email);
    }

    #endregion

    public override string ToString()
    {
        return Value;
    }
}