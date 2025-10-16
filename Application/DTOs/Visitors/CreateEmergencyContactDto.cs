using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Common;

namespace VisitorManagementSystem.Api.Application.DTOs.Visitors;

/// <summary>
/// DTO for creating a new emergency contact
/// </summary>
public class CreateEmergencyContactDto
{
    /// <summary>
    /// Contact first name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Contact last name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Relationship to visitor
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Relationship { get; set; } = string.Empty;

    /// <summary>
    /// Primary phone number
    /// </summary>
    [Required]
    [Phone]
    public string? PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Alternate phone number
    /// </summary>
    [Phone]
    public string? AlternatePhoneNumber { get; set; }

    /// <summary>
    /// Email address
    /// </summary>
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>
    /// Address
    /// </summary>
    public AddressDto? Address { get; set; }

    /// <summary>
    /// Priority order
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Whether this is the primary contact
    /// </summary>
    public bool IsPrimary { get; set; } = false;

    /// <summary>
    /// Additional notes
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}
