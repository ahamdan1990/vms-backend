using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository for audit log operations
/// </summary>
public class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<AuditLog>> GetByEntityAsync(string entityType, int entityId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<AuditLog>()
            .Where(a => a.EntityName == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AuditLog>> GetByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<AuditLog>()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedOn)
            .ToListAsync(cancellationToken);
    }
    public async Task<List<AuditLog>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _context.Set<AuditLog>()
            .Where(a => a.CreatedOn >= fromDate && a.CreatedOn <= toDate)
            .OrderByDescending(a => a.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CleanupOldLogsAsync(int olderThanDays, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
        
        var oldLogs = await _context.Set<AuditLog>()
            .Where(a => a.CreatedOn < cutoffDate)
            .ToListAsync(cancellationToken);

        _context.Set<AuditLog>().RemoveRange(oldLogs);
        
        return oldLogs.Count();
    }
}
