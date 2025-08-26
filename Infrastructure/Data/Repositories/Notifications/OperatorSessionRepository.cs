using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Repositories.Notifications;

/// <summary>
/// Repository implementation for operator sessions
/// </summary>
public class OperatorSessionRepository : BaseRepository<OperatorSession>, IOperatorSessionRepository
{
    public OperatorSessionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<OperatorSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OperatorSessions
            .Where(s => s.SessionEnd == null && s.Status != OperatorStatus.Offline && s.IsActive)
            .Include(s => s.User)
            .Include(s => s.Location)
            .OrderBy(s => s.SessionStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<OperatorSession>> GetActiveSessionsForLocationAsync(int locationId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.OperatorSessions
            .Where(s => s.LocationId == locationId && 
                       s.SessionEnd == null && 
                       s.Status != OperatorStatus.Offline && 
                       s.IsActive)
            .Include(s => s.User)
            .Include(s => s.Location)
            .OrderBy(s => s.SessionStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<OperatorSession?> GetSessionByConnectionIdAsync(string connectionId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.OperatorSessions
            .Where(s => s.ConnectionId == connectionId && s.IsActive)
            .Include(s => s.User)
            .Include(s => s.Location)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> EndSessionByConnectionIdAsync(string connectionId, 
        CancellationToken cancellationToken = default)
    {
        var session = await _context.OperatorSessions
            .FirstOrDefaultAsync(s => s.ConnectionId == connectionId && s.SessionEnd == null, cancellationToken);

        if (session == null)
            return false;

        session.EndSession();
        _context.OperatorSessions.Update(session);
        
        return await _context.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<OperatorSession?> GetActiveSessionForUserAsync(int userId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.OperatorSessions
            .Where(s => s.UserId == userId && 
                       s.SessionEnd == null && 
                       s.Status != OperatorStatus.Offline && 
                       s.IsActive)
            .Include(s => s.User)
            .Include(s => s.Location)
            .OrderByDescending(s => s.SessionStart)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> UpdateSessionActivityAsync(string connectionId, 
        CancellationToken cancellationToken = default)
    {
        var session = await _context.OperatorSessions
            .FirstOrDefaultAsync(s => s.ConnectionId == connectionId && s.SessionEnd == null, cancellationToken);

        if (session == null)
            return false;

        session.UpdateActivity();
        _context.OperatorSessions.Update(session);
        
        return await _context.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<Dictionary<OperatorStatus, int>> GetSessionStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var activeStatuses = await _context.OperatorSessions
            .Where(s => s.SessionEnd == null && s.IsActive)
            .GroupBy(s => s.Status)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Count(),
                cancellationToken);

        // Ensure all statuses are represented
        var allStatuses = Enum.GetValues<OperatorStatus>().ToDictionary(status => status, _ => 0);
        foreach (var kvp in activeStatuses)
        {
            allStatuses[kvp.Key] = kvp.Value;
        }

        return allStatuses;
    }

    /// <summary>
    /// Get sessions by user with date range
    /// </summary>
    public async Task<IEnumerable<OperatorSession>> GetSessionsForUserAsync(int userId, DateTime? fromDate = null, 
        DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.OperatorSessions
            .Where(s => s.UserId == userId && s.IsActive);

        if (fromDate.HasValue)
            query = query.Where(s => s.SessionStart >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(s => s.SessionStart <= toDate.Value);

        return await query
            .Include(s => s.Location)
            .OrderByDescending(s => s.SessionStart)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get inactive sessions that need cleanup (older than threshold)
    /// </summary>
    public async Task<IEnumerable<OperatorSession>> GetInactiveSessionsAsync(TimeSpan inactivityThreshold, 
        CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow - inactivityThreshold;

        return await _context.OperatorSessions
            .Where(s => s.SessionEnd == null && 
                       s.LastActivity < cutoffTime && 
                       s.IsActive)
            .Include(s => s.User)
            .OrderBy(s => s.LastActivity)
            .Take(50) // Process in batches
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get session duration statistics
    /// </summary>
    public async Task<SessionStatistics> GetSessionDurationStatisticsAsync(DateTime fromDate, DateTime toDate, 
        CancellationToken cancellationToken = default)
    {
        var completedSessions = await _context.OperatorSessions
            .Where(s => s.SessionStart >= fromDate && 
                       s.SessionStart <= toDate && 
                       s.SessionEnd != null &&
                       s.IsActive)
            .ToListAsync(cancellationToken);

        if (!completedSessions.Any())
        {
            return new SessionStatistics
            {
                TotalSessions = 0,
                AverageSessionDuration = TimeSpan.Zero,
                MinSessionDuration = TimeSpan.Zero,
                MaxSessionDuration = TimeSpan.Zero
            };
        }

        var durations = completedSessions.Select(s => s.SessionDuration).ToList();

        return new SessionStatistics
        {
            TotalSessions = completedSessions.Count,
            AverageSessionDuration = TimeSpan.FromTicks((long)durations.Average(d => d.Ticks)),
            MinSessionDuration = durations.Min(),
            MaxSessionDuration = durations.Max(),
            SessionsByLocation = completedSessions
                .Where(s => s.LocationId.HasValue)
                .GroupBy(s => s.LocationId!.Value)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    /// <summary>
    /// Clean up old completed sessions
    /// </summary>
    public async Task<int> CleanupOldSessionsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var oldSessions = await _context.OperatorSessions
            .Where(s => s.SessionEnd != null && s.SessionEnd < olderThan && s.IsActive)
            .Take(1000) // Process in batches
            .ToListAsync(cancellationToken);

        foreach (var session in oldSessions)
        {
            session.Deactivate();
        }

        _context.OperatorSessions.UpdateRange(oldSessions);
        return await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Session duration statistics
/// </summary>
public class SessionStatistics
{
    public int TotalSessions { get; set; }
    public TimeSpan AverageSessionDuration { get; set; }
    public TimeSpan MinSessionDuration { get; set; }
    public TimeSpan MaxSessionDuration { get; set; }
    public Dictionary<int, int> SessionsByLocation { get; set; } = new();
}
