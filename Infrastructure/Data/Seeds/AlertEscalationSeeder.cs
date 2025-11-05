using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Seeds;

/// <summary>
/// Seeder for alert escalation rules
/// </summary>
public static class AlertEscalationSeeder
{
    /// <summary>
    /// Gets seed alert escalation rules for initial database setup
    /// </summary>
    /// <returns>List of seed alert escalation rules</returns>
    public static List<AlertEscalation> GetSeedAlertEscalations()
    {
        var escalations = new List<AlertEscalation>();

        // Emergency alerts - immediate escalation (5 minutes)
        escalations.AddRange(CreateEmergencyEscalations());
        
        // Critical alerts - fast escalation (15 minutes)
        escalations.AddRange(CreateCriticalEscalations());
        
        // High priority alerts - moderate escalation (30 minutes)
        escalations.AddRange(CreateHighPriorityEscalations());
        
        // Medium priority alerts - delayed escalation (60 minutes)
        escalations.AddRange(CreateMediumPriorityEscalations());
        
        // Low priority alerts - long escalation (120 minutes)
        escalations.AddRange(CreateLowPriorityEscalations());

        return escalations;
    }

    /// <summary>
    /// Creates emergency alert escalations (5 minutes)
    /// </summary>
    private static List<AlertEscalation> CreateEmergencyEscalations()
    {
        var escalations = new List<AlertEscalation>();

        // Security/Emergency alerts
        var emergencyAlertTypes = new[]
        {
            NotificationAlertType.BlacklistAlert,
            NotificationAlertType.EmergencyAlert,
            NotificationAlertType.UnknownFace
        };

        foreach (var alertType in emergencyAlertTypes)
        {
            // Immediate notification to administrators
            escalations.Add(new AlertEscalation
            {
                RuleName = $"Emergency {alertType} - Immediate Admin",
                AlertType = alertType,
                AlertPriority = AlertPriority.Emergency,
                EscalationDelayMinutes = 0, // Immediate
                Action = EscalationAction.EscalateToRole,
                EscalationTargetRole = UserRoles.Administrator,
                IsEnabled = true,
                RulePriority = 1,
                MaxAttempts = 3,
                CreatedBy = 1, // System admin
                CreatedOn = DateTime.UtcNow
            });

            // Email to administrators after 5 minutes if not acknowledged
            escalations.Add(new AlertEscalation
            {
                RuleName = $"Emergency {alertType} - Email Admin",
                AlertType = alertType,
                AlertPriority = AlertPriority.Emergency,
                EscalationDelayMinutes = 5,
                Action = EscalationAction.SendEmail,
                EscalationTargetRole = UserRoles.Administrator,
                IsEnabled = true,
                RulePriority = 2,
                MaxAttempts = 3,
                CreatedBy = 1,
                CreatedOn = DateTime.UtcNow
            });
        }

        return escalations;
    }

    /// <summary>
    /// Creates critical alert escalations (15 minutes)
    /// </summary>
    private static List<AlertEscalation> CreateCriticalEscalations()
    {
        var escalations = new List<AlertEscalation>();

        var criticalAlertTypes = new[]
        {
            NotificationAlertType.FRSystemOffline,
            NotificationAlertType.SystemAlert,
            NotificationAlertType.CapacityAlert
        };

        foreach (var alertType in criticalAlertTypes)
        {
            // Notify administrators immediately
            escalations.Add(new AlertEscalation
            {
                RuleName = $"Critical {alertType} - Admin Notification",
                AlertType = alertType,
                AlertPriority = AlertPriority.Critical,
                EscalationDelayMinutes = 0,
                Action = EscalationAction.EscalateToRole,
                EscalationTargetRole = UserRoles.Administrator,
                IsEnabled = true,
                RulePriority = 1,
                MaxAttempts = 3,
                CreatedBy = 1,
                CreatedOn = DateTime.UtcNow
            });

            // Email after 15 minutes
            escalations.Add(new AlertEscalation
            {
                RuleName = $"Critical {alertType} - Email Escalation",
                AlertType = alertType,
                AlertPriority = AlertPriority.Critical,
                EscalationDelayMinutes = 15,
                Action = EscalationAction.SendEmail,
                EscalationTargetRole = UserRoles.Administrator,
                IsEnabled = true,
                RulePriority = 2,
                MaxAttempts = 3,
                CreatedBy = 1,
                CreatedOn = DateTime.UtcNow
            });
        }

        return escalations;
    }

    /// <summary>
    /// Creates high priority alert escalations (30 minutes)
    /// </summary>
    private static List<AlertEscalation> CreateHighPriorityEscalations()
    {
        var escalations = new List<AlertEscalation>();

        var highPriorityAlertTypes = new[]
        {
            NotificationAlertType.VipArrival,
            NotificationAlertType.VisitorOverstay,
            NotificationAlertType.InvitationPendingApproval
        };

        foreach (var alertType in highPriorityAlertTypes)
        {
            // Notify operators first
            escalations.Add(new AlertEscalation
            {
                RuleName = $"High {alertType} - Operator Notification",
                AlertType = alertType,
                AlertPriority = AlertPriority.High,
                EscalationDelayMinutes = 0,
                Action = EscalationAction.EscalateToRole,
                EscalationTargetRole = UserRoles.Receptionist,
                IsEnabled = true,
                RulePriority = 1,
                MaxAttempts = 3,
                CreatedBy = 1,
                CreatedOn = DateTime.UtcNow
            });

            // Escalate to admin after 30 minutes
            escalations.Add(new AlertEscalation
            {
                RuleName = $"High {alertType} - Admin Escalation",
                AlertType = alertType,
                AlertPriority = AlertPriority.High,
                EscalationDelayMinutes = 30,
                Action = EscalationAction.EscalateToRole,
                EscalationTargetRole = UserRoles.Administrator,
                IsEnabled = true,
                RulePriority = 2,
                MaxAttempts = 3,
                CreatedBy = 1,
                CreatedOn = DateTime.UtcNow
            });
        }

        return escalations;
    }

    /// <summary>
    /// Creates medium priority alert escalations (60 minutes)
    /// </summary>
    private static List<AlertEscalation> CreateMediumPriorityEscalations()
    {
        var escalations = new List<AlertEscalation>();

        var mediumPriorityAlertTypes = new[]
        {
            NotificationAlertType.VisitorArrival,
            NotificationAlertType.VisitorCheckedIn,
            NotificationAlertType.VisitorCheckedOut,
            NotificationAlertType.BadgePrintingError
        };

        foreach (var alertType in mediumPriorityAlertTypes)
        {
            // Notify operators
            escalations.Add(new AlertEscalation
            {
                RuleName = $"Medium {alertType} - Operator Notification",
                AlertType = alertType,
                AlertPriority = AlertPriority.Medium,
                EscalationDelayMinutes = 0,
                Action = EscalationAction.EscalateToRole,
                EscalationTargetRole = UserRoles.Receptionist,
                IsEnabled = true,
                RulePriority = 1,
                MaxAttempts = 3,
                CreatedBy = 1,
                CreatedOn = DateTime.UtcNow
            });

            // Log as high priority after 60 minutes if not acknowledged
            escalations.Add(new AlertEscalation
            {
                RuleName = $"Medium {alertType} - High Priority Log",
                AlertType = alertType,
                AlertPriority = AlertPriority.Medium,
                EscalationDelayMinutes = 60,
                Action = EscalationAction.CreateHighPriorityAlert,
                EscalationTargetRole = UserRoles.Administrator,
                IsEnabled = true,
                RulePriority = 2,
                MaxAttempts = 3,
                CreatedBy = 1,
                CreatedOn = DateTime.UtcNow
            });
        }

        return escalations;
    }

    /// <summary>
    /// Creates low priority alert escalations (120 minutes)
    /// </summary>
    private static List<AlertEscalation> CreateLowPriorityEscalations()
    {
        var escalations = new List<AlertEscalation>();

        var lowPriorityAlertTypes = new[]
        {
            NotificationAlertType.InvitationApproved,
            NotificationAlertType.InvitationRejected,
            NotificationAlertType.ManualOverride,
            NotificationAlertType.Custom
        };

        foreach (var alertType in lowPriorityAlertTypes)
        {
            // Just log as critical event after 120 minutes if not acknowledged
            escalations.Add(new AlertEscalation
            {
                RuleName = $"Low {alertType} - Log Escalation",
                AlertType = alertType,
                AlertPriority = AlertPriority.Low,
                EscalationDelayMinutes = 120,
                Action = EscalationAction.LogCriticalEvent,
                TargetRole = null,
                IsEnabled = true,
                RulePriority = 1,
                MaxAttempts = 1,
                CreatedBy = 1,
                CreatedOn = DateTime.UtcNow
            });
        }

        return escalations;
    }
}
