using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.ValueObjects;
using VisitorManagementSystem.Api.Infrastructure.Data.Seeds;

namespace VisitorManagementSystem.Api.Infrastructure.Data;

/// <summary>
/// Database initializer for seeding initial data
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Initializes the database with seed data
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="serviceProvider">Service provider for dependency injection</param>
    /// <returns>Task</returns>
    public static async Task InitializeAsync(ApplicationDbContext context, IServiceProvider serviceProvider)
    {
        try
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Check if database is already seeded
            if (await context.Users.AnyAsync())
            {
                // Check if system configurations are seeded
                if (!await context.SystemConfigurations.AnyAsync())
                {
                    var logger = serviceProvider.GetService<ILogger<ApplicationDbContext>>();
                    logger?.LogInformation("Users exist but system configurations missing. Migrating ALL configurations...");
                    
                    // Find or create a system admin user for seeding
                    var adminUser = await context.Users
                        .Where(u => u.Role == Domain.Enums.UserRole.Administrator)
                        .FirstOrDefaultAsync();
                    
                    if (adminUser == null)
                    {
                        logger?.LogWarning("No admin user found. Creating system admin for configuration seeding...");
                        await SeedUsersAsync(context);
                        await context.SaveChangesAsync();
                    }
                    
                    await ComprehensiveConfigurationSeeder.SeedAllConfigurationsAsync(context, serviceProvider);
                }
                return; // Database has already been seeded
            }

            // Seed data in order of dependencies
            await SeedUsersAsync(context);
            await context.SaveChangesAsync(); // Save users first
            
            await SeedAlertEscalationsAsync(context);
            await context.SaveChangesAsync(); // Save alert escalations

            await SeedVisitPurposesAsync(context);
            await context.SaveChangesAsync(); // Save visit purposes

            await SeedSystemConfigAsync(context, serviceProvider);
            await context.SaveChangesAsync(); // Save configurations

        }
        catch (Exception ex)
        {
            // Log the exception
            var logger = serviceProvider.GetService<ILogger<ApplicationDbContext>>();
            logger?.LogError(ex, "An error occurred while initializing the database");
            throw;
        }
    }

    /// <summary>
    /// Seeds initial users
    /// </summary>
    /// <param name="context">Database context</param>
    /// <returns>Task</returns>
    private static async Task SeedUsersAsync(ApplicationDbContext context)
    {
        var users = UserSeeder.GetSeedUsers();

        foreach (var user in users)
        {
            await context.Users.AddAsync(user);
        }
    }

    /// <summary>
    /// Seeds alert escalation rules
    /// </summary>
    /// <param name="context">Database context</param>
    /// <returns>Task</returns>
    private static async Task SeedAlertEscalationsAsync(ApplicationDbContext context)
    {
        if (await context.AlertEscalations.AnyAsync())
        {
            return; // Alert escalations already seeded
        }

        var escalations = AlertEscalationSeeder.GetSeedAlertEscalations();

        foreach (var escalation in escalations)
        {
            await context.AlertEscalations.AddAsync(escalation);
        }
    }

    /// <summary>
    /// Seeds initial visit purposes
    /// </summary>
    /// <param name="context">Database context</param>
    private static async Task SeedVisitPurposesAsync(ApplicationDbContext context)
    {
        if (await context.VisitPurposes.AnyAsync())
        {
            return; // Already seeded
        }

        var visitPurposes = VisitPurposeSeeder.GetSeedVisitPurposes();

        foreach (var purpose in visitPurposes)
        {
            await context.VisitPurposes.AddAsync(purpose);
        }
    }

    private static async Task SeedSystemConfigAsync(ApplicationDbContext context, IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetService<ILogger<ApplicationDbContext>>();
        
        try
        {
            // Seed ALL configurations from appsettings.json to database
            await ComprehensiveConfigurationSeeder.SeedAllConfigurationsAsync(context, serviceProvider);
            
            logger?.LogInformation("System configurations migrated successfully from appsettings.json");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error migrating system configurations");
            throw;
        }

        // System configuration seeding would go here
        // For now, we'll add basic audit log entries
        var systemUser = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Administrator);
        if (systemUser != null)
        {
            var auditLog = AuditLog.CreateSuccessEntry(
                EventType.SystemConfiguration,
                "System",
                null,
                "Database Initialized",
                "Initial database setup completed with configuration migration",
                systemUser.Id,
                "127.0.0.1",
                "System Initializer"
            );

            await context.AuditLogs.AddAsync(auditLog);
        }
    }

    /// <summary>
    /// Creates a migration if needed
    /// </summary>
    /// <param name="context">Database context</param>
    /// <returns>Task</returns>
    public static async Task MigrateAsync(ApplicationDbContext context)
    {
        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to migrate database", ex);
        }
    }

    /// <summary>
    /// Resets the database (USE WITH EXTREME CAUTION)
    /// </summary>
    /// <param name="context">Database context</param>
    /// <returns>Task</returns>
    public static async Task ResetDatabaseAsync(ApplicationDbContext context)
    {
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Checks database health
    /// </summary>
    /// <param name="context">Database context</param>
    /// <returns>Database health status</returns>
    public static async Task<DatabaseHealthStatus> CheckDatabaseHealthAsync(ApplicationDbContext context)
    {
        var health = new DatabaseHealthStatus
        {
            CheckTime = DateTime.UtcNow
        };

        try
        {
            // Check connection
            health.CanConnect = await context.Database.CanConnectAsync();

            if (health.CanConnect)
            {
                // Check if tables exist
                var tableCount = await context.Database.ExecuteSqlRawAsync(
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'");
                health.TablesExist = tableCount > 0;

                // Check if seeded
                health.IsSeeded = await context.Users.AnyAsync();

                // Get record counts
                if (health.IsSeeded)
                {
                    health.UserCount = await context.Users.CountAsync();
                    health.AuditLogCount = await context.AuditLogs.CountAsync();
                    health.RefreshTokenCount = await context.RefreshTokens.CountAsync();
                    health.SystemConfigurationCount = await context.SystemConfigurations.CountAsync();
                }

                health.IsHealthy = health.TablesExist && health.IsSeeded;
            }
        }
        catch (Exception ex)
        {
            health.IsHealthy = false;
            health.ErrorMessage = ex.Message;
        }

        return health;
    }
}

/// <summary>
/// Database health status information
/// </summary>
public class DatabaseHealthStatus
{
    public DateTime CheckTime { get; set; }
    public bool IsHealthy { get; set; }
    public bool CanConnect { get; set; }
    public bool TablesExist { get; set; }
    public bool IsSeeded { get; set; }
    public int UserCount { get; set; }
    public int AuditLogCount { get; set; }
    public int RefreshTokenCount { get; set; }
    public int SystemConfigurationCount { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan CheckDuration { get; set; }

    public Dictionary<string, object> GetHealthData()
    {
        return new Dictionary<string, object>
        {
            ["IsHealthy"] = IsHealthy,
            ["CanConnect"] = CanConnect,
            ["TablesExist"] = TablesExist,
            ["IsSeeded"] = IsSeeded,
            ["UserCount"] = UserCount,
            ["AuditLogCount"] = AuditLogCount,
            ["RefreshTokenCount"] = RefreshTokenCount,
            ["CheckTime"] = CheckTime,
            ["ErrorMessage"] = ErrorMessage ?? string.Empty
        };
    }
}