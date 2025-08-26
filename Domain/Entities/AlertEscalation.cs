using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Defines escalation rules for unacknowledged alerts
/// </summary>
public class AlertEscalation : AuditableEntity
{
    /// <summary>
    /// Rule name/description
    /// </summary>
    [Required]
    [MaxLength(100)]
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
    /// Target role this rule applies to (null = all)
    /// </summary>
    [MaxLength(50)]
    public string? TargetRole { get; set; }

    /// <summary>
    /// Location this rule applies to (null = all)
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
    /// Target role for escalation (when Action = EscalateToRole)
    /// </summary>
    [MaxLength(50)]
    public string? EscalationTargetRole { get; set; }

    /// <summary>
    /// Target user for escalation (when Action = EscalateToUser)
    /// </summary>
    public int? EscalationTargetUserId { get; set; }

    /// <summary>
    /// Email addresses for escalation (when Action = SendEmail)
    /// </summary>
    [MaxLength(500)]
    public string? EscalationEmails { get; set; }

    /// <summary>
    /// Phone numbers for SMS (when Action = SendSMS)
    /// </summary>
    [MaxLength(200)]
    public string? EscalationPhones { get; set; }

    /// <summary>
    /// Maximum number of escalation attempts
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Whether this rule is currently active
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Rule priority (lower number = higher priority)
    /// </summary>
    public int RulePriority { get; set; } = 10;

    /// <summary>
    /// Additional escalation configuration (JSON)
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// Navigation properties
    /// </summary>
    public virtual Location? Location { get; set; }
    public virtual User? EscalationTargetUser { get; set; }

    /// <summary>
    /// Check if rule matches the alert
    /// </summary>
    public bool MatchesAlert(NotificationAlert alert)
    {
        if (!IsEnabled || !IsActive)
            return false;

        // Check alert type match
        if (AlertType != alert.Type)
            return false;

        // Check priority match
        if (AlertPriority != alert.Priority)
            return false;

        // Check role match (null means applies to all)
        if (!string.IsNullOrEmpty(TargetRole) && TargetRole != alert.TargetRole)
            return false;

        // Check location match (null means applies to all)
        if (LocationId.HasValue && LocationId.Value != alert.TargetLocationId)
            return false;

        return true;
    }
}


