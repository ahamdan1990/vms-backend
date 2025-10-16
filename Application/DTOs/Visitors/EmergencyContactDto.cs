using VisitorManagementSystem.Api.Application.DTOs.Common;

namespace VisitorManagementSystem.Api.Application.DTOs.Visitors;

/// <summary>
/// Emergency contact data transfer object
/// </summary>
public class EmergencyContactDto
{
    /// <summary>
    /// Contact ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Visitor ID
    /// </summary>
    public int VisitorId { get; set; }

    /// <summary>
    /// Contact first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Contact last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Contact full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Relationship to visitor
    /// </summary>
    public string Relationship { get; set; } = string.Empty;

    /// <summary>
    /// Primary phone number
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Alternate phone number
    /// </summary>
    public string? AlternatePhoneNumber { get; set; }

    /// <summary>
    /// Email address
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Address
    /// </summary>
    public AddressDto? Address { get; set; }

    /// <summary>
    /// Priority order
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Whether this is the primary contact
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>
    /// Whether contact is active
    /// </summary>
    public bool IsActive { get; set; }
}
