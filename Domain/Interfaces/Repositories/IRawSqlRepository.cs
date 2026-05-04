namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Narrowed interface exposing only raw-SQL escape-hatch methods, kept separate from the
/// core CRUD contract so callers that need raw SQL declare that dependency explicitly.
/// </summary>
/// <typeparam name="TEntity">Entity type returned by the query</typeparam>
public interface IRawSqlRepository<TEntity>
{
    /// <summary>
    /// Executes a raw SQL query and materialises the result as a list of entities.
    /// </summary>
    Task<List<TEntity>> FromSqlAsync(string sql, object[] parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a stored procedure and materialises the result as a list of entities.
    /// </summary>
    Task<List<TEntity>> ExecuteStoredProcedureAsync(string procedureName,
        Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
}
