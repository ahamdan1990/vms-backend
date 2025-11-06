using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.DTOs.Locations;
using VisitorManagementSystem.Api.Application.DTOs.VisitPurposes;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.DTOs.Invitations;

/// <summary>
/// Invitation data transfer object
/// </summary>
public class InvitationDto
{
    /// <summary>
    /// Invitation ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique invitation reference number
    /// </summary>
    public string InvitationNumber { get; set; } = string.Empty;

    /// <summary>
    /// Visitor ID
    /// </summary>
    public int VisitorId { get; set; }

    /// <summary>
    /// Host ID
    /// </summary>
    public int HostId { get; set; }

    /// <summary>
    /// Visit purpose ID
    /// </summary>
    public int? VisitPurposeId { get; set; }

    /// <summary>
    /// Location ID
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Time slot ID for calendar booking
    /// </summary>
    public int? TimeSlotId { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public InvitationStatus Status { get; set; }

    /// <summary>
    /// Invitation type
    /// </summary>
    public InvitationType Type { get; set; }

    /// <summary>
    /// Subject
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Scheduled start time
    /// </summary>
    public DateTime ScheduledStartTime { get; set; }

    /// <summary>
    /// Scheduled end time
    /// </summary>
    public DateTime ScheduledEndTime { get; set; }

    /// <summary>
    /// Expected visitor count
    /// </summary>
    public int ExpectedVisitorCount { get; set; }

    /// <summary>
    /// Special instructions
    /// </summary>
    public string? SpecialInstructions { get; set; }

    /// <summary>
    /// Requires approval
    /// </summary>
    public bool RequiresApproval { get; set; }

    /// <summary>
    /// Requires escort
    /// </summary>
    public bool RequiresEscort { get; set; }

    /// <summary>
    /// Requires badge
    /// </summary>
    public bool RequiresBadge { get; set; }

    /// <summary>
    /// Needs parking
    /// </summary>
    public bool NeedsParking { get; set; }

    /// <summary>
    /// Parking instructions
    /// </summary>
    public string? ParkingInstructions { get; set; }

    /// <summary>
    /// QR code
    /// </summary>
    public string? QrCode { get; set; }

    /// <summary>
    /// Sent date
    /// </summary>
    public DateTime? SentOn { get; set; }

    /// <summary>
    /// Approved date
    /// </summary>
    public DateTime? ApprovedOn { get; set; }

    /// <summary>
    /// Approved by user ID
    /// </summary>
    public int? ApprovedBy { get; set; }

    /// <summary>
    /// Approval comments
    /// </summary>
    public string? ApprovalComments { get; set; }

    /// <summary>
    /// Rejected date
    /// </summary>
    public DateTime? RejectedOn { get; set; }

    /// <summary>
    /// Rejected by user ID
    /// </summary>
    public int? RejectedBy { get; set; }

    /// <summary>
    /// Rejection reason
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Check-in time
    /// </summary>
    public DateTime? CheckedInAt { get; set; }

    /// <summary>
    /// Check-out time
    /// </summary>
    public DateTime? CheckedOutAt { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Modified date
    /// </summary>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>
    /// Visitor information
    /// </summary>
    public VisitorDto? Visitor { get; set; }

    /// <summary>
    /// Host information
    /// </summary>
    public UserDto? Host { get; set; }

    /// <summary>
    /// Visit purpose information
    /// </summary>
    public VisitPurposeDto? VisitPurpose { get; set; }

    /// <summary>
    /// Location information
    /// </summary>
    public LocationDto? Location { get; set; }

    /// <summary>
    /// Visit duration in hours
    /// </summary>
    public double VisitDurationHours { get; set; }

    /// <summary>
    /// Whether invitation is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether invitation is approved
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Whether invitation can be modified
    /// </summary>
    public bool CanBeModified { get; set; }

    /// <summary>
    /// Whether invitation can be cancelled
    /// </summary>
    public bool CanBeCancelled { get; set; }

    /// <summary>
    /// Whether invitation is expired
    /// </summary>
    public bool IsExpired { get; set; }
}
