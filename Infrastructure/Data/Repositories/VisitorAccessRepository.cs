using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for visitor access operations
/// </summary>
public class VisitorAccessRepository : BaseRepository<VisitorAccess>, IVisitorAccessRepository
{
    public VisitorAccessRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> HasAccessAsync(
        int userId,
        int visitorId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(
            va => va.UserId == userId &&
                  va.VisitorId == visitorId &&
                  va.IsActive,
            cancellationToken);
    }

    public async Task<List<int>> GetAccessibleVisitorIdsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(va => va.UserId == userId && va.IsActive)
            .Select(va => va.VisitorId)
            .ToListAsync(cancellationToken);
    }

    public async Task GrantAccessAsync(
        int userId,
        int visitorId,
        VisitorAccessType accessType,
        int grantedBy,
        CancellationToken cancellationToken = default)
    {
        // Check if access already exists
        var existingAccess = await _dbSet
            .FirstOrDefaultAsync(
                va => va.UserId == userId && va.VisitorId == visitorId,
                cancellationToken);

        if (existingAccess != null)
        {
            // Access already exists - reactivate if needed
            if (!existingAccess.IsActive)
            {
                existingAccess.IsActive = true;
                existingAccess.ModifiedOn = DateTime.UtcNow;
            }
            return;
        }

        // Create new access record
        var access = new VisitorAccess
        {
            UserId = userId,
            VisitorId = visitorId,
            AccessType = accessType,
            GrantedBy = grantedBy,
            GrantedOn = DateTime.UtcNow,
            IsActive = true,
            CreatedOn = DateTime.UtcNow
        };

        await AddAsync(access, cancellationToken);
    }

    public async Task<List<VisitorAccess>> GetByVisitorIdAsync(
        int visitorId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(va => va.User)
            .Include(va => va.GrantedByUser)
            .Where(va => va.VisitorId == visitorId && va.IsActive)
            .OrderByDescending(va => va.GrantedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<VisitorAccess>> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(va => va.Visitor)
            .Include(va => va.GrantedByUser)
            .Where(va => va.UserId == userId && va.IsActive)
            .OrderByDescending(va => va.GrantedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeAccessAsync(
        int userId,
        int visitorId,
        CancellationToken cancellationToken = default)
    {
        var access = await _dbSet
            .FirstOrDefaultAsync(
                va => va.UserId == userId && va.VisitorId == visitorId && va.IsActive,
                cancellationToken);

        if (access != null)
        {
            access.IsActive = false;
            access.ModifiedOn = DateTime.UtcNow;
        }
    }
}
