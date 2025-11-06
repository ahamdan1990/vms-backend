using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents a visitor in the system
/// </summary>
public class Visitor : SoftDeleteEntity
{
    /// <summary>
    /// Visitor's first name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Visitor's last name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Visitor's email address
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
    /// Visitor's phone number
    /// </summary>
    public PhoneNumber? PhoneNumber { get; set; }

    /// <summary>
    /// Visitor's company name
    /// </summary>
    [MaxLength(100)]
    public string? Company { get; set; }

    /// <summary>
    /// Visitor's job title
    /// </summary>
    [MaxLength(100)]
    public string? JobTitle { get; set; }

    /// <summary>
    /// Visitor's address
    /// </summary>
    public Address? Address { get; set; }

    /// <summary>
    /// Date of birth for age verification
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Government ID number (passport, driver's license, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? GovernmentId { get; set; }

    /// <summary>
    /// Type of government ID
    /// </summary>
    [MaxLength(30)]
    public string? GovernmentIdType { get; set; }

    /// <summary>
    /// Visitor's nationality
    /// </summary>
    [MaxLength(50)]
    public string? Nationality { get; set; }

    /// <summary>
    /// Visitor's preferred language
    /// </summary>
    [MaxLength(10)]
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// Path to visitor's profile photo
    /// </summary>
    [MaxLength(500)]
    public string? ProfilePhotoPath { get; set; }

    /// <summary>
    /// Special dietary requirements or allergies
    /// </summary>
    [MaxLength(500)]
    public string? DietaryRequirements { get; set; }

    /// <summary>
    /// Special accessibility requirements
    /// </summary>
    [MaxLength(500)]
    public string? AccessibilityRequirements { get; set; }

    /// <summary>
    /// Security clearance level (if applicable)
    /// </summary>
    [MaxLength(50)]
    public string? SecurityClearance { get; set; }

    /// <summary>
    /// Whether visitor is a VIP
    /// </summary>
    public bool IsVip { get; set; } = false;

    /// <summary>
    /// Whether visitor is blacklisted
    /// </summary>
    public bool IsBlacklisted { get; set; } = false;

    /// <summary>
    /// Reason for blacklisting (if applicable)
    /// </summary>
    [MaxLength(500)]
    public string? BlacklistReason { get; set; }

    /// <summary>
    /// Date when blacklisted
    /// </summary>
    public DateTime? BlacklistedOn { get; set; }

    /// <summary>
    /// User who blacklisted the visitor
    /// </summary>
    public int? BlacklistedBy { get; set; }

    /// <summary>
    /// Number of times visitor has been to the facility
    /// </summary>
    public int VisitCount { get; set; } = 0;

    /// <summary>
    /// Date of last visit
    /// </summary>
    public DateTime? LastVisitDate { get; set; }

    /// <summary>
    /// Additional notes about the visitor
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// External system ID (for integration purposes)
    /// </summary>
    [MaxLength(100)]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Preferred location ID for visits
    /// </summary>
    public int? PreferredLocationId { get; set; }

    /// <summary>
    /// Default visit purpose ID
    /// </summary>
    public int? DefaultVisitPurposeId { get; set; }

    /// <summary>
    /// Visitor's preferred timezone
    /// </summary>
    [MaxLength(50)]
    public string? TimeZone { get; set; } = "Asia/Beirut";

    /// <summary>
    /// Navigation property for preferred location
    /// </summary>
    public virtual Location? PreferredLocation { get; set; }

    /// <summary>
    /// Navigation property for default visit purpose
    /// </summary>
    public virtual VisitPurpose? DefaultVisitPurpose { get; set; }

    /// <summary>
    /// Navigation property for the user who blacklisted this visitor
    /// </summary>
    public virtual User? BlacklistedByUser { get; set; }

    /// <summary>
    /// Navigation property for visitor documents
    /// </summary>
    public virtual ICollection<VisitorDocument> Documents { get; set; } = new List<VisitorDocument>();

    /// <summary>
    /// Navigation property for visitor notes
    /// </summary>
    public virtual ICollection<VisitorNote> VisitorNotes { get; set; } = new List<VisitorNote>();

    /// <summary>
    /// Navigation property for emergency contacts
    /// </summary>
    public virtual ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();

    /// <summary>
    /// Navigation property for visitor access records (users who can access this visitor)
    /// </summary>
    public virtual ICollection<VisitorAccess> VisitorAccesses { get; set; } = new List<VisitorAccess>();

    /// <summary>
    /// Gets the visitor's full name
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Gets the visitor's display name (full name or email if name is empty)
    /// </summary>
    public string DisplayName => !string.IsNullOrWhiteSpace(FullName) ? FullName : Email.Value;

    /// <summary>
    /// Gets the visitor's age based on date of birth
    /// </summary>
    public int? Age => DateOfBirth?.CalculateAge();

    /// <summary>
    /// Checks if the visitor is currently blacklisted
    /// </summary>
    /// <returns>True if visitor is blacklisted</returns>
    public bool IsCurrentlyBlacklisted()
    {
        return IsBlacklisted && BlacklistedOn.HasValue;
    }

    /// <summary>
    /// Blacklists the visitor
    /// </summary>
    /// <param name="reason">Reason for blacklisting</param>
    /// <param name="blacklistedBy">User performing the action</param>
    public void Blacklist(string reason, int blacklistedBy)
    {
        IsBlacklisted = true;
        BlacklistReason = reason;
        BlacklistedOn = DateTime.UtcNow;
        BlacklistedBy = blacklistedBy;
        UpdateModifiedBy(blacklistedBy);
    }

    /// <summary>
    /// Removes blacklist status from the visitor
    /// </summary>
    /// <param name="modifiedBy">User performing the action</param>
    public void RemoveBlacklist(int modifiedBy)
    {
        IsBlacklisted = false;
        BlacklistReason = null;
        BlacklistedOn = null;
        BlacklistedBy = null;
        UpdateModifiedBy(modifiedBy);
    }

    /// <summary>
    /// Marks visitor as VIP
    /// </summary>
    /// <param name="modifiedBy">User performing the action</param>
    public void MarkAsVip(int modifiedBy)
    {
        IsVip = true;
        UpdateModifiedBy(modifiedBy);
    }

    /// <summary>
    /// Removes VIP status from visitor
    /// </summary>
    /// <param name="modifiedBy">User performing the action</param>
    public void RemoveVipStatus(int modifiedBy)
    {
        IsVip = false;
        UpdateModifiedBy(modifiedBy);
    }

    /// <summary>
    /// Updates visitor's visit statistics
    /// </summary>
    public void UpdateVisitStatistics()
    {
        VisitCount++;
        LastVisitDate = DateTime.UtcNow;
        UpdateModifiedOn();
    }

    /// <summary>
    /// Validates the visitor's information
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> ValidateVisitor()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(FirstName))
            errors.Add("First name is required.");

        if (string.IsNullOrWhiteSpace(LastName))
            errors.Add("Last name is required.");

        if (Email == null)
            errors.Add("Email address is required.");

        if (DateOfBirth.HasValue && DateOfBirth.Value > DateTime.Now.AddYears(-16))
            errors.Add("Visitor must be at least 16 years old.");

        if (DateOfBirth.HasValue && DateOfBirth.Value < DateTime.Now.AddYears(-120))
            errors.Add("Invalid date of birth.");

        if (IsBlacklisted && string.IsNullOrWhiteSpace(BlacklistReason))
            errors.Add("Blacklist reason is required when visitor is blacklisted.");

        return errors;
    }

    /// <summary>
    /// Gets a masked version of sensitive information for display
    /// </summary>
    /// <returns>Visitor with masked sensitive data</returns>
    public VisitorDisplayInfo GetMaskedInfo()
    {
        return new VisitorDisplayInfo
        {
            Id = Id,
            FullName = FullName,
            Email = Email?.GetMasked() ?? string.Empty,
            Company = Company,
            PhoneNumber = PhoneNumber?.GetNationalFormat().Substring(0, Math.Min(3, PhoneNumber.GetNationalFormat().Length)) + "***",
            IsVip = IsVip,
            IsBlacklisted = IsBlacklisted,
            VisitCount = VisitCount,
            LastVisitDate = LastVisitDate
        };
    }

    /// <summary>
    /// Checks if visitor information is complete
    /// </summary>
    /// <returns>True if all required information is present</returns>
    public bool IsInformationComplete()
    {
        return !string.IsNullOrWhiteSpace(FirstName) &&
               !string.IsNullOrWhiteSpace(LastName) &&
               Email != null &&
               PhoneNumber != null;
    }

    /// <summary>
    /// Updates the normalized email when email changes
    /// </summary>
    public void UpdateNormalizedEmail()
    {
        if (Email != null)
        {
            NormalizedEmail = Email.Value.ToUpperInvariant();
        }
    }
}

/// <summary>
/// Visitor display information with sensitive data masked
/// </summary>
public class VisitorDisplayInfo
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsVip { get; set; }
    public bool IsBlacklisted { get; set; }
    public int VisitCount { get; set; }
    public DateTime? LastVisitDate { get; set; }
}

/// <summary>
/// Extension methods for DateTime to calculate age
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Calculates age based on birth date
    /// </summary>
    /// <param name="birthDate">Birth date</param>
    /// <returns>Age in years</returns>
    public static int CalculateAge(this DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        
        if (birthDate.Date > today.AddYears(-age))
            age--;
            
        return age;
    }
}