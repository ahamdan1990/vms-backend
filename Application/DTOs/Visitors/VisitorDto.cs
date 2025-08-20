using VisitorManagementSystem.Api.Application.DTOs.Common;

namespace VisitorManagementSystem.Api.Application.DTOs.Visitors;

/// <summary>
/// Visitor data transfer object
/// </summary>
public class VisitorDto
{
    /// <summary>
    /// Visitor ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Visitor first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Visitor last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Visitor full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Visitor email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Visitor phone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Phone country code
    /// </summary>
    public string? PhoneCountryCode { get; set; }

    /// <summary>
    /// Phone type (Mobile, Landline, etc.)
    /// </summary>
    public string? PhoneType { get; set; }

    /// <summary>
    /// Company name
    /// </summary>
    public string? Company { get; set; }    /// <summary>
    /// Job title
    /// </summary>
    public string? JobTitle { get; set; }

    /// <summary>
    /// Address information
    /// </summary>
    public AddressDto? Address { get; set; }

    /// <summary>
    /// Date of birth
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Age calculated from date of birth
    /// </summary>
    public int? Age { get; set; }

    /// <summary>
    /// Government ID number
    /// </summary>
    public string? GovernmentId { get; set; }

    /// <summary>
    /// Type of government ID
    /// </summary>
    public string? GovernmentIdType { get; set; }

    /// <summary>
    /// Nationality
    /// </summary>
    public string? Nationality { get; set; }

    /// <summary>
    /// Preferred language
    /// </summary>
    public string Language { get; set; } = "en-US";    /// <summary>
    /// Profile photo URL
    /// </summary>
    public string? ProfilePhotoUrl { get; set; }

    /// <summary>
    /// Dietary requirements
    /// </summary>
    public string? DietaryRequirements { get; set; }

    /// <summary>
    /// Accessibility requirements
    /// </summary>
    public string? AccessibilityRequirements { get; set; }

    /// <summary>
    /// Security clearance level
    /// </summary>
    public string? SecurityClearance { get; set; }

    /// <summary>
    /// Whether visitor is VIP
    /// </summary>
    public bool IsVip { get; set; }

    /// <summary>
    /// Whether visitor is blacklisted
    /// </summary>
    public bool IsBlacklisted { get; set; }

    /// <summary>
    /// Blacklist reason
    /// </summary>
    public string? BlacklistReason { get; set; }

    /// <summary>
    /// Date when blacklisted
    /// </summary>
    public DateTime? BlacklistedOn { get; set; }    /// <summary>
    /// Number of visits
    /// </summary>
    public int VisitCount { get; set; }

    /// <summary>
    /// Date of last visit
    /// </summary>
    public DateTime? LastVisitDate { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// External system ID
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>
    /// Whether visitor is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Emergency contacts
    /// </summary>
    public List<EmergencyContactDto> EmergencyContacts { get; set; } = new();

    /// <summary>
    /// Documents count
    /// </summary>
    public int DocumentsCount { get; set; }

    /// <summary>
    /// Notes count
    /// </summary>
    public int NotesCount { get; set; }
    public object CreatedByName { get; internal set; }
    public object ModifiedByName { get; internal set; }
    public object BlacklistedByName { get; internal set; }
}
