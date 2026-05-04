using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Full Unit of Work — composes all domain-scoped contexts and adds infrastructure-level
/// operations (raw SQL, savepoints, diagnostics, bulk ops).
/// Handlers that only touch a single domain should prefer the narrower context interface
/// (IUserContext, IVisitorContext, IInvitationContext, ISecurityContext, IAdminContext).
/// </summary>
public interface IUnitOfWork
    : IUserContext,
      IVisitorContext,
      IInvitationContext,
      ISecurityContext,
      IAdminContext,
      IDisposable
{
    /// <summary>Gets a generic repository for any entity type.</summary>
    IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity;

    // -------------------------------------------------------------------------
    // Raw SQL (infrastructure escape hatch — prefer EF LINQ or specifications)
    // -------------------------------------------------------------------------

    Task<int> ExecuteSqlAsync(string sql, object[] parameters, CancellationToken cancellationToken = default);

    Task<T> ExecuteScalarAsync<T>(string sql, object[] parameters, CancellationToken cancellationToken = default);

    // -------------------------------------------------------------------------
    // Change tracking
    // -------------------------------------------------------------------------

    bool HasChanges();
    void DiscardChanges();

    Task ReloadEntityAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : BaseEntity;

    void DetachEntity<TEntity>(TEntity entity) where TEntity : BaseEntity;
    void AttachEntity<TEntity>(TEntity entity) where TEntity : BaseEntity;

    // -------------------------------------------------------------------------
    // Context tuning
    // -------------------------------------------------------------------------

    void SetCommandTimeout(int timeout);
    void SetChangeTrackingEnabled(bool enabled);
    void SetQueryTrackingEnabled(bool enabled);

    // -------------------------------------------------------------------------
    // Database lifecycle
    // -------------------------------------------------------------------------

    Task MigrateAsync(CancellationToken cancellationToken = default);
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
    string GetConnectionString();

    // -------------------------------------------------------------------------
    // Savepoints (nested transaction support)
    // -------------------------------------------------------------------------

    Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default);
    Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default);
    Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default);

    // -------------------------------------------------------------------------
    // Diagnostics and auditing
    // -------------------------------------------------------------------------

    EntityEntryInfo GetEntityEntry<TEntity>(TEntity entity) where TEntity : BaseEntity;
    List<PendingChange> GetPendingChanges();
    List<EntityValidationResult> ValidateEntities();
    void SetupAuditContext(int? userId, string? ipAddress, string? userAgent, string? correlationId);
    PerformanceMetrics GetPerformanceMetrics();

    // -------------------------------------------------------------------------
    // Bulk operations
    // -------------------------------------------------------------------------

    Task<int> BulkOperationAsync<TEntity>(IEnumerable<TEntity> entities, BulkOperation operation,
        CancellationToken cancellationToken = default) where TEntity : BaseEntity;
}

// ---------------------------------------------------------------------------
// Supporting value types (kept here because they are part of the UoW contract)
// ---------------------------------------------------------------------------

public class EntityEntryInfo
{
    public string EntityType { get; set; } = string.Empty;
    public object EntityId { get; set; } = null!;
    public string State { get; set; } = string.Empty;
    public Dictionary<string, object?> OriginalValues { get; set; } = new();
    public Dictionary<string, object?> CurrentValues { get; set; } = new();
    public List<string> ModifiedProperties { get; set; } = new();
    public bool HasChanges { get; set; }
}

public class PendingChange
{
    public string EntityType { get; set; } = string.Empty;
    public object EntityId { get; set; } = null!;
    public string Operation { get; set; } = string.Empty;
    public Dictionary<string, object?> Changes { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class EntityValidationResult
{
    public object Entity { get; set; } = null!;
    public string EntityType { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
}

public enum BulkOperation { Insert, Update, Delete, Merge }

public class PerformanceMetrics
{
    public int QueriesExecuted { get; set; }
    public TimeSpan TotalQueryTime { get; set; }
    public int EntitiesTracked { get; set; }
    public int ChangesDetected { get; set; }
    public long MemoryUsage { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;
    public List<QueryMetric> QueryMetrics { get; set; } = new();
}

public class QueryMetric
{
    public string CommandText { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public int RecordsAffected { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}
