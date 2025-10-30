using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for Permission entity
/// </summary>
public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Permission>> GetAsync(System.Linq.Expressions.Expression<Func<Permission, bool>> predicate, CancellationToken cancellationToken = default);
    Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Permission entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Permission entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Permission entity, CancellationToken cancellationToken = default);
}
