using System.ComponentModel.DataAnnotations;
﻿using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.DTOs.Visitors;

/// <summary>
/// DTO for creating a new visitor
/// </summary>
public class CreateVisitorDto : IValidatableObject
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
    public string? PhoneType { get; set; }

    /// <summary>
    /// Company name
    /// </summary>
    [MaxLength(100)]
    public string? Company { get; set; }    /// <summary>
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
    public string? Nationality { get; set; }

    /// <summary>
    /// Preferred language
    /// </summary>
    [MaxLength(10)]
    public string Language { get; set; } = "en-US";    /// <summary>
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
    /// Whether visitor is VIP
    /// </summary>
    public bool IsVip { get; set; }

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

    /// <summary>
    /// Preferred location ID for visits
    /// </summary>
    public int? PreferredLocationId { get; set; }

    /// <summary>
    /// Default visit purpose ID
    /// </summary>
    public int? DefaultVisitPurposeId { get; set; }

    /// <summary>
    /// Preferred timezone
    /// </summary>
    [MaxLength(50)]
    public string? TimeZone { get; set; } = "Asia/Beirut";

    /// <summary>
    /// Emergency contacts
    /// </summary>
    public List<CreateEmergencyContactDto> EmergencyContacts { get; set; } = new();

    #region Optional Invitation Creation

    /// <summary>
    /// Whether to create an invitation immediately after creating the visitor
    /// </summary>
    public bool CreateInvitation { get; set; } = false;

    /// <summary>
    /// Invitation subject/title (required if CreateInvitation is true)
    /// </summary>
    [MaxLength(200)]
    public string? InvitationSubject { get; set; }

    /// <summary>
    /// Invitation message/description
    /// </summary>
    [MaxLength(1000)]
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
    [Range(1, 100)]
    public int InvitationExpectedVisitorCount { get; set; } = 1;

    /// <summary>
    /// Special instructions for the invitation
    /// </summary>
    [MaxLength(500)]
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
    [MaxLength(200)]
    public string? InvitationParkingInstructions { get; set; }

    /// <summary>
    /// Whether to submit the invitation for approval immediately
    /// </summary>
    public bool InvitationSubmitForApproval { get; set; } = false;

    #endregion

    /// <summary>
    /// Custom validation logic for invitation data
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (CreateInvitation)
        {
            // Validate required invitation fields
            if (string.IsNullOrWhiteSpace(InvitationSubject))
            {
                results.Add(new ValidationResult(
                    "Invitation subject is required when creating an invitation.",
                    new[] { nameof(InvitationSubject) }));
            }

            if (!InvitationScheduledStartTime.HasValue)
            {
                results.Add(new ValidationResult(
                    "Invitation start time is required when creating an invitation.",
                    new[] { nameof(InvitationScheduledStartTime) }));
            }

            if (!InvitationScheduledEndTime.HasValue)
            {
                results.Add(new ValidationResult(
                    "Invitation end time is required when creating an invitation.",
                    new[] { nameof(InvitationScheduledEndTime) }));
            }

            // Validate date/time logic
            if (InvitationScheduledStartTime.HasValue && InvitationScheduledEndTime.HasValue)
            {
                if (InvitationScheduledEndTime <= InvitationScheduledStartTime)
                {
                    results.Add(new ValidationResult(
                        "Invitation end time must be after start time.",
                        new[] { nameof(InvitationScheduledEndTime) }));
                }

                var duration = InvitationScheduledEndTime.Value - InvitationScheduledStartTime.Value;
                if (duration.TotalMinutes < 15)
                {
                    results.Add(new ValidationResult(
                        "Invitation duration must be at least 15 minutes.",
                        new[] { nameof(InvitationScheduledEndTime) }));
                }

                if (duration.TotalHours > 24)
                {
                    results.Add(new ValidationResult(
                        "Invitation duration cannot exceed 24 hours.",
                        new[] { nameof(InvitationScheduledEndTime) }));
                }
            }

            // Validate parking instructions if parking is needed
            if (InvitationNeedsParking && string.IsNullOrWhiteSpace(InvitationParkingInstructions))
            {
                results.Add(new ValidationResult(
                    "Parking instructions are required when parking is needed for the invitation.",
                    new[] { nameof(InvitationParkingInstructions) }));
            }
        }

        return results;
    }
}
