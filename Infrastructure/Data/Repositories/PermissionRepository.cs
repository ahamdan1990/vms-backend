using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Permission entity
/// </summary>
public class PermissionRepository : IPermissionRepository
{
    private readonly ApplicationDbContext _context;

    public PermissionRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Permission?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<List<Permission>> GetAsync(Expression<Func<Permission, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions.Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Permissions.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Permission entity, CancellationToken cancellationToken = default)
    {
        await _context.Permissions.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(Permission entity, CancellationToken cancellationToken = default)
    {
        _context.Permissions.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Permission entity, CancellationToken cancellationToken = default)
    {
        _context.Permissions.Remove(entity);
        return Task.CompletedTask;
    }
}
