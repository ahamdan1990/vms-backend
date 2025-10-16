using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Common;

namespace VisitorManagementSystem.Api.Application.DTOs.Visitors;

/// <summary>
/// DTO for updating an existing visitor
/// </summary>
public class UpdateVisitorDto
{
    /// <summary>
    /// Visitor first name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Visitor last name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Visitor email address
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Visitor phone number
    /// </summary>
    [Phone]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Phone country code
    /// </summary>
    [MaxLength(4)]
    public string? PhoneCountryCode { get; set; }

    /// <summary>
    /// Phone type (Mobile, Landline, etc.)
    /// </summary>
    [MaxLength(20)]
    public string? PhoneType { get; set; }    /// <summary>
    /// Company name
    /// </summary>
    [MaxLength(100)]
    public string? Company { get; set; }

    /// <summary>
    /// Job title
    /// </summary>
    [MaxLength(100)]
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
    /// Government ID number
    /// </summary>
    [MaxLength(50)]
    public string? GovernmentId { get; set; }

    /// <summary>
    /// Type of government ID
    /// </summary>
    [MaxLength(30)]
    public string? GovernmentIdType { get; set; }

    /// <summary>
    /// Nationality
    /// </summary>
    [MaxLength(50)]
    public string? Nationality { get; set; }    /// <summary>
    /// Preferred language
    /// </summary>
    [MaxLength(10)]
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// Dietary requirements
    /// </summary>
    [MaxLength(500)]
    public string? DietaryRequirements { get; set; }

    /// <summary>
    /// Accessibility requirements
    /// </summary>
    [MaxLength(500)]
    public string? AccessibilityRequirements { get; set; }

    /// <summary>
    /// Security clearance level
    /// </summary>
    [MaxLength(50)]
    public string? SecurityClearance { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// External system ID
    /// </summary>
    [MaxLength(100)]
    public string? ExternalId { get; set; }
}
