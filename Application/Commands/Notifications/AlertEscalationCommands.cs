using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Notifications;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Commands.Notifications;

/// <summary>
/// Command to create a new alert escalation rule
/// </summary>
public class CreateAlertEscalationCommand : IRequest<AlertEscalationDto>
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

    /// <summary>
    /// User creating the rule
    /// </summary>
    public int CreatedBy { get; set; }
}

/// <summary>
/// Command to update an existing alert escalation rule
/// </summary>
public class UpdateAlertEscalationCommand : IRequest<AlertEscalationDto>
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
    /// User updating the rule
    /// </summary>
    public int ModifiedBy { get; set; }
}

/// <summary>
/// Command to delete an alert escalation rule
/// </summary>
public class DeleteAlertEscalationCommand : IRequest<bool>
{
    /// <summary>
    /// Escalation rule ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User deleting the rule
    /// </summary>
    public int DeletedBy { get; set; }
}
