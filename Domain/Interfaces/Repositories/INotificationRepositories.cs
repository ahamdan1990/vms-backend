using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for notification alerts
/// </summary>
public interface INotificationAlertRepository : IGenericRepository<NotificationAlert>
{
    /// <summary>
    /// Get unacknowledged alerts for a specific user
    /// </summary>
    Task<IEnumerable<NotificationAlert>> GetUnacknowledgedAlertsForUserAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unacknowledged alerts for a specific role
    /// </summary>
    Task<IEnumerable<NotificationAlert>> GetUnacknowledgedAlertsForRoleAsync(string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unacknowledged alerts for a specific location
    /// </summary>
    Task<IEnumerable<NotificationAlert>> GetUnacknowledgedAlertsForLocationAsync(int locationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get alerts by type and priority
    /// </summary>
    Task<IEnumerable<NotificationAlert>> GetAlertsByTypeAndPriorityAsync(NotificationAlertType type, AlertPriority priority, 
        DateTime? fromDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent alerts for a user (last N days)
    /// </summary>
    Task<IEnumerable<NotificationAlert>> GetRecentAlertsForUserAsync(int userId, int days = 7, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark alert as acknowledged
    /// </summary>
    Task<bool> AcknowledgeAlertAsync(int alertId, int acknowledgedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get escalation candidates (unacknowledged alerts older than X minutes)
    /// </summary>
    Task<IEnumerable<NotificationAlert>> GetEscalationCandidatesAsync(int olderThanMinutes, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get expired alerts that need cleanup
    /// </summary>
    Task<IEnumerable<NotificationAlert>> GetExpiredAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get alert statistics for a date range
    /// </summary>
    Task<Dictionary<NotificationAlertType, int>> GetAlertStatisticsAsync(DateTime fromDate, DateTime toDate, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for operator sessions
/// </summary>
public interface IOperatorSessionRepository : IGenericRepository<OperatorSession>
{
    /// <summary>
    /// Get active operator sessions
    /// </summary>
    Task<IEnumerable<OperatorSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active sessions for a specific location
    /// </summary>
    Task<IEnumerable<OperatorSession>> GetActiveSessionsForLocationAsync(int locationId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get session by connection ID
    /// </summary>
    Task<OperatorSession?> GetSessionByConnectionIdAsync(string connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// End session by connection ID
    /// </summary>
    Task<bool> EndSessionByConnectionIdAsync(string connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active session for user
    /// </summary>
    Task<OperatorSession?> GetActiveSessionForUserAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update session activity
    /// </summary>
    Task<bool> UpdateSessionActivityAsync(string connectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get session statistics
    /// </summary>
    Task<Dictionary<OperatorStatus, int>> GetSessionStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for alert escalation rules
/// </summary>
public interface IAlertEscalationRepository : IGenericRepository<AlertEscalation>
{
    /// <summary>
    /// Get applicable escalation rules for an alert
    /// </summary>
    Task<IEnumerable<AlertEscalation>> GetApplicableRulesAsync(NotificationAlert alert, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active escalation rules by priority
    /// </summary>
    Task<IEnumerable<AlertEscalation>> GetActiveRulesByPriorityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get rules by alert type
    /// </summary>
    Task<IEnumerable<AlertEscalation>> GetRulesByAlertTypeAsync(NotificationAlertType alertType, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Test escalation rule against an alert
    /// </summary>
    Task<bool> TestRuleMatchAsync(int ruleId, NotificationAlert alert, CancellationToken cancellationToken = default);
}
