using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents an invitation for a visitor to the facility
/// </summary>
public class Invitation : SoftDeleteEntity
{
    /// <summary>
    /// Unique invitation reference number
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string InvitationNumber { get; set; } = string.Empty;

    /// <summary>
    /// Visitor this invitation is for
    /// </summary>
    [Required]
    public int VisitorId { get; set; }

    /// <summary>
    /// Host (staff member) creating the invitation
    /// </summary>
    [Required]
    public int HostId { get; set; }

    /// <summary>
    /// Visit purpose
    /// </summary>
    public int? VisitPurposeId { get; set; }

    /// <summary>
    /// Location to visit
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Current invitation status
    /// </summary>
    [Required]
    public InvitationStatus Status { get; set; } = InvitationStatus.Draft;

    /// <summary>
    /// Invitation type
    /// </summary>
    [Required]
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
    /// Expected number of visitors (for group invitations)
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
    /// QR code for check-in
    /// </summary>
    [MaxLength(500)]
    public string? QrCode { get; set; }

    /// <summary>
    /// Date invitation was sent
    /// </summary>
    public DateTime? SentOn { get; set; }

    /// <summary>
    /// Date invitation was approved
    /// </summary>
    public DateTime? ApprovedOn { get; set; }

    /// <summary>
    /// Who approved the invitation
    /// </summary>
    public int? ApprovedBy { get; set; }

    /// <summary>
    /// Approval comments
    /// </summary>
    [MaxLength(500)]
    public string? ApprovalComments { get; set; }

    /// <summary>
    /// Date invitation was rejected
    /// </summary>
    public DateTime? RejectedOn { get; set; }

    /// <summary>
    /// Who rejected the invitation
    /// </summary>
    public int? RejectedBy { get; set; }

    /// <summary>
    /// Rejection reason
    /// </summary>
    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Actual check-in time
    /// </summary>
    public DateTime? CheckedInAt { get; set; }

    /// <summary>
    /// Actual check-out time
    /// </summary>
    public DateTime? CheckedOutAt { get; set; }

    /// <summary>
    /// External system ID for integration
    /// </summary>
    [MaxLength(100)]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Import batch ID if created via bulk import
    /// </summary>
    public int? ImportBatchId { get; set; }
    // Navigation Properties
    /// <summary>
    /// Navigation property for the visitor
    /// </summary>
    public virtual Visitor Visitor { get; set; } = null!;

    /// <summary>
    /// Navigation property for the host (staff member)
    /// </summary>
    public virtual User Host { get; set; } = null!;

    /// <summary>
    /// Navigation property for the visit purpose
    /// </summary>
    public virtual VisitPurpose? VisitPurpose { get; set; }

    /// <summary>
    /// Navigation property for the location
    /// </summary>
    public virtual Location? Location { get; set; }

    /// <summary>
    /// Navigation property for the user who approved
    /// </summary>
    public virtual User? ApprovedByUser { get; set; }

    /// <summary>
    /// Navigation property for the user who rejected
    /// </summary>
    public virtual User? RejectedByUser { get; set; }

    /// <summary>
    /// Navigation property for invitation approvals
    /// </summary>
    public virtual ICollection<InvitationApproval> Approvals { get; set; } = new List<InvitationApproval>();

    /// <summary>
    /// Navigation property for invitation events
    /// </summary>
    public virtual ICollection<InvitationEvent> Events { get; set; } = new List<InvitationEvent>();

    // Business Methods
    /// <summary>
    /// Checks if the invitation is currently active
    /// </summary>
    public new bool IsActive => Status == InvitationStatus.Active;

    /// <summary>
    /// Checks if the invitation is approved
    /// </summary>
    public bool IsApproved => Status == InvitationStatus.Approved || Status == InvitationStatus.Active || Status == InvitationStatus.Completed;

    /// <summary>
    /// Checks if the invitation can be modified
    /// </summary>
    public bool CanBeModified => Status == InvitationStatus.Draft || Status == InvitationStatus.Submitted;

    /// <summary>
    /// Checks if the invitation can be cancelled
    /// </summary>
    public bool CanBeCancelled => Status != InvitationStatus.Cancelled && 
                                  Status != InvitationStatus.Completed && 
                                  Status != InvitationStatus.Expired;

    /// <summary>
    /// Gets the visit duration in hours
    /// </summary>
    public double VisitDurationHours => (ScheduledEndTime - ScheduledStartTime).TotalHours;

    /// <summary>
    /// Checks if the invitation is expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ScheduledEndTime && 
                            Status != InvitationStatus.Completed && 
                            Status != InvitationStatus.Cancelled;
    /// <summary>
    /// Submits the invitation for approval
    /// </summary>
    /// <param name="submittedBy">User submitting the invitation</param>
    public void Submit(int submittedBy)
    {
        if (Status != InvitationStatus.Draft)
            throw new InvalidOperationException("Only draft invitations can be submitted.");

        Status = InvitationStatus.Submitted;
        SentOn = DateTime.UtcNow;
        UpdateModifiedBy(submittedBy);
    }

    /// <summary>
    /// Approves the invitation
    /// </summary>
    /// <param name="approvedBy">User approving the invitation</param>
    /// <param name="comments">Approval comments</param>
    /// <summary>
    /// Approves the invitation
    /// </summary>
    /// <param name="approvedBy">User approving the invitation</param>
    /// <param name="comments">Optional approval comments</param>
    public void Approve(int approvedBy, string? comments = null)
    {
        if (Status != InvitationStatus.Submitted &&
            Status != InvitationStatus.UnderReview &&
            Status != InvitationStatus.Rejected)
            throw new InvalidOperationException("Only submitted or under review invitations can be approved.");

        Status = InvitationStatus.Approved;
        ApprovedOn = DateTime.UtcNow;
        ApprovedBy = approvedBy;
        ApprovalComments = comments?.Trim();
        UpdateModifiedBy(approvedBy);
    }

    /// <summary>
    /// Checks if the invitation can be approved
    /// </summary>
    /// <returns>True if the invitation can be approved, false otherwise</returns>
    public bool CanBeApproved()
    {
        return Status == InvitationStatus.Submitted ||
               Status == InvitationStatus.UnderReview ||
               Status == InvitationStatus.Rejected;
    }

    /// <summary>
    /// Rejects the invitation
    /// </summary>
    /// <param name="rejectedBy">User rejecting the invitation</param>
    /// <param name="reason">Rejection reason</param>
    public void Reject(int rejectedBy, string reason)
    {
        //if (Status != InvitationStatus.Submitted && Status != InvitationStatus.UnderReview)
        //    throw new InvalidOperationException("Only submitted or under review invitations can be rejected.");

        Status = InvitationStatus.Rejected;
        RejectedOn = DateTime.UtcNow;
        RejectedBy = rejectedBy;
        RejectionReason = reason?.Trim();
        UpdateModifiedBy(rejectedBy);
    }

    /// <summary>
    /// Cancels the invitation
    /// </summary>
    /// <param name="cancelledBy">User cancelling the invitation</param>
    public void Cancel(int cancelledBy)
    {
        if (!CanBeCancelled)
            throw new InvalidOperationException("This invitation cannot be cancelled.");

        Status = InvitationStatus.Cancelled;
        UpdateModifiedBy(cancelledBy);
    }

    /// <summary>
    /// Marks the invitation as active (visitor checked in)
    /// </summary>
    /// <param name="checkedInBy">User processing the check-in</param>
    public void CheckIn(int checkedInBy)
    {
        if (Status != InvitationStatus.Approved)
            throw new InvalidOperationException("Only approved invitations can be checked in.");

        Status = InvitationStatus.Active;
        CheckedInAt = DateTime.UtcNow;
        UpdateModifiedBy(checkedInBy);
    }

    /// <summary>
    /// Marks the invitation as completed (visitor checked out)
    /// </summary>
    /// <param name="checkedOutBy">User processing the check-out</param>
    public void CheckOut(int checkedOutBy)
    {
        if (Status != InvitationStatus.Active)
            throw new InvalidOperationException("Only active invitations can be checked out.");

        Status = InvitationStatus.Completed;
        CheckedOutAt = DateTime.UtcNow;
        UpdateModifiedBy(checkedOutBy);
    }
    /// <summary>
    /// Validates the invitation data
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> ValidateInvitation()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Subject))
            errors.Add("Subject is required.");

        if (ScheduledStartTime >= ScheduledEndTime)
            errors.Add("Scheduled end time must be after start time.");

        if (ScheduledStartTime < DateTime.UtcNow.AddMinutes(-30))
            errors.Add("Scheduled start time cannot be in the past.");

        if (VisitDurationHours > 24)
            errors.Add("Visit duration cannot exceed 24 hours.");

        if (ExpectedVisitorCount <= 0)
            errors.Add("Expected visitor count must be greater than 0.");

        if (Type == InvitationType.Group && ExpectedVisitorCount == 1)
            errors.Add("Group invitations must have more than 1 expected visitor.");

        return errors;
    }

    /// <summary>
    /// Generates a unique invitation number
    /// </summary>
    /// <returns>Invitation number</returns>
    public static string GenerateInvitationNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(100, 999);
        return $"INV-{timestamp}-{random}";
    }

    /// <summary>
    /// Checks if the invitation conflicts with building capacity
    /// </summary>
    /// <param name="currentOccupancy">Current building occupancy</param>
    /// <param name="maxCapacity">Maximum building capacity</param>
    /// <returns>True if there's a capacity conflict</returns>
    public bool HasCapacityConflict(int currentOccupancy, int maxCapacity)
    {
        return (currentOccupancy + ExpectedVisitorCount) > maxCapacity;
    }

    /// <summary>
    /// Gets a summary of the invitation
    /// </summary>
    /// <returns>Invitation summary</returns>
    public string GetSummary()
    {
        return $"{Subject} - {Visitor?.FullName} hosted by {Host?.FullName} on {ScheduledStartTime:yyyy-MM-dd HH:mm}";
    }

    /// <summary>
    /// Updates the QR code for the invitation
    /// </summary>
    /// <param name="qrCodeData">QR code data</param>
    public void UpdateQrCode(string qrCodeData)
    {
        QrCode = qrCodeData;
        UpdateModifiedOn();
    }
}
