using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Seeds;

/// <summary>
/// Seeder to migrate existing users from enum-based Role to database Role FK
/// </summary>
public static class UserRoleMigrationSeeder
{
    /// <summary>
    /// Migrates existing users to use RoleId instead of Role enum
    /// </summary>
    public static async Task MigrateUserRolesAsync(ApplicationDbContext context)
    {
        Console.WriteLine("Migrating user roles to new system...");

        // Get all roles from database
        var roles = await context.Roles.ToDictionaryAsync(r => r.Name, r => r.Id);

        if (!roles.ContainsKey("Staff") || !roles.ContainsKey("Receptionist") || !roles.ContainsKey("Administrator"))
        {
            throw new InvalidOperationException("System roles must be seeded before migrating user roles.");
        }

        // Get all users without RoleId set
        var usersToMigrate = await context.Users
            .Where(u => u.RoleId == null)
            .ToListAsync();

        if (!usersToMigrate.Any())
        {
            Console.WriteLine("No users to migrate. Skipping...");
            return;
        }

        var migratedCount = 0;

        foreach (var user in usersToMigrate)
        {
            // Map old enum Role to new RoleId
            var newRoleId = user.Role switch
            {
                UserRole.Staff => roles["Staff"],
                UserRole.Operator => roles["Receptionist"], // Operator becomes Receptionist
                UserRole.Administrator => roles["Administrator"],
                _ => roles["Staff"] // Default to Staff if unknown
            };

            user.RoleId = newRoleId;
            migratedCount++;
        }

        await context.SaveChangesAsync();

        Console.WriteLine($"Successfully migrated {migratedCount} users to new role system.");
        Console.WriteLine($"  - Staff: {usersToMigrate.Count(u => u.Role == UserRole.Staff)}");
        Console.WriteLine($"  - Receptionist (formerly Operator): {usersToMigrate.Count(u => u.Role == UserRole.Operator)}");
        Console.WriteLine($"  - Administrator: {usersToMigrate.Count(u => u.Role == UserRole.Administrator)}");
    }

    /// <summary>
    /// Validates that all users have been migrated
    /// </summary>
    public static async Task<bool> ValidateMigrationAsync(ApplicationDbContext context)
    {
        var unmigrated = await context.Users
            .Where(u => u.RoleId == null)
            .CountAsync();

        if (unmigrated > 0)
        {
            Console.WriteLine($"WARNING: {unmigrated} users still have null RoleId.");
            return false;
        }

        Console.WriteLine("All users successfully migrated to new role system.");
        return true;
    }

    /// <summary>
    /// Rollback migration by clearing RoleId
    /// </summary>
    public static async Task RollbackMigrationAsync(ApplicationDbContext context)
    {
        Console.WriteLine("Rolling back user role migration...");

        var users = await context.Users
            .Where(u => u.RoleId != null)
            .ToListAsync();

        foreach (var user in users)
        {
            user.RoleId = null;
        }

        await context.SaveChangesAsync();

        Console.WriteLine($"Successfully rolled back {users.Count} users to enum-based roles.");
    }
}
