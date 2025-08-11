using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.DTOs.Invitations;

/// <summary>
/// DTO for invitation approval
/// </summary>
public class InvitationApprovalDto
{
    /// <summary>
    /// Approval ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Invitation ID
    /// </summary>
    public int InvitationId { get; set; }

    /// <summary>
    /// Approver ID
    /// </summary>
    public int ApproverId { get; set; }

    /// <summary>
    /// Approver name
    /// </summary>
    public string? ApproverName { get; set; }

    /// <summary>
    /// Step order
    /// </summary>
    public int StepOrder { get; set; }

    /// <summary>
    /// Approval decision
    /// </summary>
    public ApprovalDecision Decision { get; set; }

    /// <summary>
    /// Decision date
    /// </summary>
    public DateTime? DecisionDate { get; set; }

    /// <summary>
    /// Comments
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// Is required
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Escalated to user ID
    /// </summary>
    public int? EscalatedToUserId { get; set; }

    /// <summary>
    /// Escalated to user name
    /// </summary>
    public string? EscalatedToUserName { get; set; }

    /// <summary>
    /// Escalation date
    /// </summary>
    public DateTime? EscalatedOn { get; set; }

    /// <summary>
    /// Whether approval is pending
    /// </summary>
    public bool IsPending { get; set; }

    /// <summary>
    /// Whether approval is completed
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedOn { get; set; }
}

/// <summary>
/// DTO for invitation event
/// </summary>
public class InvitationEventDto
{
    /// <summary>
    /// Event ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Invitation ID
    /// </summary>
    public int InvitationId { get; set; }

    /// <summary>
    /// Event type
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Event description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Triggered by user ID
    /// </summary>
    public int? TriggeredBy { get; set; }

    /// <summary>
    /// Triggered by user name
    /// </summary>
    public string? TriggeredByUserName { get; set; }

    /// <summary>
    /// Event data
    /// </summary>
    public string? EventData { get; set; }

    /// <summary>
    /// Event timestamp
    /// </summary>
    public DateTime EventTimestamp { get; set; }
}

/// <summary>
/// DTO for invitation template
/// </summary>
public class InvitationTemplateDto
{
    /// <summary>
    /// Template ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Template name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Template category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Subject template
    /// </summary>
    public string SubjectTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Message template
    /// </summary>
    public string? MessageTemplate { get; set; }

    /// <summary>
    /// Default visit purpose ID
    /// </summary>
    public int? DefaultVisitPurposeId { get; set; }

    /// <summary>
    /// Default visit purpose name
    /// </summary>
    public string? DefaultVisitPurposeName { get; set; }

    /// <summary>
    /// Default location ID
    /// </summary>
    public int? DefaultLocationId { get; set; }

    /// <summary>
    /// Default location name
    /// </summary>
    public string? DefaultLocationName { get; set; }

    /// <summary>
    /// Default duration in hours
    /// </summary>
    public double DefaultDurationHours { get; set; }

    /// <summary>
    /// Default requires approval
    /// </summary>
    public bool DefaultRequiresApproval { get; set; }

    /// <summary>
    /// Default requires escort
    /// </summary>
    public bool DefaultRequiresEscort { get; set; }

    /// <summary>
    /// Default requires badge
    /// </summary>
    public bool DefaultRequiresBadge { get; set; }

    /// <summary>
    /// Default special instructions
    /// </summary>
    public string? DefaultSpecialInstructions { get; set; }

    /// <summary>
    /// Is shared template
    /// </summary>
    public bool IsShared { get; set; }

    /// <summary>
    /// Is system template
    /// </summary>
    public bool IsSystemTemplate { get; set; }

    /// <summary>
    /// Usage count
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Last used date
    /// </summary>
    public DateTime? LastUsedOn { get; set; }

    /// <summary>
    /// Created by name
    /// </summary>
    public string? CreatedByName { get; set; }

    /// <summary>
    /// Modified by name
    /// </summary>
    public string? ModifiedByName { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Modified date
    /// </summary>
    public DateTime? ModifiedOn { get; set; }
}
