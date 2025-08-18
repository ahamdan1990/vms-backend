using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.Specifications;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Base repository implementation for common CRUD operations
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
public class BaseRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseEntity
{

    public async Task<List<TEntity>> GetAllIncludingAsync(
    CancellationToken cancellationToken = default,
    params Expression<Func<TEntity, object>>[] includes
)
    {
        IQueryable<TEntity> query = _dbSet;

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.ToListAsync(cancellationToken);
    }


    /// <summary>
    /// Gets queryable for advanced querying (use with caution)
    /// </summary>
    /// <returns>IQueryable of entities</returns>
    public virtual IQueryable<TEntity> GetQueryable()
    {
        return _dbSet.AsQueryable();
    }

    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public BaseRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<TEntity?> GetByIdAsync(int id, params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> query = _dbSet;

        if (includes != null)
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
        }

        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public virtual async Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.IsActive).ToListAsync(cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetAsync(BaseSpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> GetSingleAsync(BaseSpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetAsync<TKey>(Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>> orderBy, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).OrderBy(orderBy).ToListAsync(cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetDescendingAsync<TKey>(Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>> orderByDesc, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).OrderByDescending(orderByDesc).ToListAsync(cancellationToken);
    }

    public virtual async Task<(List<TEntity> Entities, int TotalCount)> GetPagedAsync<TKey>(int pageIndex, int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, TKey>>? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;

        if (predicate != null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync(cancellationToken);

        if (orderBy != null)
            query = query.OrderBy(orderBy);

        var entities = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (entities, totalCount);
    }

    public virtual async Task<(List<TEntity> Entities, int TotalCount)> GetPagedAsync(BaseSpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        var countQuery = ApplySpecificationWithoutPaging(specification);

        var totalCount = await countQuery.CountAsync(cancellationToken);
        var entities = await query.ToListAsync(cancellationToken);

        return (entities, totalCount);
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(cancellationToken);
    }

    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(predicate, cancellationToken);
    }

    public virtual async Task<int> CountAsync(BaseSpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecificationWithoutPaging(specification).CountAsync(cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(BaseSpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecificationWithoutPaging(specification).AnyAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.SingleOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public virtual void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    public virtual void UpdateRange(IEnumerable<TEntity> entities)
    {
        _dbSet.UpdateRange(entities);
    }

    public virtual void Remove(TEntity entity)
    {
        _dbSet.Remove(entity);
    }

    /// <summary>
    /// Delete method alias for consistency with handlers
    /// </summary>
    public virtual void Delete(TEntity entity)
    {
        Remove(entity);
    }

    public virtual async Task RemoveAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            Remove(entity);
        }
    }

    public virtual void RemoveRange(IEnumerable<TEntity> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    public virtual async Task<int> RemoveAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var entities = await _dbSet.Where(predicate).ToListAsync(cancellationToken);
        _dbSet.RemoveRange(entities);
        return entities.Count;
    }

    public virtual void SoftDelete(TEntity entity, int deletedBy)
    {
        if (entity is SoftDeleteEntity softDeleteEntity)
        {
            softDeleteEntity.SoftDelete(deletedBy);
            Update(entity);
        }
        else
        {
            entity.Deactivate();
            Update(entity);
        }
    }

    public virtual async Task<int> SoftDeleteAsync(Expression<Func<TEntity, bool>> predicate, int deletedBy, CancellationToken cancellationToken = default)
    {
        var entities = await _dbSet.Where(predicate).ToListAsync(cancellationToken);
        foreach (var entity in entities)
        {
            SoftDelete(entity, deletedBy);
        }
        return entities.Count;
    }

    public virtual void Restore(TEntity entity, int restoredBy)
    {
        if (entity is SoftDeleteEntity softDeleteEntity)
        {
            softDeleteEntity.Restore(restoredBy);
            Update(entity);
        }
        else
        {
            entity.Activate();
            Update(entity);
        }
    }

    public virtual async Task<int> BulkUpdateAsync(Expression<Func<TEntity, bool>> predicate,
        // This is the correct type for the update expression
        Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> updateExpression,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ExecuteUpdateAsync(updateExpression, cancellationToken);
    }

    public virtual async Task<int> BulkDeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ExecuteDeleteAsync(cancellationToken);
    }

    public virtual async Task<List<TEntity>> FromSqlAsync(string sql, object[] parameters, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FromSqlRaw(sql, parameters).ToListAsync(cancellationToken);
    }

    public virtual async Task<List<TEntity>> ExecuteStoredProcedureAsync(string procedureName,
        Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        var paramList = parameters.Select(p => $"@{p.Key}").ToArray();
        var sql = $"EXEC {procedureName} {string.Join(",", paramList)}";
        var paramValues = parameters.Values.ToArray();

        return await _dbSet.FromSqlRaw(sql, paramValues).ToListAsync(cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetAsNoTrackingAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<List<TResult>> ProjectAsync<TResult>(Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;

        if (predicate != null)
            query = query.Where(predicate);

        return await query.Select(selector).ToListAsync(cancellationToken);
    }

    public virtual async Task<List<TResult>> GroupByAsync<TKey, TResult>(
        Expression<Func<TEntity, TKey>> keySelector,
        Expression<Func<IGrouping<TKey, TEntity>, TResult>> resultSelector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;

        if (predicate != null)
            query = query.Where(predicate);

        return await query.GroupBy(keySelector).Select(resultSelector).ToListAsync(cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(e => e.Id == id, cancellationToken);
    }

    public virtual async Task<TProperty?> MaxAsync<TProperty>(Expression<Func<TEntity, TProperty>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;

        if (predicate != null)
            query = query.Where(predicate);

        return await query.MaxAsync(selector, cancellationToken);
    }

    public virtual async Task<TProperty?> MinAsync<TProperty>(Expression<Func<TEntity, TProperty>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;

        if (predicate != null)
            query = query.Where(predicate);

        return await query.MinAsync(selector, cancellationToken);
    }

    public virtual async Task<decimal> SumAsync(Expression<Func<TEntity, decimal>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;

        if (predicate != null)
            query = query.Where(predicate);

        return await query.SumAsync(selector, cancellationToken);
    }

    public virtual async Task<decimal> AverageAsync(Expression<Func<TEntity, decimal>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;

        if (predicate != null)
            query = query.Where(predicate);

        return await query.AverageAsync(selector, cancellationToken);
    }

    public virtual async Task ReloadAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _context.Entry(entity).ReloadAsync(cancellationToken);
    }

    public virtual void Detach(TEntity entity)
    {
        _context.Entry(entity).State = EntityState.Detached;
    }

    public virtual void Attach(TEntity entity)
    {
        _dbSet.Attach(entity);
    }

    protected virtual IQueryable<TEntity> ApplySpecification(BaseSpecification<TEntity> specification)
    {
        return SpecificationEvaluator.GetQuery(_dbSet, specification);
    }

    protected virtual IQueryable<TEntity> ApplySpecificationWithoutPaging(BaseSpecification<TEntity> specification)
    {
        return SpecificationEvaluator.GetQuery(_dbSet, specification, true);
    }
}

/// <summary>
/// Specification evaluator for applying specifications to queries
/// </summary>
public static class SpecificationEvaluator
{
    public static IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, BaseSpecification<T> specification, bool ignorePaging = false)
        where T : BaseEntity
    {
        var query = inputQuery;

        // Apply criteria
        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // Apply includes
        if (specification.Includes.Any())
        {
            query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
        }

        // Apply string includes
        if (specification.IncludeStrings.Any())
        {
            query = specification.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));
        }

        // Apply ordering
        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        // Apply grouping
        if (specification.GroupBy != null)
        {
            query = query.GroupBy(specification.GroupBy).SelectMany(x => x);
        }

        // Apply paging
        if (specification.IsPagingEnabled && !ignorePaging)
        {
            var skip = specification.Skip!.Value;
            var take = specification.Take!.Value;

            query = query.Skip(skip).Take(take);
        }


        return query;
    }
}

