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
    /// IMPROVED: Also fixes mismatched Role/RoleId values, not just NULL
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

        // Get all users with NULL RoleId OR mismatched Role string vs RoleId FK
        var allUsers = await context.Users.Include(u => u.RoleEntity).ToListAsync();

        var usersToMigrate = allUsers
            .Where(u => u.RoleId == null || u.RoleEntity == null || u.Role.ToString() != u.RoleEntity.Name)
            .ToList();

        if (!usersToMigrate.Any())
        {
            Console.WriteLine("No users to migrate. All users have correct RoleId values.");
            return;
        }

        Console.WriteLine($"Found {usersToMigrate.Count} users needing role migration:");

        var nullRoleIdUsers = usersToMigrate.Where(u => u.RoleId == null).ToList();
        var mismatchedUsers = usersToMigrate.Where(u => u.RoleId != null && u.RoleEntity != null && u.Role.ToString() != u.RoleEntity.Name).ToList();

        if (nullRoleIdUsers.Any())
        {
            Console.WriteLine($"  - {nullRoleIdUsers.Count} users with NULL RoleId");
        }

        if (mismatchedUsers.Any())
        {
            Console.WriteLine($"  - {mismatchedUsers.Count} users with mismatched Role/RoleId:");
            foreach (var user in mismatchedUsers)
            {
                Console.WriteLine($"      • {user.Email.Value}: Role enum={user.Role} but RoleId points to '{user.RoleEntity?.Name}'");
            }
        }

        var migratedCount = 0;
        var fixedNullCount = 0;
        var fixedMismatchCount = 0;

        foreach (var user in usersToMigrate)
        {
            var oldRoleId = user.RoleId;

            // Map old enum Role to new RoleId
            var roleName = user.Role.ToString();
            var newRoleId = roles.ContainsKey(roleName)
                ? roles[roleName]
                : roles["Staff"]; // Default to Staff if role name not found

            user.RoleId = newRoleId;
            migratedCount++;

            // Track what we fixed
            if (oldRoleId == null)
            {
                fixedNullCount++;
            }
            else if (oldRoleId != newRoleId)
            {
                fixedMismatchCount++;
            }
        }

        await context.SaveChangesAsync();

        Console.WriteLine($"Successfully migrated {migratedCount} users to new role system:");
        if (fixedNullCount > 0)
        {
            Console.WriteLine($"  - Fixed {fixedNullCount} users with NULL RoleId");
        }
        if (fixedMismatchCount > 0)
        {
            Console.WriteLine($"  - Fixed {fixedMismatchCount} users with mismatched RoleId");
        }

        Console.WriteLine($"Role distribution:");
        Console.WriteLine($"  - Staff: {usersToMigrate.Count(u => u.Role == UserRole.Staff)}");
        Console.WriteLine($"  - Receptionist: {usersToMigrate.Count(u => u.Role == UserRole.Receptionist)}");
        Console.WriteLine($"  - Administrator: {usersToMigrate.Count(u => u.Role == UserRole.Administrator)}");
    }

    /// <summary>
    /// Validates that all users have been migrated and have matching Role/RoleId
    /// </summary>
    public static async Task<bool> ValidateMigrationAsync(ApplicationDbContext context)
    {
        // Check for NULL RoleId
        var nullRoleIdCount = await context.Users
            .Where(u => u.RoleId == null)
            .CountAsync();

        // Check for mismatched Role/RoleId
        var allUsers = await context.Users.Include(u => u.RoleEntity).ToListAsync();
        var mismatchedCount = allUsers
            .Count(u => u.RoleId != null && u.RoleEntity != null && u.Role.ToString() != u.RoleEntity.Name);

        var totalIssues = nullRoleIdCount + mismatchedCount;

        if (totalIssues > 0)
        {
            Console.WriteLine("WARNING: User role validation failed:");
            if (nullRoleIdCount > 0)
            {
                Console.WriteLine($"  - {nullRoleIdCount} users still have NULL RoleId");
            }
            if (mismatchedCount > 0)
            {
                Console.WriteLine($"  - {mismatchedCount} users have mismatched Role/RoleId");

                // Show the mismatched users for debugging
                var mismatchedUsers = allUsers
                    .Where(u => u.RoleId != null && u.RoleEntity != null && u.Role.ToString() != u.RoleEntity.Name)
                    .Take(5); // Show first 5 to avoid flooding console

                foreach (var user in mismatchedUsers)
                {
                    Console.WriteLine($"      • {user.Email.Value}: Role={user.Role}, RoleId={user.RoleId} ({user.RoleEntity?.Name})");
                }
                if (mismatchedCount > 5)
                {
                    Console.WriteLine($"      ... and {mismatchedCount - 5} more");
                }
            }
            return false;
        }

        Console.WriteLine("✅ All users successfully migrated to new role system.");
        Console.WriteLine("   All users have matching Role enum and RoleId FK values.");
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
