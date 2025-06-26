using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Specifications;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Generic repository interface for common CRUD operations
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
public interface IGenericRepository<TEntity> where TEntity : BaseEntity
{
    /// <summary>
    /// Gets an entity by ID
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entity if found, null otherwise</returns>
    Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an entity by ID with includes
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <param name="includes">Include expressions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entity if found, null otherwise</returns>
    Task<TEntity?> GetByIdAsync(int id, params Expression<Func<TEntity, object>>[] includes);

    /// <summary>
    /// Gets all entities
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all entities</returns>
    Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities by specification
    /// </summary>
    /// <param name="specification">Specification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of entities matching specification</returns>
    Task<List<TEntity>> GetAsync(BaseSpecification<TEntity> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single entity by specification
    /// </summary>
    /// <param name="specification">Specification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entity if found, null otherwise</returns>
    Task<TEntity?> GetSingleAsync(BaseSpecification<TEntity> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with custom criteria
    /// </summary>
    /// <param name="predicate">Predicate expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of entities matching predicate</returns>
    Task<List<TEntity>> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with ordering
    /// </summary>
    /// <param name="predicate">Predicate expression</param>
    /// <param name="orderBy">Order by expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ordered list of entities</returns>
    Task<List<TEntity>> GetAsync<TKey>(Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>> orderBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with ordering descending
    /// </summary>
    /// <param name="predicate">Predicate expression</param>
    /// <param name="orderByDesc">Order by descending expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ordered list of entities</returns>
    Task<List<TEntity>> GetDescendingAsync<TKey>(Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>> orderByDesc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated entities
    /// </summary>
    /// <param name="pageIndex">Page index (0-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="predicate">Optional predicate</param>
    /// <param name="orderBy">Optional order by expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result with entities and total count</returns>
    Task<(List<TEntity> Entities, int TotalCount)> GetPagedAsync<TKey>(int pageIndex, int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, TKey>>? orderBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated entities with specification
    /// </summary>
    /// <param name="specification">Specification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result with entities and total count</returns>
    Task<(List<TEntity> Entities, int TotalCount)> GetPagedAsync(BaseSpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count of entities</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities by predicate
    /// </summary>
    /// <param name="predicate">Predicate expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of entities matching predicate</returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities by specification
    /// </summary>
    /// <param name="specification">Specification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of entities matching specification</returns>
    Task<int> CountAsync(BaseSpecification<TEntity> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity exists
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if any entity exists</returns>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity exists by predicate
    /// </summary>
    /// <param name="predicate">Predicate expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if any entity matches predicate</returns>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity exists by specification
    /// </summary>
    /// <param name="specification">Specification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if any entity matches specification</returns>
    Task<bool> AnyAsync(BaseSpecification<TEntity> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets first entity or default
    /// </summary>
    /// <param name="predicate">Predicate expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>First entity or null</returns>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets single entity or default
    /// </summary>
    /// <param name="predicate">Predicate expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Single entity or null</returns>
    Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity
    /// </summary>
    /// <param name="entity">Entity to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Added entity</returns>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities
    /// </summary>
    /// <param name="entities">Entities to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an entity
    /// </summary>
    /// <param name="entity">Entity to update</param>
    void Update(TEntity entity);

    /// <summary>
    /// Updates multiple entities
    /// </summary>
    /// <param name="entities">Entities to update</param>
    void UpdateRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// Removes an entity
    /// </summary>
    /// <param name="entity">Entity to remove</param>
    void Remove(TEntity entity);

    /// <summary>
    /// Removes entities by ID
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task RemoveAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple entities
    /// </summary>
    /// <param name="entities">Entities to remove</param>
    void RemoveRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// Removes entities by predicate
    /// </summary>
    /// <param name="predicate">Predicate expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities removed</returns>
    Task<int> RemoveAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes an entity (if it inherits from SoftDeleteEntity)
    /// </summary>
    /// <param name="entity">Entity to soft delete</param>
    /// <param name="deletedBy">User performing the deletion</param>
    void SoftDelete(TEntity entity, int deletedBy);

    /// <summary>
    /// Soft deletes entities by predicate
    /// </summary>
    /// <param name="predicate">Predicate expression</param>
    /// <param name="deletedBy">User performing the deletion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities soft deleted</returns>
    Task<int> SoftDeleteAsync(Expression<Func<TEntity, bool>> predicate, int deletedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft deleted entity
    /// </summary>
    /// <param name="entity">Entity to restore</param>
    /// <param name="restoredBy">User performing the restoration</param>
    void Restore(TEntity entity, int restoredBy);

    /// <summary>
    /// Bulk updates entities
    /// </summary>
    /// <param name="predicate">Predicate to match entities</param>
    /// <param name="updateExpression">Update expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities updated</returns>
    Task<int> BulkUpdateAsync(Expression<Func<TEntity, bool>> predicate,
    Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> updateExpression,
    CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk deletes entities
    /// </summary>
    /// <param name="predicate">Predicate to match entities</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities deleted</returns>
    Task<int> BulkDeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a raw SQL query
    /// </summary>
    /// <param name="sql">SQL query</param>
    /// <param name="parameters">Query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of entities</returns>
    Task<List<TEntity>> FromSqlAsync(string sql, object[] parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a stored procedure
    /// </summary>
    /// <param name="procedureName">Stored procedure name</param>
    /// <param name="parameters">Parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of entities</returns>
    Task<List<TEntity>> ExecuteStoredProcedureAsync(string procedureName,
        Dictionary<string, object> parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with tracking disabled (for read-only scenarios)
    /// </summary>
    /// <param name="predicate">Optional predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of entities (not tracked)</returns>
    Task<List<TEntity>> GetAsNoTrackingAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Projects entities to a different type
    /// </summary>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="selector">Projection selector</param>
    /// <param name="predicate">Optional predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of projected results</returns>
    Task<List<TResult>> ProjectAsync<TResult>(Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Groups entities and projects the result
    /// </summary>
    /// <typeparam name="TKey">Group key type</typeparam>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="keySelector">Key selector for grouping</param>
    /// <param name="resultSelector">Result selector for projection</param>
    /// <param name="predicate">Optional predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of grouped and projected results</returns>
    Task<List<TResult>> GroupByAsync<TKey, TResult>(
        Expression<Func<TEntity, TKey>> keySelector,
        Expression<Func<IGrouping<TKey, TEntity>, TResult>> resultSelector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if entity with ID exists
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if entity exists</returns>
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets maximum value of a property
    /// </summary>
    /// <typeparam name="TProperty">Property type</typeparam>
    /// <param name="selector">Property selector</param>
    /// <param name="predicate">Optional predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Maximum value</returns>
    Task<TProperty?> MaxAsync<TProperty>(Expression<Func<TEntity, TProperty>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets minimum value of a property
    /// </summary>
    /// <typeparam name="TProperty">Property type</typeparam>
    /// <param name="selector">Property selector</param>
    /// <param name="predicate">Optional predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Minimum value</returns>
    Task<TProperty?> MinAsync<TProperty>(Expression<Func<TEntity, TProperty>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sum of a numeric property
    /// </summary>
    /// <param name="selector">Property selector</param>
    /// <param name="predicate">Optional predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sum value</returns>
    Task<decimal> SumAsync(Expression<Func<TEntity, decimal>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets average of a numeric property
    /// </summary>
    /// <param name="selector">Property selector</param>
    /// <param name="predicate">Optional predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Average value</returns>
    Task<decimal> AverageAsync(Expression<Func<TEntity, decimal>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reloads an entity from the database
    /// </summary>
    /// <param name="entity">Entity to reload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task ReloadAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detaches an entity from the context
    /// </summary>
    /// <param name="entity">Entity to detach</param>
    void Detach(TEntity entity);

    /// <summary>
    /// Attaches an entity to the context
    /// </summary>
    /// <param name="entity">Entity to attach</param>
    void Attach(TEntity entity);
}