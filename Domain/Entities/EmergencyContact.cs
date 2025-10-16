using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents emergency contact information for a visitor
/// </summary>
public class EmergencyContact : SoftDeleteEntity
{
    /// <summary>
    /// Foreign key to the visitor
    /// </summary>
    [Required]
    public int VisitorId { get; set; }

    /// <summary>
    /// Emergency contact's first name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Emergency contact's last name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Relationship to the visitor
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Relationship { get; set; } = string.Empty;    /// <summary>
    /// Primary phone number
    /// </summary>
    [Required]
    public PhoneNumber PhoneNumber { get; set; } = null!;

    /// <summary>
    /// Alternate phone number
    /// </summary>
    public PhoneNumber? AlternatePhoneNumber { get; set; }

    /// <summary>
    /// Email address
    /// </summary>
    public Email? Email { get; set; }

    /// <summary>
    /// Address
    /// </summary>
    public Address? Address { get; set; }

    /// <summary>
    /// Priority order (1 = primary contact)
    /// </summary>
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Whether this is the primary emergency contact
    /// </summary>
    public bool IsPrimary { get; set; } = false;

    /// <summary>
    /// Additional notes about the contact
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }    /// <summary>
    /// Navigation property to the visitor
    /// </summary>
    public virtual Visitor Visitor { get; set; } = null!;

    /// <summary>
    /// Gets the contact's full name
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Gets a formatted display string for the contact
    /// </summary>
    public string DisplayInfo => $"{FullName} ({Relationship}) - {PhoneNumber.FormattedValue}";

    /// <summary>
    /// Validates the emergency contact information
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> ValidateContact()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(FirstName))
            errors.Add("Emergency contact first name is required.");

        if (string.IsNullOrWhiteSpace(LastName))
            errors.Add("Emergency contact last name is required.");

        if (string.IsNullOrWhiteSpace(Relationship))
            errors.Add("Relationship to visitor is required.");

        if (PhoneNumber == null)
            errors.Add("Primary phone number is required.");

        if (Priority < 1)
            errors.Add("Priority must be greater than zero.");

        return errors;
    }
}
