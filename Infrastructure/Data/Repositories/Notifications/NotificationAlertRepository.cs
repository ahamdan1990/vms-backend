using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Repositories.Notifications;

/// <summary>
/// Repository implementation for notification alerts
/// </summary>
public class NotificationAlertRepository : BaseRepository<NotificationAlert>, INotificationAlertRepository
{
    public NotificationAlertRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<NotificationAlert>> GetUnacknowledgedAlertsForUserAsync(int userId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationAlerts
            .Where(a => a.TargetUserId == userId && !a.IsAcknowledged && a.IsActive)
            .Include(a => a.TargetLocation)
            .OrderByDescending(a => a.Priority)
            .ThenByDescending(a => a.CreatedOn)
            .Take(50) // Limit to recent alerts
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationAlert>> GetUnacknowledgedAlertsForRoleAsync(string role, 
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationAlerts
            .Where(a => a.TargetRole == role && !a.IsAcknowledged && a.IsActive)
            .Include(a => a.TargetLocation)
            .OrderByDescending(a => a.Priority)
            .ThenByDescending(a => a.CreatedOn)
            .Take(100) // More for role-based alerts
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationAlert>> GetUnacknowledgedAlertsForLocationAsync(int locationId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationAlerts
            .Where(a => a.TargetLocationId == locationId && !a.IsAcknowledged && a.IsActive)
            .Include(a => a.TargetLocation)
            .OrderByDescending(a => a.Priority)
            .ThenByDescending(a => a.CreatedOn)
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationAlert>> GetAlertsByTypeAndPriorityAsync(NotificationAlertType type, 
        AlertPriority priority, DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.NotificationAlerts
            .Where(a => a.Type == type && a.Priority == priority && a.IsActive);

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.CreatedOn >= fromDate.Value);
        }

        return await query
            .Include(a => a.TargetUser)
            .Include(a => a.TargetLocation)
            .OrderByDescending(a => a.CreatedOn)
            .Take(200)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationAlert>> GetRecentAlertsForUserAsync(int userId, int days = 7, 
        CancellationToken cancellationToken = default)
    {
        var fromDate = DateTime.UtcNow.AddDays(-days);

        return await _context.NotificationAlerts
            .Where(a => a.TargetUserId == userId && a.CreatedOn >= fromDate && a.IsActive)
            .Include(a => a.AcknowledgedByUser)
            .OrderByDescending(a => a.CreatedOn)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AcknowledgeAlertAsync(int alertId, int acknowledgedBy, 
        CancellationToken cancellationToken = default)
    {
        var alert = await _context.NotificationAlerts
            .FirstOrDefaultAsync(a => a.Id == alertId && !a.IsAcknowledged, cancellationToken);

        if (alert == null)
            return false;

        alert.Acknowledge(acknowledgedBy);
        _context.NotificationAlerts.Update(alert);
        
        return await _context.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<IEnumerable<NotificationAlert>> GetEscalationCandidatesAsync(int olderThanMinutes, 
        CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-olderThanMinutes);

        return await _context.NotificationAlerts
            .Where(a => !a.IsAcknowledged && 
                       a.IsActive && 
                       a.CreatedOn < cutoffTime &&
                       (a.ExpiresOn == null || a.ExpiresOn > DateTime.UtcNow) &&
                       (a.Priority == AlertPriority.High || a.Priority == AlertPriority.Critical || a.Priority == AlertPriority.Emergency))
            .Include(a => a.TargetUser)
            .Include(a => a.TargetLocation)
            .OrderBy(a => a.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationAlert>> GetExpiredAlertsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.NotificationAlerts
            .Where(a => a.ExpiresOn.HasValue && a.ExpiresOn < now && a.IsActive)
            .OrderBy(a => a.ExpiresOn)
            .Take(1000) // Process in batches
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<NotificationAlertType, int>> GetAlertStatisticsAsync(DateTime fromDate, DateTime toDate, 
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationAlerts
            .Where(a => a.CreatedOn >= fromDate && a.CreatedOn <= toDate && a.IsActive)
            .GroupBy(a => a.Type)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Count(),
                cancellationToken);
    }

    /// <summary>
    /// Get alerts that need external delivery (email/SMS)
    /// </summary>
    public async Task<IEnumerable<NotificationAlert>> GetAlertsForExternalDeliveryAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NotificationAlerts
            .Where(a => !a.SentExternally && 
                       a.IsActive &&
                       (a.Priority == AlertPriority.Critical || a.Priority == AlertPriority.Emergency) &&
                       a.CreatedOn > DateTime.UtcNow.AddHours(-24))
            .OrderBy(a => a.CreatedOn)
            .Take(20) // Process in small batches
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get summary statistics for dashboard
    /// </summary>
    public async Task<AlertSummary> GetAlertSummaryAsync(DateTime fromDate, CancellationToken cancellationToken = default)
    {
        var alerts = await _context.NotificationAlerts
            .Where(a => a.CreatedOn >= fromDate && a.IsActive)
            .ToListAsync(cancellationToken);

        return new AlertSummary
        {
            TotalAlerts = alerts.Count,
            UnacknowledgedAlerts = alerts.Count(a => !a.IsAcknowledged),
            CriticalAlerts = alerts.Count(a => a.Priority == AlertPriority.Critical || a.Priority == AlertPriority.Emergency),
            AlertsByType = alerts.GroupBy(a => a.Type).ToDictionary(g => g.Key, g => g.Count()),
            AlertsByPriority = alerts.GroupBy(a => a.Priority).ToDictionary(g => g.Key, g => g.Count())
        };
    }
}

/// <summary>
/// Alert summary for dashboard display
/// </summary>
public class AlertSummary
{
    public int TotalAlerts { get; set; }
    public int UnacknowledgedAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public Dictionary<NotificationAlertType, int> AlertsByType { get; set; } = new();
    public Dictionary<AlertPriority, int> AlertsByPriority { get; set; } = new();
}
