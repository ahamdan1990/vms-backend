using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Repositories.Notifications;

/// <summary>
/// Repository implementation for alert escalation rules
/// </summary>
public class AlertEscalationRepository : BaseRepository<AlertEscalation>, IAlertEscalationRepository
{
    public AlertEscalationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AlertEscalation>> GetApplicableRulesAsync(NotificationAlert alert, 
        CancellationToken cancellationToken = default)
    {
        // Get all active escalation rules and filter in memory for complex matching logic
        var allActiveRules = await _context.AlertEscalations
            .Where(rule => rule.IsEnabled && rule.IsActive)
            .Include(rule => rule.Location)
            .Include(rule => rule.EscalationTargetUser)
            .OrderBy(rule => rule.RulePriority)
            .ToListAsync(cancellationToken);

        // Filter rules that match the alert
        return allActiveRules.Where(rule => rule.MatchesAlert(alert));
    }

    public async Task<IEnumerable<AlertEscalation>> GetActiveRulesByPriorityAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.AlertEscalations
            .Where(rule => rule.IsEnabled && rule.IsActive)
            .Include(rule => rule.Location)
            .Include(rule => rule.EscalationTargetUser)
            .OrderBy(rule => rule.RulePriority)
            .ThenBy(rule => rule.EscalationDelayMinutes)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AlertEscalation>> GetRulesByAlertTypeAsync(NotificationAlertType alertType, 
        CancellationToken cancellationToken = default)
    {
        return await _context.AlertEscalations
            .Where(rule => rule.AlertType == alertType && rule.IsEnabled && rule.IsActive)
            .Include(rule => rule.Location)
            .Include(rule => rule.EscalationTargetUser)
            .OrderBy(rule => rule.RulePriority)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TestRuleMatchAsync(int ruleId, NotificationAlert alert, 
        CancellationToken cancellationToken = default)
    {
        var rule = await _context.AlertEscalations
            .FirstOrDefaultAsync(r => r.Id == ruleId && r.IsActive, cancellationToken);

        return rule?.MatchesAlert(alert) ?? false;
    }

    /// <summary>
    /// Get rules by action type
    /// </summary>
    public async Task<IEnumerable<AlertEscalation>> GetRulesByActionAsync(EscalationAction action, 
        CancellationToken cancellationToken = default)
    {
        return await _context.AlertEscalations
            .Where(rule => rule.Action == action && rule.IsEnabled && rule.IsActive)
            .Include(rule => rule.Location)
            .Include(rule => rule.EscalationTargetUser)
            .OrderBy(rule => rule.RulePriority)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get rules for a specific location
    /// </summary>
    public async Task<IEnumerable<AlertEscalation>> GetRulesForLocationAsync(int locationId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.AlertEscalations
            .Where(rule => (rule.LocationId == locationId || rule.LocationId == null) && 
                          rule.IsEnabled && rule.IsActive)
            .Include(rule => rule.Location)
            .Include(rule => rule.EscalationTargetUser)
            .OrderBy(rule => rule.RulePriority)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get rules targeting a specific role
    /// </summary>
    public async Task<IEnumerable<AlertEscalation>> GetRulesForRoleAsync(string role, 
        CancellationToken cancellationToken = default)
    {
        return await _context.AlertEscalations
            .Where(rule => (rule.TargetRole == role || rule.TargetRole == null) && 
                          rule.IsEnabled && rule.IsActive)
            .Include(rule => rule.Location)
            .Include(rule => rule.EscalationTargetUser)
            .OrderBy(rule => rule.RulePriority)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get rules with email escalation
    /// </summary>
    public async Task<IEnumerable<AlertEscalation>> GetEmailEscalationRulesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.AlertEscalations
            .Where(rule => rule.Action == EscalationAction.SendEmail && 
                          !string.IsNullOrEmpty(rule.EscalationEmails) && 
                          rule.IsEnabled && rule.IsActive)
            .Include(rule => rule.Location)
            .OrderBy(rule => rule.RulePriority)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get rules with SMS escalation
    /// </summary>
    public async Task<IEnumerable<AlertEscalation>> GetSmsEscalationRulesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.AlertEscalations
            .Where(rule => rule.Action == EscalationAction.SendSMS && 
                          !string.IsNullOrEmpty(rule.EscalationPhones) && 
                          rule.IsEnabled && rule.IsActive)
            .Include(rule => rule.Location)
            .OrderBy(rule => rule.RulePriority)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get escalation statistics
    /// </summary>
    public async Task<EscalationStatistics> GetEscalationStatisticsAsync(DateTime fromDate, DateTime toDate, 
        CancellationToken cancellationToken = default)
    {
        var rules = await _context.AlertEscalations
            .Where(rule => rule.IsActive)
            .ToListAsync(cancellationToken);

        return new EscalationStatistics
        {
            TotalRules = rules.Count,
            ActiveRules = rules.Count(r => r.IsEnabled),
            InactiveRules = rules.Count(r => !r.IsEnabled),
            RulesByAction = rules.GroupBy(r => r.Action).ToDictionary(g => g.Key, g => g.Count()),
            RulesByAlertType = rules.GroupBy(r => r.AlertType).ToDictionary(g => g.Key, g => g.Count()),
            RulesByPriority = rules.GroupBy(r => r.AlertPriority).ToDictionary(g => g.Key, g => g.Count())
        };
    }

    /// <summary>
    /// Create default escalation rules
    /// </summary>
    public async Task CreateDefaultRulesAsync(CancellationToken cancellationToken = default)
    {
        var existingRules = await _context.AlertEscalations.AnyAsync(cancellationToken);
        if (existingRules) return; // Don't create if rules already exist

        var defaultRules = new List<AlertEscalation>
        {
            // Critical blacklist alerts
            new AlertEscalation
            {
                RuleName = "Critical Blacklist Alert Escalation",
                AlertType = NotificationAlertType.BlacklistAlert,
                AlertPriority = AlertPriority.Critical,
                EscalationDelayMinutes = 2,
                Action = EscalationAction.EscalateToRole,
                EscalationTargetRole = UserRoles.Administrator,
                MaxAttempts = 3,
                RulePriority = 1
            },

            // Emergency alerts
            new AlertEscalation
            {
                RuleName = "Emergency Alert Escalation",
                AlertType = NotificationAlertType.EmergencyAlert,
                AlertPriority = AlertPriority.Emergency,
                EscalationDelayMinutes = 1,
                Action = EscalationAction.CreateHighPriorityAlert,
                MaxAttempts = 5,
                RulePriority = 1
            },

            // FR System offline
            new AlertEscalation
            {
                RuleName = "FR System Offline Escalation",
                AlertType = NotificationAlertType.FRSystemOffline,
                AlertPriority = AlertPriority.High,
                EscalationDelayMinutes = 10,
                Action = EscalationAction.EscalateToRole,
                EscalationTargetRole = UserRoles.Administrator,
                MaxAttempts = 3,
                RulePriority = 5
            },

            // Capacity alerts
            new AlertEscalation
            {
                RuleName = "Capacity Alert Escalation",
                AlertType = NotificationAlertType.CapacityAlert,
                AlertPriority = AlertPriority.Medium,
                EscalationDelayMinutes = 15,
                Action = EscalationAction.LogCriticalEvent,
                MaxAttempts = 2,
                RulePriority = 10
            }
        };

        foreach (var rule in defaultRules)
        {
            rule.SetCreatedBy(1); // System user
        }

        _context.AlertEscalations.AddRange(defaultRules);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Test escalation rule configuration
    /// </summary>
    public async Task<RuleTestResult> TestRuleConfigurationAsync(int ruleId, 
        CancellationToken cancellationToken = default)
    {
        var rule = await _context.AlertEscalations
            .Include(r => r.Location)
            .Include(r => r.EscalationTargetUser)
            .FirstOrDefaultAsync(r => r.Id == ruleId, cancellationToken);

        if (rule == null)
        {
            return new RuleTestResult { IsValid = false, ErrorMessage = "Rule not found" };
        }

        var errors = new List<string>();

        // Validate rule configuration
        if (rule.EscalationDelayMinutes < 0)
            errors.Add("Escalation delay cannot be negative");

        if (rule.MaxAttempts < 1 || rule.MaxAttempts > 10)
            errors.Add("Max attempts must be between 1 and 10");

        // Validate action-specific configuration
        switch (rule.Action)
        {
            case EscalationAction.EscalateToRole:
                if (string.IsNullOrEmpty(rule.EscalationTargetRole))
                    errors.Add("Target role is required for role escalation");
                break;

            case EscalationAction.EscalateToUser:
                if (!rule.EscalationTargetUserId.HasValue)
                    errors.Add("Target user is required for user escalation");
                break;

            case EscalationAction.SendEmail:
                if (string.IsNullOrEmpty(rule.EscalationEmails))
                    errors.Add("Email addresses are required for email escalation");
                break;

            case EscalationAction.SendSMS:
                if (string.IsNullOrEmpty(rule.EscalationPhones))
                    errors.Add("Phone numbers are required for SMS escalation");
                break;
        }

        return new RuleTestResult
        {
            IsValid = !errors.Any(),
            ErrorMessage = string.Join("; ", errors),
            Rule = rule
        };
    }
}

/// <summary>
/// Escalation statistics for reporting
/// </summary>
public class EscalationStatistics
{
    public int TotalRules { get; set; }
    public int ActiveRules { get; set; }
    public int InactiveRules { get; set; }
    public Dictionary<EscalationAction, int> RulesByAction { get; set; } = new();
    public Dictionary<NotificationAlertType, int> RulesByAlertType { get; set; } = new();
    public Dictionary<AlertPriority, int> RulesByPriority { get; set; } = new();
}

/// <summary>
/// Rule test result
/// </summary>
public class RuleTestResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public AlertEscalation? Rule { get; set; }
}
