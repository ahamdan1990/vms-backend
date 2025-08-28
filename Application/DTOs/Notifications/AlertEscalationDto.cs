using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.DTOs.Notifications;

/// <summary>
/// Data transfer object for alert escalation rules
/// </summary>
public class AlertEscalationDto
{
    /// <summary>
    /// Escalation rule ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Rule name
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Alert type this rule applies to
    /// </summary>
    public NotificationAlertType AlertType { get; set; }

    /// <summary>
    /// Alert priority this rule applies to
    /// </summary>
    public AlertPriority AlertPriority { get; set; }

    /// <summary>
    /// Target role this rule applies to
    /// </summary>
    public string? TargetRole { get; set; }

    /// <summary>
    /// Location ID this rule applies to
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Location name
    /// </summary>
    public string? LocationName { get; set; }

    /// <summary>
    /// Minutes to wait before escalation
    /// </summary>
    public int EscalationDelayMinutes { get; set; }

    /// <summary>
    /// Escalation action to take
    /// </summary>
    public EscalationAction Action { get; set; }

    /// <summary>
    /// Target role for escalation
    /// </summary>
    public string? EscalationTargetRole { get; set; }

    /// <summary>
    /// Target user ID for escalation
    /// </summary>
    public int? EscalationTargetUserId { get; set; }

    /// <summary>
    /// Target user name for escalation
    /// </summary>
    public string? EscalationTargetUserName { get; set; }

    /// <summary>
    /// Email addresses for escalation
    /// </summary>
    public string? EscalationEmails { get; set; }

    /// <summary>
    /// Phone numbers for SMS escalation
    /// </summary>
    public string? EscalationPhones { get; set; }

    /// <summary>
    /// Maximum escalation attempts
    /// </summary>
    public int MaxAttempts { get; set; }

    /// <summary>
    /// Is rule enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Rule priority (execution order)
    /// </summary>
    public int RulePriority { get; set; }

    /// <summary>
    /// Additional configuration data
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Modified date
    /// </summary>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>
    /// Is active
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Data transfer object for creating/updating alert escalation rules
/// </summary>
public class CreateUpdateAlertEscalationDto
{
    /// <summary>
    /// Rule name
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Alert type this rule applies to
    /// </summary>
    public NotificationAlertType AlertType { get; set; }

    /// <summary>
    /// Alert priority this rule applies to
    /// </summary>
    public AlertPriority AlertPriority { get; set; }

    /// <summary>
    /// Target role this rule applies to
    /// </summary>
    public string? TargetRole { get; set; }

    /// <summary>
    /// Location ID this rule applies to
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Minutes to wait before escalation
    /// </summary>
    public int EscalationDelayMinutes { get; set; } = 5;

    /// <summary>
    /// Escalation action to take
    /// </summary>
    public EscalationAction Action { get; set; }

    /// <summary>
    /// Target role for escalation
    /// </summary>
    public string? EscalationTargetRole { get; set; }

    /// <summary>
    /// Target user ID for escalation
    /// </summary>
    public int? EscalationTargetUserId { get; set; }

    /// <summary>
    /// Email addresses for escalation
    /// </summary>
    public string? EscalationEmails { get; set; }

    /// <summary>
    /// Phone numbers for SMS escalation
    /// </summary>
    public string? EscalationPhones { get; set; }

    /// <summary>
    /// Maximum escalation attempts
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Is rule enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Rule priority (execution order)
    /// </summary>
    public int RulePriority { get; set; } = 10;

    /// <summary>
    /// Additional configuration data
    /// </summary>
    public string? Configuration { get; set; }
}

/// <summary>
/// Data transfer object for acknowledging notifications
/// </summary>
public class AcknowledgeNotificationDto
{
    /// <summary>
    /// Notification ID to acknowledge
    /// </summary>
    public int NotificationId { get; set; }

    /// <summary>
    /// Optional acknowledgment notes
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Data transfer object for notification statistics
/// </summary>
public class NotificationStatsDto
{
    /// <summary>
    /// Total unacknowledged notifications
    /// </summary>
    public int TotalUnacknowledged { get; set; }

    /// <summary>
    /// Critical unacknowledged notifications
    /// </summary>
    public int CriticalUnacknowledged { get; set; }

    /// <summary>
    /// High priority unacknowledged notifications
    /// </summary>
    public int HighUnacknowledged { get; set; }

    /// <summary>
    /// Notifications in last 24 hours
    /// </summary>
    public int Last24Hours { get; set; }

    /// <summary>
    /// Most common alert type
    /// </summary>
    public string? MostCommonType { get; set; }

    /// <summary>
    /// Alert type statistics
    /// </summary>
    public Dictionary<string, int> AlertTypeStats { get; set; } = new();

    /// <summary>
    /// Priority statistics
    /// </summary>
    public Dictionary<string, int> PriorityStats { get; set; } = new();
}
