using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Command for creating a new visitor
/// </summary>
public class CreateVisitorCommand : IRequest<VisitorDto>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // Enhanced phone fields
    public string? PhoneNumber { get; set; }
    public string? PhoneCountryCode { get; set; }
    public string? PhoneType { get; set; }
    
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public AddressDto? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? GovernmentId { get; set; }
    public string? GovernmentIdType { get; set; }
    public string? Nationality { get; set; }
    public string Language { get; set; } = "en-US";
    public string? DietaryRequirements { get; set; }
    public string? AccessibilityRequirements { get; set; }
    public string? SecurityClearance { get; set; }
    public bool IsVip { get; set; } = false;
    public string? Notes { get; set; }
    public string? ExternalId { get; set; }
    
    // Visitor preferences
    public int? PreferredLocationId { get; set; }
    public int? DefaultVisitPurposeId { get; set; }
    public string? TimeZone { get; set; } = "Asia/Beirut";
    
    public List<CreateEmergencyContactDto> EmergencyContacts { get; set; } = new();
    public int CreatedBy { get; set; }

    #region Optional Invitation Creation

    /// <summary>
    /// Whether to create an invitation immediately after creating the visitor
    /// </summary>
    public bool CreateInvitation { get; set; } = false;

    /// <summary>
    /// Invitation subject/title (required if CreateInvitation is true)
    /// </summary>
    public string? InvitationSubject { get; set; }

    /// <summary>
    /// Invitation message/description
    /// </summary>
    public string? InvitationMessage { get; set; }

    /// <summary>
    /// Scheduled start date and time for the invitation
    /// </summary>
    public DateTime? InvitationScheduledStartTime { get; set; }

    /// <summary>
    /// Scheduled end date and time for the invitation
    /// </summary>
    public DateTime? InvitationScheduledEndTime { get; set; }

    /// <summary>
    /// Location ID for the invitation (will use PreferredLocationId if not specified)
    /// </summary>
    public int? InvitationLocationId { get; set; }

    /// <summary>
    /// Visit purpose ID for the invitation (will use DefaultVisitPurposeId if not specified)
    /// </summary>
    public int? InvitationVisitPurposeId { get; set; }

    /// <summary>
    /// Expected number of visitors for the invitation
    /// </summary>
    public int InvitationExpectedVisitorCount { get; set; } = 1;

    /// <summary>
    /// Special instructions for the invitation
    /// </summary>
    public string? InvitationSpecialInstructions { get; set; }

    /// <summary>
    /// Whether the invitation requires approval
    /// </summary>
    public bool InvitationRequiresApproval { get; set; } = true;

    /// <summary>
    /// Whether the invitation requires escort
    /// </summary>
    public bool InvitationRequiresEscort { get; set; } = false;

    /// <summary>
    /// Whether the invitation requires badge
    /// </summary>
    public bool InvitationRequiresBadge { get; set; } = true;

    /// <summary>
    /// Whether parking is needed for the invitation
    /// </summary>
    public bool InvitationNeedsParking { get; set; } = false;

    /// <summary>
    /// Parking instructions for the invitation
    /// </summary>
    public string? InvitationParkingInstructions { get; set; }

    /// <summary>
    /// Whether to submit the invitation for approval immediately
    /// </summary>
    public bool InvitationSubmitForApproval { get; set; } = false;

    #endregion
}
