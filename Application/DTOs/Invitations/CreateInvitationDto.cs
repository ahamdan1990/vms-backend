using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace VisitorManagementSystem.Api.Application.DTOs.Invitations;

/// <summary>
/// DTO for creating a new invitation
/// </summary>
public class CreateInvitationDto : IValidatableObject
{
    /// <summary>
    /// Visitor ID this invitation is for (for single visitor invitations)
    /// </summary>
    public int? VisitorId { get; set; }

    /// <summary>
    /// Multiple visitor IDs for group invitations
    /// </summary>
    public List<int> VisitorIds { get; set; } = new List<int>();

    /// <summary>
    /// Visit purpose ID
    /// </summary>
    public int? VisitPurposeId { get; set; }

    /// <summary>
    /// Location ID
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Invitation type
    /// </summary>
    public InvitationType Type { get; set; } = InvitationType.Single;

    /// <summary>
    /// Subject/title of the invitation
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Message/description of the visit
    /// </summary>
    [MaxLength(1000)]
    public string? Message { get; set; }

    /// <summary>
    /// Scheduled start date and time
    /// </summary>
    [Required]
    public DateTime ScheduledStartTime { get; set; }

    /// <summary>
    /// Scheduled end date and time
    /// </summary>
    [Required]
    public DateTime ScheduledEndTime { get; set; }

    /// <summary>
    /// Expected number of visitors
    /// </summary>
    [Range(1, 100)]
    public int ExpectedVisitorCount { get; set; } = 1;

    /// <summary>
    /// Special instructions or requirements
    /// </summary>
    [MaxLength(500)]
    public string? SpecialInstructions { get; set; }

    /// <summary>
    /// Whether pre-approval is required
    /// </summary>
    public bool RequiresApproval { get; set; } = true;

    /// <summary>
    /// Whether escort is required
    /// </summary>
    public bool RequiresEscort { get; set; } = false;

    /// <summary>
    /// Whether badge printing is required
    /// </summary>
    public bool RequiresBadge { get; set; } = true;

    /// <summary>
    /// Whether parking is needed
    /// </summary>
    public bool NeedsParking { get; set; } = false;

    /// <summary>
    /// Parking instructions
    /// </summary>
    [MaxLength(200)]
    public string? ParkingInstructions { get; set; }

    /// <summary>
    /// Template ID to use (optional)
    /// </summary>
    public int? TemplateId { get; set; }

    /// <summary>
    /// Whether to submit immediately for approval
    /// </summary>
    public bool SubmitForApproval { get; set; } = false;

    /// <summary>
    /// Custom validation logic
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // Validate visitor selection based on invitation type
        if (Type == InvitationType.Single)
        {
            if (!VisitorId.HasValue || VisitorId.Value <= 0)
            {
                results.Add(new ValidationResult(
                    "VisitorId is required for single visitor invitations.",
                    new[] { nameof(VisitorId) }));
            }
        }
        else if (Type == InvitationType.Group)
        {
            if (VisitorIds == null || !VisitorIds.Any())
            {
                results.Add(new ValidationResult(
                    "At least one visitor ID is required for group invitations.",
                    new[] { nameof(VisitorIds) }));
            }
            else if (VisitorIds.Any(id => id <= 0))
            {
                results.Add(new ValidationResult(
                    "All visitor IDs must be valid positive numbers.",
                    new[] { nameof(VisitorIds) }));
            }
        }

        // Validate date/time logic
        if (ScheduledStartTime != default && ScheduledEndTime != default)
        {
            if (ScheduledEndTime <= ScheduledStartTime)
            {
                results.Add(new ValidationResult(
                    "End time must be after start time.",
                    new[] { nameof(ScheduledEndTime) }));
            }

            var duration = ScheduledEndTime - ScheduledStartTime;
            if (duration.TotalMinutes < 15)
            {
                results.Add(new ValidationResult(
                    "Visit duration must be at least 15 minutes.",
                    new[] { nameof(ScheduledEndTime) }));
            }

            if (duration.TotalHours > 24)
            {
                results.Add(new ValidationResult(
                    "Visit duration cannot exceed 24 hours.",
                    new[] { nameof(ScheduledEndTime) }));
            }
        }

        // Validate parking instructions if parking is needed
        if (NeedsParking && string.IsNullOrWhiteSpace(ParkingInstructions))
        {
            results.Add(new ValidationResult(
                "Parking instructions are required when parking is needed.",
                new[] { nameof(ParkingInstructions) }));
        }

        return results;
    }
}
