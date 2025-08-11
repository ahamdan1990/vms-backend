using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for location operations
/// </summary>
public class LocationRepository : BaseRepository<Location>, ILocationRepository
{
    public LocationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Location>> GetOrderedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(l => l.ParentLocation)
            .Include(l => l.ChildLocations)
            .Where(l => l.IsActive)
            .OrderBy(l => l.DisplayOrder)
            .ThenBy(l => l.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Location?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(l => l.ParentLocation)
            .Include(l => l.ChildLocations)
            .FirstOrDefaultAsync(l => l.Code.ToLower() == code.ToLower() && 
                                     l.IsActive, 
                                cancellationToken);
    }

    public async Task<List<Location>> GetByTypeAsync(string locationType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(l => l.ParentLocation)
            .Where(l => l.LocationType.ToLower() == locationType.ToLower() && 
                       l.IsActive)
            .OrderBy(l => l.DisplayOrder)
            .ThenBy(l => l.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Location>> GetRootLocationsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(l => l.ChildLocations.Where(c => c.IsActive))
            .Where(l => l.ParentLocationId == null && l.IsActive)
            .OrderBy(l => l.DisplayOrder)
            .ThenBy(l => l.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Location>> GetChildLocationsAsync(int parentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(l => l.ChildLocations.Where(c => c.IsActive))
            .Where(l => l.ParentLocationId == parentId && l.IsActive)
            .OrderBy(l => l.DisplayOrder)
            .ThenBy(l => l.Name)
            .ToListAsync(cancellationToken);
    }
}
