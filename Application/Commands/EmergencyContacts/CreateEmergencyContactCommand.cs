using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Commands.EmergencyContacts;

/// <summary>
/// Command to create a new emergency contact
/// </summary>
public class CreateEmergencyContactCommand : IRequest<EmergencyContactDto>
{
    /// <summary>
    /// Visitor ID this contact belongs to
    /// </summary>
    [Required]
    public int VisitorId { get; set; }

    /// <summary>
    /// First name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
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
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Alternate phone number
    /// </summary>
    [MaxLength(20)]
    public string? AlternatePhoneNumber { get; set; }

    /// <summary>
    /// Email address
    /// </summary>
    [MaxLength(256)]
    public string? Email { get; set; }

    /// <summary>
    /// Address information
    /// </summary>
    public AddressDto? Address { get; set; }

    /// <summary>
    /// Contact priority (1 = highest)
    /// </summary>
    [Range(1, 10)]
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

    /// <summary>
    /// User creating the contact
    /// </summary>
    public int CreatedBy { get; set; }
}
