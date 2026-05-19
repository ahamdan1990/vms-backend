using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Seeds;

/// <summary>
/// Seeder for system roles
/// </summary>
public static class RoleSeeder
{
    private static readonly List<Role> SystemRoles = new()
    {
        new Role
        {
            Name = "Staff",
            DisplayName = "Staff (Host)",
            Description = "Staff members who can create and manage their own invitations. They see only visitors they invited and mutual visitors. They receive notifications regarding their own invitations (approval, arrival, delays).",
            HierarchyLevel = 1,
            IsSystemRole = true,
            IsActive = true,
            DisplayOrder = 1,
            Color = "#3B82F6",
            Icon = "UserIcon",
            CreatedAt = DateTime.UtcNow
        },
        new Role
        {
            Name = "Receptionist",
            DisplayName = "Receptionist",
            Description = "Reception staff who handle check-in/check-out operations, create walk-ins, and manage visitor flow. Can view all approved invitations (including pending) but cannot modify them.",
            HierarchyLevel = 2,
            IsSystemRole = true,
            IsActive = true,
            DisplayOrder = 2,
            Color = "#10B981",
            Icon = "ClipboardCheckIcon",
            CreatedAt = DateTime.UtcNow
        },
        new Role
        {
            Name = "Administrator",
            DisplayName = "Administrator",
            Description = "Full system access with all permissions. Can manage users, roles, permissions, system configuration, approve invitations, and handle blacklist management.",
            HierarchyLevel = 3,
            IsSystemRole = true,
            IsActive = true,
            DisplayOrder = 3,
            Color = "#EF4444",
            Icon = "ShieldCheckIcon",
            CreatedAt = DateTime.UtcNow
        },
        new Role
        {
            Name = "CivilDefense_Receptionist",
            DisplayName = "Civil Defense Receptionist",
            Description = "Civil Defense reception staff handling specialized check-in operations for civil defense personnel and events.",
            HierarchyLevel = 4,
            IsSystemRole = true,
            IsActive = true,
            DisplayOrder = 4,
            Color = "#8B5CF6",
            Icon = "ShieldExclamationIcon",
            DashboardRoute = "/civil-defense/receptionist",
            CreatedAt = DateTime.UtcNow
        },
        new Role
        {
            Name = "CivilDefense_Manager",
            DisplayName = "Civil Defense Manager",
            Description = "Civil Defense manager with oversight, reporting capabilities, and staff activity monitoring for civil defense operations.",
            HierarchyLevel = 5,
            IsSystemRole = true,
            IsActive = true,
            DisplayOrder = 5,
            Color = "#F59E0B",
            Icon = "StarIcon",
            DashboardRoute = "/civil-defense/manager",
            CreatedAt = DateTime.UtcNow
        }
    };

    /// <summary>
    /// Seeds all system roles to the database. Adds missing roles without re-seeding existing ones.
    /// </summary>
    public static async Task SeedRolesToDbAsync(ApplicationDbContext context)
    {
        var existingNames = await context.Roles.Select(r => r.Name).ToListAsync();
        var toAdd = SystemRoles.Where(r => !existingNames.Contains(r.Name)).ToList();

        if (toAdd.Count == 0)
        {
            Console.WriteLine("All system roles already seeded. Skipping...");
            return;
        }

        Console.WriteLine($"Seeding {toAdd.Count} missing system role(s)...");
        await context.Roles.AddRangeAsync(toAdd);
        await context.SaveChangesAsync();
        Console.WriteLine($"Successfully seeded: {string.Join(", ", toAdd.Select(r => r.Name))}");
    }

    /// <summary>
    /// Gets default role by name
    /// </summary>
    public static string GetDefaultRoleName(int hierarchyLevel)
    {
        return hierarchyLevel switch
        {
            1 => "Staff",
            2 => "Receptionist",
            3 => "Administrator",
            _ => "Staff"
        };
    }

    /// <summary>
    /// Validates role hierarchy rules
    /// </summary>
    public static bool CanModifyRole(int modifierLevel, int targetLevel)
    {
        // Only administrators (level 3) can modify any role
        // Receptionists (level 2) cannot modify any roles
        // Staff (level 1) cannot modify any roles
        return modifierLevel >= 3 && modifierLevel > targetLevel;
    }
}
