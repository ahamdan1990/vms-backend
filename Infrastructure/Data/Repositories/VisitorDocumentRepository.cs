using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for visitor document operations
/// </summary>
public class VisitorDocumentRepository : BaseRepository<VisitorDocument>, IVisitorDocumentRepository
{
    public VisitorDocumentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<VisitorDocument>> GetByVisitorIdAsync(int visitorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(d => d.CreatedByUser)
            .Include(d => d.ModifiedByUser)
            .Where(d => d.VisitorId == visitorId && d.IsActive)
            .OrderByDescending(d => d.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<VisitorDocument>> GetByVisitorAndTypeAsync(
        int visitorId, 
        string documentType, 
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(d => d.CreatedByUser)
            .Include(d => d.ModifiedByUser)
            .Where(d => d.VisitorId == visitorId && 
                       d.DocumentType.ToLower() == documentType.ToLower() && 
                       d.IsActive)
            .OrderByDescending(d => d.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<VisitorDocument>> GetExpiredDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Include(d => d.Visitor)
            .Include(d => d.CreatedByUser)
            .Where(d => d.ExpirationDate.HasValue && 
                       d.ExpirationDate.Value < now && 
                       d.IsActive)
            .OrderBy(d => d.ExpirationDate)
            .ToListAsync(cancellationToken);
    }
}
