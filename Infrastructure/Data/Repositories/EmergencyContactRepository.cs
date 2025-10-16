using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for emergency contact operations
/// </summary>
public class EmergencyContactRepository : BaseRepository<EmergencyContact>, IEmergencyContactRepository
{
    public EmergencyContactRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<EmergencyContact>> GetByVisitorIdAsync(int visitorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.CreatedByUser)
            .Include(c => c.ModifiedByUser)
            .Where(c => c.VisitorId == visitorId && c.IsActive)
            .OrderBy(c => c.Priority)
            .ThenBy(c => c.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmergencyContact?> GetPrimaryContactAsync(int visitorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.CreatedByUser)
            .Include(c => c.ModifiedByUser)
            .Where(c => c.VisitorId == visitorId && c.IsPrimary && c.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
