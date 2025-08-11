using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for visitor note operations
/// </summary>
public class VisitorNoteRepository : BaseRepository<VisitorNote>, IVisitorNoteRepository
{
    public VisitorNoteRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<VisitorNote>> GetByVisitorIdAsync(int visitorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(n => n.CreatedByUser)
            .Include(n => n.ModifiedByUser)
            .Where(n => n.VisitorId == visitorId && n.IsActive)
            .OrderByDescending(n => n.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<VisitorNote>> GetFlaggedNotesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(n => n.Visitor)
            .Include(n => n.CreatedByUser)
            .Where(n => n.IsFlagged && n.IsActive)
            .OrderByDescending(n => n.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<VisitorNote>> GetOverdueFollowUpsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Include(n => n.Visitor)
            .Include(n => n.CreatedByUser)
            .Where(n => n.IsFlagged && 
                       n.FollowUpDate.HasValue && 
                       n.FollowUpDate.Value < now && 
                       n.IsActive)
            .OrderBy(n => n.FollowUpDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<VisitorNote>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(n => n.Visitor)
            .Include(n => n.CreatedByUser)
            .Where(n => n.Category.ToLower() == category.ToLower() && 
                       n.IsActive)
            .OrderByDescending(n => n.CreatedOn)
            .ToListAsync(cancellationToken);
    }
}
