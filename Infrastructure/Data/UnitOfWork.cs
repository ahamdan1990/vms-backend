using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Infrastructure.Data;

/// <summary>
/// Unit of Work implementation for managing database transactions and repositories
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly Dictionary<Type, object> _repositories;
    private bool _disposed;

    // Repository properties - Authentication & System
    public IUserRepository Users { get; private set; }
    public IRefreshTokenRepository RefreshTokens { get; private set; }
    public IGenericRepository<AuditLog> AuditLogs { get; private set; }
    public ISystemConfigurationRepository SystemConfigurations { get; private set; }
    public IConfigurationAuditRepository ConfigurationAudits { get; private set; }

    // Repository properties - Visitor Domain
    public IVisitorRepository Visitors { get; private set; }
    public IVisitorAccessRepository VisitorAccess { get; private set; }
    public IVisitorDocumentRepository VisitorDocuments { get; private set; }
    public IVisitorNoteRepository VisitorNotes { get; private set; }
    public IEmergencyContactRepository EmergencyContacts { get; private set; }
    public IVisitPurposeRepository VisitPurposes { get; private set; }
    public ILocationRepository Locations { get; private set; }
    public ICameraRepository Cameras { get; private set; }
    public IInvitationRepository Invitations { get; private set; }

    // Repository properties - Notification System
    public INotificationAlertRepository NotificationAlerts { get; private set; }
    public IOperatorSessionRepository OperatorSessions { get; private set; }
    public IAlertEscalationRepository AlertEscalations { get; private set; }

    // Repository properties - Permission System
    public IRolePermissionRepository RolePermissions { get; private set; }
    public IRoleRepository Roles { get; private set; }
    public IPermissionRepository Permissions { get; private set; }

    public UnitOfWork(ApplicationDbContext context,
                     IUserRepository userRepository,
                     IRefreshTokenRepository refreshTokenRepository,
                     ISystemConfigurationRepository systemConfigurationRepository,
                     IConfigurationAuditRepository configurationAuditRepository,
                     IVisitorRepository visitorRepository,
                     IVisitorAccessRepository visitorAccessRepository,
                     IVisitorDocumentRepository visitorDocumentRepository,
                     IVisitorNoteRepository visitorNoteRepository,
                     IEmergencyContactRepository emergencyContactRepository,
                     IVisitPurposeRepository visitPurposeRepository,
                     ILocationRepository locationRepository,
                     ICameraRepository cameraRepository,
                     IInvitationRepository invitationRepository,
                     INotificationAlertRepository notificationAlertRepository,
                     IOperatorSessionRepository operatorSessionRepository,
                     IAlertEscalationRepository alertEscalationRepository,
                     IRolePermissionRepository rolePermissionRepository,
                     IRoleRepository roleRepository,
                     IPermissionRepository permissionRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _repositories = new Dictionary<Type, object>();

        // Initialize system repositories
        Users = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        RefreshTokens = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        AuditLogs = new Repositories.BaseRepository<AuditLog>(context);
        SystemConfigurations = systemConfigurationRepository ?? throw new ArgumentNullException(nameof(systemConfigurationRepository));
        ConfigurationAudits = configurationAuditRepository ?? throw new ArgumentNullException(nameof(configurationAuditRepository));

        // Initialize visitor domain repositories
        Visitors = visitorRepository ?? throw new ArgumentNullException(nameof(visitorRepository));
        VisitorAccess = visitorAccessRepository ?? throw new ArgumentNullException(nameof(visitorAccessRepository));
        VisitorDocuments = visitorDocumentRepository ?? throw new ArgumentNullException(nameof(visitorDocumentRepository));
        VisitorNotes = visitorNoteRepository ?? throw new ArgumentNullException(nameof(visitorNoteRepository));
        EmergencyContacts = emergencyContactRepository ?? throw new ArgumentNullException(nameof(emergencyContactRepository));
        VisitPurposes = visitPurposeRepository ?? throw new ArgumentNullException(nameof(visitPurposeRepository));
        Locations = locationRepository ?? throw new ArgumentNullException(nameof(locationRepository));
        Cameras = cameraRepository ?? throw new ArgumentNullException(nameof(cameraRepository));
        Invitations = invitationRepository ?? throw new ArgumentNullException(nameof(invitationRepository));

        // Initialize notification repositories
        NotificationAlerts = notificationAlertRepository ?? throw new ArgumentNullException(nameof(notificationAlertRepository));
        OperatorSessions = operatorSessionRepository ?? throw new ArgumentNullException(nameof(operatorSessionRepository));
        AlertEscalations = alertEscalationRepository ?? throw new ArgumentNullException(nameof(alertEscalationRepository));

        // Initialize permission system repositories
        RolePermissions = rolePermissionRepository ?? throw new ArgumentNullException(nameof(rolePermissionRepository));
        Roles = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        Permissions = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
    }

    public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity
    {
        if (_repositories.ContainsKey(typeof(TEntity)))
        {
            return (IGenericRepository<TEntity>)_repositories[typeof(TEntity)];
        }

        var repository = new Repositories.BaseRepository<TEntity>(_context);
        _repositories.Add(typeof(TEntity), repository);
        return repository;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Handle concurrency conflicts
            foreach (var entry in ex.Entries)
            {
                if (entry.Entity is BaseEntity)
                {
                    var proposedValues = entry.CurrentValues;
                    var databaseValues = entry.GetDatabaseValues();

                    if (databaseValues == null)
                    {
                        // Entity was deleted by another user
                        throw new InvalidOperationException("The entity was deleted by another user.");
                    }

                    // Reload the entity from database
                    entry.OriginalValues.SetValues(databaseValues);
                }
            }

            // Retry the save operation
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Log the exception and rethrow
            throw;
        }
    }

    public int SaveChanges()
    {
        try
        {
            return _context.SaveChanges();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Handle concurrency conflicts
            foreach (var entry in ex.Entries)
            {
                if (entry.Entity is BaseEntity)
                {
                    var proposedValues = entry.CurrentValues;
                    var databaseValues = entry.GetDatabaseValues();

                    if (databaseValues == null)
                    {
                        throw new InvalidOperationException("The entity was deleted by another user.");
                    }

                    entry.OriginalValues.SetValues(databaseValues);
                }
            }

            return _context.SaveChanges();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public IDbContextTransaction BeginTransaction()
    {
        return _context.Database.BeginTransaction();
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.CurrentTransaction.CommitAsync(cancellationToken);
        }
    }

    public void CommitTransaction()
    {
        _context.Database.CurrentTransaction?.Commit();
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.CurrentTransaction.RollbackAsync(cancellationToken);
        }
    }

    public void RollbackTransaction()
    {
        _context.Database.CurrentTransaction?.Rollback();
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> func, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await func();
                await CommitTransactionAsync(cancellationToken);
                return result;
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await BeginTransactionAsync(cancellationToken);
            try
            {
                await action();
                await CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<int> ExecuteSqlAsync(string sql, object[] parameters, CancellationToken cancellationToken = default)
    {
        return await _context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
    }

    public async Task<T> ExecuteScalarAsync<T>(string sql, object[] parameters, CancellationToken cancellationToken = default)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"@p{i}";
            parameter.Value = parameters[i] ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return (T)(result ?? default(T)!);
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    public bool HasChanges()
    {
        return _context.ChangeTracker.HasChanges();
    }

    public void DiscardChanges()
    {
        _context.DetachAllEntities();
    }

    public async Task ReloadEntityAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : BaseEntity
    {
        await _context.Entry(entity).ReloadAsync(cancellationToken);
    }

    public void DetachEntity<TEntity>(TEntity entity) where TEntity : BaseEntity
    {
        _context.Entry(entity).State = EntityState.Detached;
    }

    public void AttachEntity<TEntity>(TEntity entity) where TEntity : BaseEntity
    {
        _context.Entry(entity).State = EntityState.Unchanged;
    }

    public IDbContextTransaction? CurrentTransaction => _context.Database.CurrentTransaction;

    public void SetCommandTimeout(int timeout)
    {
        _context.Database.SetCommandTimeout(timeout);
    }

    public void SetChangeTrackingEnabled(bool enabled)
    {
        _context.ChangeTracker.AutoDetectChangesEnabled = enabled;
    }

    public void SetQueryTrackingEnabled(bool enabled)
    {
        _context.ChangeTracker.QueryTrackingBehavior = enabled ?
            QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.MigrateAsync(cancellationToken);
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Database.CanConnectAsync(cancellationToken);
    }

    public string GetConnectionString()
    {
        return _context.Database.GetConnectionString() ?? string.Empty;
    }

    public async Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.CurrentTransaction.CreateSavepointAsync(name, cancellationToken);
        }
    }

    public async Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.CurrentTransaction.RollbackToSavepointAsync(name, cancellationToken);
        }
    }

    public async Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.CurrentTransaction.ReleaseSavepointAsync(name, cancellationToken);
        }
    }

    public EntityEntryInfo GetEntityEntry<TEntity>(TEntity entity) where TEntity : BaseEntity
    {
        var entry = _context.Entry(entity);

        return new EntityEntryInfo
        {
            EntityType = typeof(TEntity).Name,
            EntityId = entity.Id,
            State = entry.State.ToString(),
            OriginalValues = entry.OriginalValues.Properties.ToDictionary(p => p.Name, p => entry.OriginalValues[p]),
            CurrentValues = entry.CurrentValues.Properties.ToDictionary(p => p.Name, p => entry.CurrentValues[p]),
            ModifiedProperties = entry.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name).ToList(),
            HasChanges = entry.State == EntityState.Modified || entry.State == EntityState.Added || entry.State == EntityState.Deleted
        };
    }

    public List<PendingChange> GetPendingChanges()
    {
        return _context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .Select(e => new PendingChange
            {
                EntityType = e.Entity.GetType().Name,
                EntityId = e.Entity is BaseEntity baseEntity ? (object)baseEntity.Id : "Unknown",
                Operation = e.State.ToString(),
                Changes = e.State == EntityState.Modified ?
                    e.Properties.Where(p => p.IsModified).ToDictionary(p => p.Metadata.Name, p => p.CurrentValue) :
                    new Dictionary<string, object?>(),
                Timestamp = DateTime.UtcNow
            })
            .ToList();
    }

    public List<EntityValidationResult> ValidateEntities()
    {
        var results = new List<EntityValidationResult>();

        foreach (var entry in _context.ChangeTracker.Entries())
        {
            var validationResult = new EntityValidationResult
            {
                Entity = entry.Entity,
                EntityType = entry.Entity.GetType().Name,
                IsValid = true
            };

            // Basic validation logic
            if (entry.Entity is BaseEntity baseEntity)
            {
                if (baseEntity.Id < 0)
                {
                    validationResult.IsValid = false;
                    validationResult.ValidationErrors.Add("ID cannot be negative");
                }
            }

            results.Add(validationResult);
        }

        return results;
    }

    public async Task<int> BulkOperationAsync<TEntity>(IEnumerable<TEntity> entities, BulkOperation operation,
        CancellationToken cancellationToken = default) where TEntity : BaseEntity
    {
        switch (operation)
        {
            case BulkOperation.Insert:
                await _context.Set<TEntity>().AddRangeAsync(entities, cancellationToken);
                break;
            case BulkOperation.Update:
                _context.Set<TEntity>().UpdateRange(entities);
                break;
            case BulkOperation.Delete:
                _context.Set<TEntity>().RemoveRange(entities);
                break;
            case BulkOperation.Merge:
                // Simple merge implementation: update existing, add new
                foreach (var entity in entities)
                {
                    var existingEntity = _context.Set<TEntity>().Find(entity.Id);
                    if (existingEntity != null)
                    {
                        _context.Entry(existingEntity).CurrentValues.SetValues(entity);
                    }
                    else
                    {
                        _context.Set<TEntity>().Add(entity);
                    }
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }

        return await SaveChangesAsync(cancellationToken);
    }

    public void SetupAuditContext(int? userId, string? ipAddress, string? userAgent, string? correlationId)
    {
        // Store audit context information for use during SaveChanges
        // This would typically be stored in a scoped service or context
        _context.ChangeTracker.StateChanged += (sender, e) =>
        {
            // Audit logic would be implemented here
        };
    }

    public PerformanceMetrics GetPerformanceMetrics()
    {
        return new PerformanceMetrics
        {
            QueriesExecuted = 0, // Would track actual queries
            TotalQueryTime = TimeSpan.Zero,
            EntitiesTracked = _context.ChangeTracker.Entries().Count(),
            ChangesDetected = _context.ChangeTracker.Entries().Count(e => e.State != EntityState.Unchanged),
            StartTime = DateTime.UtcNow,
            QueryMetrics = new List<QueryMetric>()
        };
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _context.Dispose();
            _repositories.Clear();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}