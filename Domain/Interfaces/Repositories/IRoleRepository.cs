using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for Role entity
/// </summary>
public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Role>> GetAsync(System.Linq.Expressions.Expression<Func<Role, bool>> predicate, CancellationToken cancellationToken = default);
    Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Role entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Role entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Role entity, CancellationToken cancellationToken = default);
}
