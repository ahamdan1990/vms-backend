using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Infrastructure.Data.Configurations;
using VisitorManagementSystem.Api.Infrastructure.Data.Configurations.Notifications;
using VisitorManagementSystem.Api.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace VisitorManagementSystem.Api.Infrastructure.Data;

/// <summary>
/// Application database context
/// </summary>
public class ApplicationDbContext : DbContext
{
    private readonly IDomainEventPublisher? _domainEventPublisher;
    private readonly IServiceProvider _serviceProvider;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IServiceProvider serviceProvider)
        : base(options)
    {
        _serviceProvider = serviceProvider;
        _domainEventPublisher = serviceProvider.GetService<IDomainEventPublisher>();
    }

    // DbSets - Authentication & System
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<SystemConfiguration> SystemConfigurations { get; set; } = null!;
    public DbSet<ConfigurationAudit> ConfigurationAudits { get; set; } = null!;

    // DbSets - Permission System (New)
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<PermissionChangeAuditLog> PermissionChangeAuditLogs { get; set; } = null!;

    // DbSets - Visitor Domain
    public DbSet<Visitor> Visitors { get; set; } = null!;
    public DbSet<VisitorAccess> VisitorAccess { get; set; } = null!;
    public DbSet<VisitorDocument> VisitorDocuments { get; set; } = null!;
    public DbSet<VisitorNote> VisitorNotes { get; set; } = null!;
    public DbSet<EmergencyContact> EmergencyContacts { get; set; } = null!;
    public DbSet<VisitPurpose> VisitPurposes { get; set; } = null!;
    public DbSet<Location> Locations { get; set; } = null!;
    public DbSet<Camera> Cameras { get; set; } = null!;

    // DbSets - Capacity Management
    public DbSet<TimeSlot> TimeSlots { get; set; } = null!;
    public DbSet<TimeSlotBooking> TimeSlotBookings { get; set; } = null!;
    public DbSet<OccupancyLog> OccupancyLogs { get; set; } = null!;

    // DbSets - Invitation Domain
    public DbSet<Invitation> Invitations { get; set; } = null!;
    public DbSet<InvitationApproval> InvitationApprovals { get; set; } = null!;
    public DbSet<InvitationEvent> InvitationEvents { get; set; } = null!;
    public DbSet<InvitationTemplate> InvitationTemplates { get; set; } = null!;

    // DbSets - Notification System
    public DbSet<NotificationAlert> NotificationAlerts { get; set; } = null!;
    public DbSet<OperatorSession> OperatorSessions { get; set; } = null!;
    public DbSet<AlertEscalation> AlertEscalations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations - System
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new SystemConfigurationConfiguration());
        modelBuilder.ApplyConfiguration(new ConfigurationAuditConfiguration());

        // Apply all configurations - Permission System (New)
        modelBuilder.ApplyConfiguration(new PermissionConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new RolePermissionConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionChangeAuditLogConfiguration());

        // Apply all configurations - Visitor Domain
        modelBuilder.ApplyConfiguration(new VisitorConfiguration());
        modelBuilder.ApplyConfiguration(new VisitorAccessConfiguration());
        modelBuilder.ApplyConfiguration(new VisitorDocumentConfiguration());
        modelBuilder.ApplyConfiguration(new VisitorNoteConfiguration());
        modelBuilder.ApplyConfiguration(new EmergencyContactConfiguration());
        modelBuilder.ApplyConfiguration(new VisitPurposeConfiguration());
        modelBuilder.ApplyConfiguration(new LocationConfiguration());
        modelBuilder.ApplyConfiguration(new CameraConfiguration());

        // Apply all configurations - Capacity Management
        modelBuilder.ApplyConfiguration(new TimeSlotConfiguration());
        modelBuilder.ApplyConfiguration(new TimeSlotBookingConfiguration());
        modelBuilder.ApplyConfiguration(new OccupancyLogConfiguration());

        // Apply all configurations - Invitation Domain
        modelBuilder.ApplyConfiguration(new InvitationConfiguration());
        modelBuilder.ApplyConfiguration(new InvitationApprovalConfiguration());
        modelBuilder.ApplyConfiguration(new InvitationEventConfiguration());
        modelBuilder.ApplyConfiguration(new InvitationTemplateConfiguration());

        // Apply all configurations - Notification System
        modelBuilder.ApplyConfiguration(new NotificationAlertConfiguration());
        modelBuilder.ApplyConfiguration(new OperatorSessionConfiguration());
        modelBuilder.ApplyConfiguration(new AlertEscalationConfiguration());

        // Global query filters for soft delete (standardized on IsDeleted pattern)
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(rt => !rt.User.IsDeleted);

        // Visitor domain soft delete filters (standardized on IsDeleted pattern)
        modelBuilder.Entity<Visitor>().HasQueryFilter(v => !v.IsDeleted);
        modelBuilder.Entity<VisitorAccess>().HasQueryFilter(va => !va.User.IsDeleted); // Filter by User soft-delete
        modelBuilder.Entity<VisitorDocument>().HasQueryFilter(d => !d.IsDeleted);
        modelBuilder.Entity<VisitorNote>().HasQueryFilter(n => !n.IsDeleted);
        modelBuilder.Entity<EmergencyContact>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<VisitPurpose>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Location>().HasQueryFilter(l => !l.IsDeleted);
        modelBuilder.Entity<Camera>().HasQueryFilter(c => !c.IsDeleted);

        // Capacity management soft delete filters
        modelBuilder.Entity<TimeSlot>().HasQueryFilter(ts => !ts.IsDeleted);
        modelBuilder.Entity<TimeSlotBooking>().HasQueryFilter(b => !b.IsDeleted && !b.BookedByUser.IsDeleted);

        // Configure decimal precision globally
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }

        // Configure datetime to be UTC
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
        {
            property.SetColumnType("datetime2");
        }

        // Configure string properties to use nvarchar
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(string)))
        {
            // Skip if already configured as nvarchar(max)
            if (property.GetColumnType()?.Contains("max") == true)
                continue;

            if (property.GetMaxLength() == null)
            {
                property.SetMaxLength(256);
            }
            property.SetColumnType($"nvarchar({property.GetMaxLength()})");
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Enable sensitive data logging in development
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }
        else
        {
            // Suppress MARS warnings in production
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(SqlServerEventId.SavepointsDisabledBecauseOfMARS));
        }

        // Configure query tracking behavior
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);

        // Configure command timeout
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await OnBeforeSaveChanges();
        var result = await base.SaveChangesAsync(cancellationToken);
        await OnAfterSaveChanges();
        return result;
    }

    public override int SaveChanges()
    {
        OnBeforeSaveChanges().GetAwaiter().GetResult();
        var result = base.SaveChanges();
        OnAfterSaveChanges().GetAwaiter().GetResult();
        return result;
    }

    private async Task OnBeforeSaveChanges()
    {
        var entries = ChangeTracker.Entries();

        foreach (var entry in entries)
        {
            if (entry.Entity is BaseEntity baseEntity)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        baseEntity.CreatedOn = DateTime.UtcNow;
                        if (baseEntity is AuditableEntity auditableEntity)
                        {
                            // Set created by from current user context
                            var currentUserId = GetCurrentUserId();
                            if (currentUserId.HasValue)
                            {
                                auditableEntity.CreatedBy = currentUserId.Value;
                            }
                        }
                        break;

                    case EntityState.Modified:
                        baseEntity.UpdateModifiedOn();
                        if (baseEntity is AuditableEntity auditableEntityModified)
                        {
                            // Set modified by from current user context
                            var currentUserId = GetCurrentUserId();
                            if (currentUserId.HasValue)
                            {
                                auditableEntityModified.ModifiedBy = currentUserId.Value;
                            }
                        }
                        break;
                }
            }
        }

        await Task.CompletedTask;
    }

    private async Task OnAfterSaveChanges()
    {
        // Publish domain events if available
        if (_domainEventPublisher != null)
        {
            var domainEvents = GetDomainEvents();
            if (domainEvents.Any())
            {
                await _domainEventPublisher.PublishAsync(domainEvents);
            }
        }
    }

    private List<IDomainEvent> GetDomainEvents()
    {
        var domainEvents = new List<IDomainEvent>();

        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity)
            .ToList();

        foreach (var entry in entries)
        {
            // Here you would extract domain events from entities
            // This is a simplified implementation
            switch (entry.State)
            {
                case EntityState.Added:
                    // Create domain events for entity creation
                    break;
                case EntityState.Modified:
                    // Create domain events for entity modification
                    break;
                case EntityState.Deleted:
                    // Create domain events for entity deletion
                    break;
            }
        }

        return domainEvents;
    }

    private int? GetCurrentUserId()
    {
        // Get current user ID from HTTP context or other user context service
        var httpContextAccessor = _serviceProvider.GetService<IHttpContextAccessor>();

        // FIXED: Use ClaimTypes.NameIdentifier instead of "sub" to match JWT configuration in Program.cs
        // Program.cs sets: NameClaimType = ClaimTypes.NameIdentifier
        var userIdClaim = httpContextAccessor?.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }

    /// <summary>
    /// Detaches all tracked entities
    /// </summary>
    public void DetachAllEntities()
    {
        var changedEntriesCopy = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added ||
                       e.State == EntityState.Modified ||
                       e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in changedEntriesCopy)
        {
            entry.State = EntityState.Detached;
        }
    }

    /// <summary>
    /// Gets entities with pending changes
    /// </summary>
    /// <returns>List of entities with changes</returns>
    public List<object> GetPendingChanges()
    {
        return ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added ||
                       e.State == EntityState.Modified ||
                       e.State == EntityState.Deleted)
            .Select(e => e.Entity)
            .ToList();
    }

    /// <summary>
    /// Checks if there are any pending changes
    /// </summary>
    /// <returns>True if there are pending changes</returns>
    public bool HasPendingChanges()
    {
        return ChangeTracker.HasChanges();
    }

    /// <summary>
    /// Sets the command timeout for the context
    /// </summary>
    /// <param name="timeout">Timeout in seconds</param>
    public void SetCommandTimeout(int timeout)
    {
        Database.SetCommandTimeout(timeout);
    }

    /// <summary>
    /// Begins a transaction
    /// </summary>
    /// <returns>Database transaction</returns>
    public async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync()
    {
        return await Database.BeginTransactionAsync();
    }

    /// <summary>
    /// Executes raw SQL
    /// </summary>
    /// <param name="sql">SQL command</param>
    /// <param name="parameters">Parameters</param>
    /// <returns>Number of affected rows</returns>
    public async Task<int> ExecuteSqlAsync(string sql, params object[] parameters)
    {
        return await Database.ExecuteSqlRawAsync(sql, parameters);
    }

    /// <summary>
    /// Gets database connection string
    /// </summary>
    /// <returns>Connection string</returns>
    public string GetConnectionString()
    {
        return Database.GetConnectionString() ?? string.Empty;
    }

    /// <summary>
    /// Checks if database can connect
    /// </summary>
    /// <returns>True if can connect</returns>
    public async Task<bool> CanConnectAsync()
    {
        try
        {
            return await Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Migrates the database
    /// </summary>
    public async Task MigrateAsync()
    {
        await Database.MigrateAsync();
    }

    /// <summary>
    /// Ensures database is created
    /// </summary>
    public async Task EnsureCreatedAsync()
    {
        await Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Resets all database data - ONLY ALLOWED IN DEVELOPMENT ENVIRONMENT
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when attempting to reset database in non-development environment</exception>
    public async Task ResetDatabaseAsync()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        if (environment != "Development")
        {
            throw new InvalidOperationException(
                "Database reset is not allowed in non-development environments. " +
                "This operation would result in permanent data loss and is blocked for security.");
        }

        await Database.EnsureDeletedAsync();
        await Database.EnsureCreatedAsync();
    }
}