using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.Models;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for visitor operations
/// </summary>
public class VisitorRepository : BaseRepository<Visitor>, IVisitorRepository
{
    public VisitorRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();
        return await _dbSet
            .Where(v => v.NormalizedEmail == normalizedEmail && (excludeId == null || v.Id != excludeId))
            .AnyAsync(cancellationToken);
    }

    public async Task<Visitor?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();
        return await _dbSet
            .Include(v => v.EmergencyContacts)
            .Include(v => v.Documents)
            .Include(v => v.VisitorNotes)
            .FirstOrDefaultAsync(v => v.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<(List<Visitor> Visitors, int TotalCount)> SearchVisitorsAsync(
        string searchTerm, 
        int pageIndex, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower().Trim();
            query = query.Where(v => 
                v.FirstName.ToLower().Contains(term) ||
                v.LastName.ToLower().Contains(term) ||
                v.Email.Value.ToLower().Contains(term) ||
                (v.Company != null && v.Company.ToLower().Contains(term)));
        }

        // Filter active visitors (IsDeleted filtering handled by global query filter)
        query = query.Where(v => v.IsActive);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var visitors = await query
            .OrderBy(v => v.FirstName)
            .ThenBy(v => v.LastName)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (visitors, totalCount);
    }

    public async Task<List<Visitor>> GetByCompanyAsync(string company, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.Company != null && 
                       v.Company.ToLower().Contains(company.ToLower()) && 
                       v.IsActive)
            .OrderBy(v => v.FirstName)
            .ThenBy(v => v.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Visitor>> GetVipVisitorsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.IsVip && v.IsActive)
            .OrderBy(v => v.FirstName)
            .ThenBy(v => v.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Visitor>> GetBlacklistedVisitorsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(v => v.BlacklistedByUser)
            .Where(v => v.IsBlacklisted && v.IsActive)
            .OrderBy(v => v.BlacklistedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Visitor>> GetIncompleteProfilesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.IsActive && 
                       (v.PhoneNumber == null || 
                        v.Company == null || 
                        v.Address == null))
            .OrderBy(v => v.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<List<Visitor>>> GetPotentialDuplicatesAsync(CancellationToken cancellationToken = default)
    {
        // Find potential duplicates based on email, name similarity, or government ID
        var duplicateGroups = new List<List<Visitor>>();

        // Group by email (case-insensitive)
        var emailGroups = await _dbSet
            .Where(v => v.IsActive)
            .GroupBy(v => v.NormalizedEmail)
            .Where(g => g.Count() > 1)
            .Select(g => g.ToList())
            .ToListAsync(cancellationToken);

        duplicateGroups.AddRange(emailGroups);

        // Group by government ID (if not null)
        var govIdGroups = await _dbSet
            .Where(v => v.IsActive && v.GovernmentId != null)
            .GroupBy(v => v.GovernmentId)
            .Where(g => g.Count() > 1)
            .Select(g => g.ToList())
            .ToListAsync(cancellationToken);

        duplicateGroups.AddRange(govIdGroups);

        // Remove duplicates from groups
        return duplicateGroups
            .GroupBy(group => string.Join(",", group.Select(v => v.Id).OrderBy(id => id)))
            .Select(g => g.First())
            .ToList();
    }

    public async Task<VisitorStatistics> GetVisitorStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfYear = new DateTime(now.Year, 1, 1);

        var totalVisitors = await _dbSet.CountAsync(cancellationToken);
        var activeVisitors = await _dbSet.CountAsync(v => v.IsActive, cancellationToken);
        var vipVisitors = await _dbSet.CountAsync(v => v.IsVip && v.IsActive, cancellationToken);
        var blacklistedVisitors = await _dbSet.CountAsync(v => v.IsBlacklisted && v.IsActive, cancellationToken);
        
        var incompleteProfiles = await _dbSet.CountAsync(v => 
            v.IsActive && 
            (v.PhoneNumber == null || v.Company == null || v.Address == null), 
            cancellationToken);

        var visitorsThisMonth = await _dbSet.CountAsync(v => 
            v.CreatedOn >= startOfMonth && v.IsActive, 
            cancellationToken);

        var visitorsThisYear = await _dbSet.CountAsync(v => 
            v.CreatedOn >= startOfYear && v.IsActive, 
            cancellationToken);

        var totalVisits = await _dbSet
            .Where(v => v.IsActive)
            .SumAsync(v => v.VisitCount, cancellationToken);

        var averageVisitsPerVisitor = activeVisitors > 0 ? (double)totalVisits / activeVisitors : 0;

        return new VisitorStatistics
        {
            TotalVisitors = totalVisitors,
            ActiveVisitors = activeVisitors,
            VipVisitors = vipVisitors,
            BlacklistedVisitors = blacklistedVisitors,
            IncompleteProfiles = incompleteProfiles,
            VisitorsThisMonth = visitorsThisMonth,
            VisitorsThisYear = visitorsThisYear,
            AverageVisitsPerVisitor = Math.Round(averageVisitsPerVisitor, 2)
        };
    }

    public async Task<List<Visitor>> GetVisitorsByDateRangeAsync(
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.CreatedOn >= startDate && 
                       v.CreatedOn <= endDate && 
                       v.IsActive)
            .OrderByDescending(v => v.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CompanyVisitorCount>> GetTopCompaniesByVisitorCountAsync(
        int limit = 10, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.Company != null && v.IsActive)
            .GroupBy(v => v.Company)
            .Select(g => new CompanyVisitorCount
            {
                Company = g.Key!,
                VisitorCount = g.Count(),
                TotalVisits = g.Sum(v => v.VisitCount),
                LastVisit = g.Max(v => v.LastVisitDate)
            })
            .OrderByDescending(c => c.VisitorCount)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateVisitStatisticsAsync(int visitorId, CancellationToken cancellationToken = default)
    {
        var visitor = await GetByIdAsync(visitorId, cancellationToken);
        if (visitor != null)
        {
            visitor.UpdateVisitStatistics();
            Update(visitor);
        }
    }

    public async Task<bool> GovernmentIdExistsAsync(
        string governmentId, 
        int? excludeId = null, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.GovernmentId == governmentId && (excludeId == null || v.Id != excludeId))
            .AnyAsync(cancellationToken);
    }

    // Method moved from RepositoryExtensions
    public async Task<Visitor?> GetByFRPersonIdAsync(
        string frPersonId,
        CancellationToken cancellationToken = default)
    {
        // This assumes the ExternalId field is used to store FR person ID
        return await _dbSet
            .Include(v => v.EmergencyContacts)
            .Include(v => v.Documents)
            .Include(v => v.VisitorNotes)
            .FirstOrDefaultAsync(v => v.ExternalId == frPersonId && v.IsActive, cancellationToken);
    }
}
