using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents an approval step in the invitation workflow
/// </summary>
public class InvitationApproval : AuditableEntity
{
    /// <summary>
    /// Invitation this approval belongs to
    /// </summary>
    [Required]
    public int InvitationId { get; set; }

    /// <summary>
    /// User responsible for this approval step
    /// </summary>
    [Required]
    public int ApproverId { get; set; }

    /// <summary>
    /// Approval step order (1, 2, 3, etc.)
    /// </summary>
    [Required]
    public int StepOrder { get; set; }

    /// <summary>
    /// Approval decision
    /// </summary>
    [Required]
    public ApprovalDecision Decision { get; set; } = ApprovalDecision.Pending;

    /// <summary>
    /// Date decision was made
    /// </summary>
    public DateTime? DecisionDate { get; set; }

    /// <summary>
    /// Comments from the approver
    /// </summary>
    [MaxLength(500)]
    public string? Comments { get; set; }

    /// <summary>
    /// Whether this approval step is required
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Escalation user if escalated
    /// </summary>
    public int? EscalatedToUserId { get; set; }

    /// <summary>
    /// Date escalated
    /// </summary>
    public DateTime? EscalatedOn { get; set; }

    /// <summary>
    /// Navigation property for the invitation
    /// </summary>
    public virtual Invitation Invitation { get; set; } = null!;

    /// <summary>
    /// Navigation property for the approver
    /// </summary>
    public virtual User Approver { get; set; } = null!;

    /// <summary>
    /// Navigation property for escalated user
    /// </summary>
    public virtual User? EscalatedToUser { get; set; }

    /// <summary>
    /// Checks if this approval step is pending
    /// </summary>
    public bool IsPending => Decision == ApprovalDecision.Pending;

    /// <summary>
    /// Checks if this approval step is completed
    /// </summary>
    public bool IsCompleted => Decision == ApprovalDecision.Approved || Decision == ApprovalDecision.Rejected;

    /// <summary>
    /// Approves this step
    /// </summary>
    /// <param name="comments">Approval comments</param>
    public void Approve(string? comments = null)
    {
        Decision = ApprovalDecision.Approved;
        DecisionDate = DateTime.UtcNow;
        Comments = comments?.Trim();
        UpdateModifiedOn();
    }

    /// <summary>
    /// Rejects this step
    /// </summary>
    /// <param name="comments">Rejection comments</param>
    public void Reject(string? comments = null)
    {
        Decision = ApprovalDecision.Rejected;
        DecisionDate = DateTime.UtcNow;
        Comments = comments?.Trim();
        UpdateModifiedOn();
    }

    /// <summary>
    /// Escalates this step to another user
    /// </summary>
    /// <param name="escalatedToUserId">User to escalate to</param>
    /// <param name="comments">Escalation comments</param>
    public void Escalate(int escalatedToUserId, string? comments = null)
    {
        Decision = ApprovalDecision.Escalated;
        DecisionDate = DateTime.UtcNow;
        EscalatedToUserId = escalatedToUserId;
        EscalatedOn = DateTime.UtcNow;
        Comments = comments?.Trim();
        UpdateModifiedOn();
    }
}
