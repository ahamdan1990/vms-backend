using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Immutable record of each time an escalation rule was executed against a specific alert.
/// Used to enforce MaxAttempts — once attempts reach the rule's limit, the rule will never
/// fire again for that alert, regardless of how many dispatcher cycles pass.
/// </summary>
public class AlertEscalationLog : BaseEntity
{
    public int NotificationAlertId { get; set; }
    public int AlertEscalationId { get; set; }
    public int AttemptNumber { get; set; }
    public EscalationAction Action { get; set; }

    /// <summary>
    /// Snapshot of where this escalation was sent (email addresses or role name).
    /// </summary>
    public string? TargetInfo { get; set; }

    public NotificationAlert NotificationAlert { get; set; } = null!;
    public AlertEscalation AlertEscalation { get; set; } = null!;
}
