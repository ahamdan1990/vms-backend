using Microsoft.EntityFrameworkCore;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Seeds;

/// <summary>
/// Master seeder that orchestrates all database seeding operations
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seeds all initial data to the database in the correct order
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="isDevelopment">Whether running in development mode</param>
    public static async Task SeedAllAsync(ApplicationDbContext context, bool isDevelopment = false)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("Starting Database Seeding...");
        Console.WriteLine("===========================================");

        try
        {
            // Ensure database is created and migrations are applied
            await context.Database.MigrateAsync();

            // 1. Seed Permissions (must be first)
            await PermissionSeeder.SeedPermissionsToDbAsync(context);

            // 2. Seed Roles (depends on permissions existing)
            await RoleSeeder.SeedRolesToDbAsync(context);

            // 3. Seed Role-Permission Mappings (depends on both roles and permissions)
            await RolePermissionSeeder.SeedRolePermissionsAsync(context);

            // 4. Migrate existing users to new role system
            await UserRoleMigrationSeeder.MigrateUserRolesAsync(context);

            // 5. Validate migration
            var isValid = await UserRoleMigrationSeeder.ValidateMigrationAsync(context);

            if (!isValid)
            {
                Console.WriteLine("WARNING: User role migration validation failed!");
            }

            Console.WriteLine("===========================================");
            Console.WriteLine("Database Seeding Completed Successfully!");
            Console.WriteLine("===========================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("ERROR: Database Seeding Failed!");
            Console.WriteLine($"Exception: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            Console.WriteLine("===========================================");
            throw;
        }
    }

    /// <summary>
    /// Seeds only permission-related data (for updates)
    /// </summary>
    public static async Task SeedPermissionsOnlyAsync(ApplicationDbContext context)
    {
        Console.WriteLine("Seeding permissions only...");

        await PermissionSeeder.SeedPermissionsToDbAsync(context);

        Console.WriteLine("Permission seeding completed.");
    }

    /// <summary>
    /// Re-seeds role permissions (useful after permission changes)
    /// </summary>
    public static async Task ReseedRolePermissionsAsync(ApplicationDbContext context)
    {
        Console.WriteLine("Re-seeding role permissions...");

        // Remove existing role permissions
        var existingRolePermissions = await context.RolePermissions.ToListAsync();
        context.RolePermissions.RemoveRange(existingRolePermissions);
        await context.SaveChangesAsync();

        // Re-seed
        await RolePermissionSeeder.SeedRolePermissionsAsync(context);

        Console.WriteLine("Role permissions re-seeded successfully.");
    }

    /// <summary>
    /// Gets seeding statistics
    /// </summary>
    public static async Task<SeedingStatistics> GetSeedingStatisticsAsync(ApplicationDbContext context)
    {
        return new SeedingStatistics
        {
            TotalPermissions = await context.Permissions.CountAsync(),
            ActivePermissions = await context.Permissions.CountAsync(p => p.IsActive),
            TotalRoles = await context.Roles.CountAsync(),
            ActiveRoles = await context.Roles.CountAsync(r => r.IsActive),
            TotalRolePermissions = await context.RolePermissions.CountAsync(),
            TotalUsers = await context.Users.CountAsync(),
            MigratedUsers = await context.Users.CountAsync(u => u.RoleId != null),
            UnmigratedUsers = await context.Users.CountAsync(u => u.RoleId == null)
        };
    }

    /// <summary>
    /// Prints seeding statistics to console
    /// </summary>
    public static async Task PrintSeedingStatisticsAsync(ApplicationDbContext context)
    {
        var stats = await GetSeedingStatisticsAsync(context);

        Console.WriteLine("\n===========================================");
        Console.WriteLine("Database Seeding Statistics:");
        Console.WriteLine("===========================================");
        Console.WriteLine($"Permissions:      {stats.ActivePermissions}/{stats.TotalPermissions} active");
        Console.WriteLine($"Roles:            {stats.ActiveRoles}/{stats.TotalRoles} active");
        Console.WriteLine($"Role Permissions: {stats.TotalRolePermissions} mappings");
        Console.WriteLine($"Users:            {stats.MigratedUsers}/{stats.TotalUsers} migrated");

        if (stats.UnmigratedUsers > 0)
        {
            Console.WriteLine($"WARNING: {stats.UnmigratedUsers} users not migrated!");
        }

        Console.WriteLine("===========================================\n");
    }
}

/// <summary>
/// Statistics about database seeding
/// </summary>
public class SeedingStatistics
{
    public int TotalPermissions { get; set; }
    public int ActivePermissions { get; set; }
    public int TotalRoles { get; set; }
    public int ActiveRoles { get; set; }
    public int TotalRolePermissions { get; set; }
    public int TotalUsers { get; set; }
    public int MigratedUsers { get; set; }
    public int UnmigratedUsers { get; set; }
}
