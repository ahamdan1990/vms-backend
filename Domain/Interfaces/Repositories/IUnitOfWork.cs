using Microsoft.EntityFrameworkCore.Storage;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Unit of Work interface for managing database transactions and repositories
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// User repository
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Refresh token repository
    /// </summary>
    IRefreshTokenRepository RefreshTokens { get; }

    /// <summary>
    /// Audit log repository
    /// </summary>
    IGenericRepository<AuditLog> AuditLogs { get; }

    /// <summary>
    /// System configuration repository
    /// </summary>
    ISystemConfigurationRepository SystemConfigurations { get; }

    /// <summary>
    /// Configuration audit repository
    /// </summary>
    IConfigurationAuditRepository ConfigurationAudits { get; }

    /// <summary>
    /// Visitor repository
    /// </summary>
    IVisitorRepository Visitors { get; }

    /// <summary>
    /// Visitor access repository
    /// </summary>
    IVisitorAccessRepository VisitorAccess { get; }

    /// <summary>
    /// Visitor document repository
    /// </summary>
    IVisitorDocumentRepository VisitorDocuments { get; }

    /// <summary>
    /// Visitor note repository
    /// </summary>
    IVisitorNoteRepository VisitorNotes { get; }

    /// <summary>
    /// Emergency contact repository
    /// </summary>
    IEmergencyContactRepository EmergencyContacts { get; }

    /// <summary>
    /// Visit purpose repository
    /// </summary>
    IVisitPurposeRepository VisitPurposes { get; }

    /// <summary>
    /// Location repository
    /// </summary>
    ILocationRepository Locations { get; }

    /// <summary>
    /// Camera repository
    /// </summary>
    ICameraRepository Cameras { get; }

    /// <summary>
    /// Invitation repository
    /// </summary>
    IInvitationRepository Invitations { get; }

    /// <summary>
    /// Notification alert repository
    /// </summary>
    INotificationAlertRepository NotificationAlerts { get; }

    /// <summary>
    /// Operator session repository
    /// </summary>
    IOperatorSessionRepository OperatorSessions { get; }

    /// <summary>
    /// Alert escalation repository
    /// </summary>
    IAlertEscalationRepository AlertEscalations { get; }

    /// <summary>
    /// Role permission repository
    /// </summary>
    IRolePermissionRepository RolePermissions { get; }

    /// <summary>
    /// Role repository
    /// </summary>
    IRoleRepository Roles { get; }

    /// <summary>
    /// Permission repository
    /// </summary>
    IPermissionRepository Permissions { get; }

    /// <summary>
    /// Gets a generic repository for any entity type
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <returns>Generic repository instance</returns>
    IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity;

    /// <summary>
    /// Saves all changes to the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of affected records</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database synchronously
    /// </summary>
    /// <returns>Number of affected records</returns>
    int SaveChanges();

    /// <summary>
    /// Begins a database transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Database transaction</returns>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a database transaction synchronously
    /// </summary>
    /// <returns>Database transaction</returns>
    IDbContextTransaction BeginTransaction();

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction synchronously
    /// </summary>
    void CommitTransaction();

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction synchronously
    /// </summary>
    void RollbackTransaction();

    /// <summary>
    /// Executes a function within a transaction
    /// </summary>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="func">Function to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the function</returns>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> func, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an action within a transaction
    /// </summary>
    /// <param name="action">Action to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes raw SQL command
    /// </summary>
    /// <param name="sql">SQL command</param>
    /// <param name="parameters">Parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of affected rows</returns>
    Task<int> ExecuteSqlAsync(string sql, object[] parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes raw SQL command and returns scalar result
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="sql">SQL command</param>
    /// <param name="parameters">Parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Scalar result</returns>
    Task<T> ExecuteScalarAsync<T>(string sql, object[] parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if there are any pending changes
    /// </summary>
    /// <returns>True if there are pending changes</returns>
    bool HasChanges();

    /// <summary>
    /// Discards all pending changes
    /// </summary>
    void DiscardChanges();

    /// <summary>
    /// Reloads an entity from the database
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <param name="entity">Entity to reload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task ReloadEntityAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : VisitorManagementSystem.Api.Domain.Entities.BaseEntity;

    /// <summary>
    /// Detaches an entity from the context
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <param name="entity">Entity to detach</param>
    void DetachEntity<TEntity>(TEntity entity) where TEntity : VisitorManagementSystem.Api.Domain.Entities.BaseEntity;

    /// <summary>
    /// Attaches an entity to the context
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <param name="entity">Entity to attach</param>
    void AttachEntity<TEntity>(TEntity entity) where TEntity : VisitorManagementSystem.Api.Domain.Entities.BaseEntity;

    /// <summary>
    /// Gets the current transaction if one exists
    /// </summary>
    /// <returns>Current transaction or null</returns>
    IDbContextTransaction? CurrentTransaction { get; }

    /// <summary>
    /// Sets the command timeout for database operations
    /// </summary>
    /// <param name="timeout">Timeout in seconds</param>
    void SetCommandTimeout(int timeout);

    /// <summary>
    /// Enables or disables change tracking
    /// </summary>
    /// <param name="enabled">Whether to enable change tracking</param>
    void SetChangeTrackingEnabled(bool enabled);

    /// <summary>
    /// Enables or disables query tracking
    /// </summary>
    /// <param name="enabled">Whether to enable query tracking</param>
    void SetQueryTrackingEnabled(bool enabled);

    /// <summary>
    /// Performs database migration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task MigrateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if database can connect
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if can connect</returns>
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets database connection string
    /// </summary>
    /// <returns>Connection string</returns>
    string GetConnectionString();

    /// <summary>
    /// Creates a savepoint in the current transaction
    /// </summary>
    /// <param name="name">Savepoint name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back to a savepoint
    /// </summary>
    /// <param name="name">Savepoint name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a savepoint
    /// </summary>
    /// <param name="name">Savepoint name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entity entry information
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <param name="entity">Entity</param>
    /// <returns>Entity entry information</returns>
    EntityEntryInfo GetEntityEntry<TEntity>(TEntity entity) where TEntity : VisitorManagementSystem.Api.Domain.Entities.BaseEntity;

    /// <summary>
    /// Gets all pending changes
    /// </summary>
    /// <returns>List of pending changes</returns>
    List<PendingChange> GetPendingChanges();

    /// <summary>
    /// Validates all entities in the context
    /// </summary>
    /// <returns>Validation results</returns>
    List<EntityValidationResult> ValidateEntities();

    /// <summary>
    /// Executes a bulk operation
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <param name="entities">Entities to process</param>
    /// <param name="operation">Operation type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of affected entities</returns>
    Task<int> BulkOperationAsync<TEntity>(IEnumerable<TEntity> entities, BulkOperation operation,
        CancellationToken cancellationToken = default) where TEntity : VisitorManagementSystem.Api.Domain.Entities.BaseEntity;

    /// <summary>
    /// Sets up audit logging for the current operation
    /// </summary>
    /// <param name="userId">User performing the operation</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="userAgent">User agent</param>
    /// <param name="correlationId">Correlation ID</param>
    void SetupAuditContext(int? userId, string? ipAddress, string? userAgent, string? correlationId);

    /// <summary>
    /// Gets performance metrics for the current context
    /// </summary>
    /// <returns>Performance metrics</returns>
    PerformanceMetrics GetPerformanceMetrics();
}

/// <summary>
/// Entity entry information
/// </summary>
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

/// <summary>
/// Pending change information
/// </summary>
public class PendingChange
{
    public string EntityType { get; set; } = string.Empty;
    public object EntityId { get; set; } = null!;
    public string Operation { get; set; } = string.Empty;
    public Dictionary<string, object?> Changes { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Entity validation result
/// </summary>
public class EntityValidationResult
{
    public object Entity { get; set; } = null!;
    public string EntityType { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
}

/// <summary>
/// Bulk operation types
/// </summary>
public enum BulkOperation
{
    Insert,
    Update,
    Delete,
    Merge
}

/// <summary>
/// Performance metrics
/// </summary>
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

/// <summary>
/// Individual query metric
/// </summary>
public class QueryMetric
{
    public string CommandText { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public int RecordsAffected { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}