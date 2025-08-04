using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Infrastructure.Data;
using VisitorManagementSystem.Api.Infrastructure.Data.Repositories;

namespace VisitorManagementSystem.Api.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for ConfigurationAudit entity
/// </summary>
public class ConfigurationAuditRepository : BaseRepository<ConfigurationAudit>, IConfigurationAuditRepository
{
    public ConfigurationAuditRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<ConfigurationAudit>> GetConfigurationHistoryAsync(string category, string key, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return await _context.ConfigurationAudits
            .Include(a => a.CreatedByUser)
            .Include(a => a.ApprovedByUser)
            .Where(a => a.Category == category && a.Key == key)
            .OrderByDescending(a => a.CreatedOn)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ConfigurationAudit>> GetConfigurationHistoryByIdAsync(int configurationId, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return await _context.ConfigurationAudits
            .Include(a => a.CreatedByUser)
            .Include(a => a.ApprovedByUser)
            .Where(a => a.SystemConfigurationId == configurationId)
            .OrderByDescending(a => a.CreatedOn)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ConfigurationAudit>> GetByUserAsync(int userId, int days = 30, CancellationToken cancellationToken = default)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        
        return await _context.ConfigurationAudits
            .Include(a => a.SystemConfiguration)
            .Where(a => a.CreatedBy == userId && a.CreatedOn >= startDate)
            .OrderByDescending(a => a.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ConfigurationAudit>> GetRecentChangesAsync(int hours = 24, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow.AddHours(-hours);
        
        return await _context.ConfigurationAudits
            .Include(a => a.CreatedByUser)
            .Include(a => a.SystemConfiguration)
            .Where(a => a.CreatedOn >= startTime)
            .OrderByDescending(a => a.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ConfigurationAudit>> GetByCategoryAsync(string category, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.ConfigurationAudits
            .Include(a => a.CreatedByUser)
            .Where(a => a.Category == category && 
                       a.CreatedOn >= startDate && 
                       a.CreatedOn <= endDate)
            .OrderByDescending(a => a.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ConfigurationAudit>> GetByActionAsync(string action, int days = 30, CancellationToken cancellationToken = default)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        
        return await _context.ConfigurationAudits
            .Include(a => a.CreatedByUser)
            .Include(a => a.SystemConfiguration)
            .Where(a => a.Action == action && a.CreatedOn >= startDate)
            .OrderByDescending(a => a.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ConfigurationAudit>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ConfigurationAudits
            .Include(a => a.CreatedByUser)
            .Include(a => a.SystemConfiguration)
            .Where(a => a.RequiresApproval && !a.IsApproved)
            .OrderBy(a => a.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ConfigurationAudit>> SearchAsync(string searchTerm, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ConfigurationAudits
            .Include(a => a.CreatedByUser)
            .Include(a => a.SystemConfiguration)
            .AsQueryable();

        var lowerSearchTerm = searchTerm.ToLower();
        query = query.Where(a => a.Category.ToLower().Contains(lowerSearchTerm) ||
                                a.Key.ToLower().Contains(lowerSearchTerm) ||
                                a.Action.ToLower().Contains(lowerSearchTerm) ||
                                (a.Reason != null && a.Reason.ToLower().Contains(lowerSearchTerm)));

        if (startDate.HasValue)
        {
            query = query.Where(a => a.CreatedOn >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.CreatedOn <= endDate.Value);
        }

        return await query
            .OrderByDescending(a => a.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<ConfigurationAuditStats> GetAuditStatsAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var audits = await _context.ConfigurationAudits
            .Where(a => a.CreatedOn >= startDate)
            .ToListAsync(cancellationToken);

        var stats = new ConfigurationAuditStats
        {
            TotalChanges = audits.Count,
            CreatedCount = audits.Count(a => a.Action == "Created"),
            UpdatedCount = audits.Count(a => a.Action == "Updated"),
            DeletedCount = audits.Count(a => a.Action == "Deleted"),
            UniqueUsers = audits.Where(a => a.CreatedBy.HasValue).Select(a => a.CreatedBy.Value).Distinct().Count(),
            UniqueConfigurations = audits.Select(a => $"{a.Category}.{a.Key}").Distinct().Count(),
            ChangesByCategory = audits.GroupBy(a => a.Category).ToDictionary(g => g.Key, g => g.Count()),
            ChangesByDay = audits.GroupBy(a => a.CreatedOn.Date.ToString("yyyy-MM-dd")).ToDictionary(g => g.Key, g => g.Count()),
            MostRecentChange = audits.MaxBy(a => a.CreatedOn)?.CreatedOn
        };

        if (stats.ChangesByCategory.Any())
        {
            stats.MostActiveCategory = stats.ChangesByCategory.OrderByDescending(kvp => kvp.Value).First().Key;
        }

        return stats;
    }

    public async Task<int> ArchiveOldEntriesAsync(int olderThanDays, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);

        var oldEntries = await _context.ConfigurationAudits
            .Where(a => a.CreatedOn < cutoffDate)
            .ToListAsync(cancellationToken);

        if (oldEntries.Any())
        {
            _context.ConfigurationAudits.RemoveRange(oldEntries);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return oldEntries.Count;
    }
}
