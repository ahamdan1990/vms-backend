using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for visit purpose operations
/// </summary>
public class VisitPurposeRepository : BaseRepository<VisitPurpose>, IVisitPurposeRepository
{
    public VisitPurposeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<VisitPurpose>> GetOrderedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<VisitPurpose?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.Code.ToLower() == code.ToLower() && 
                                     p.IsActive, 
                                cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.Code.ToLower() == code.ToLower() && 
                       (excludeId == null || p.Id != excludeId) &&
                       p.IsActive)
            .AnyAsync(cancellationToken);
    }
}
