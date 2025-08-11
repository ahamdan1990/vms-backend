using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations;

/// <summary>
/// Command to update an existing invitation
/// </summary>
public class UpdateInvitationCommand : IRequest<InvitationDto>
{
    /// <summary>
    /// Invitation ID to update
    /// </summary>
    [Required]
    public int Id { get; set; }

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
    /// User modifying the invitation
    /// </summary>
    public int ModifiedBy { get; set; }
}
