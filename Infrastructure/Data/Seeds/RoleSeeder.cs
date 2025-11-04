using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Seeds;

/// <summary>
/// Seeder for system roles
/// </summary>
public static class RoleSeeder
{
    /// <summary>
    /// Seeds all system roles to the database
    /// </summary>
    public static async Task SeedRolesToDbAsync(ApplicationDbContext context)
    {
        // Check if roles already exist
        if (await context.Roles.AnyAsync())
        {
            Console.WriteLine("Roles already seeded. Skipping...");
            return;
        }

        Console.WriteLine("Seeding roles to database...");

        var roles = new List<Role>
        {
            new Role
            {
                Name = "Staff",
                DisplayName = "Staff (Host)",
                Description = "Staff members who can create and manage their own invitations. They see only visitors they invited and mutual visitors.",
                HierarchyLevel = 1,
                IsSystemRole = true,
                IsActive = true,
                DisplayOrder = 1,
                Color = "#3B82F6", // Blue
                Icon = "UserIcon",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = null // System seed
            },
            new Role
            {
                Name = "Receptionist",
                DisplayName = "Receptionist",
                Description = "Reception staff who handle check-in/check-out operations, create walk-ins, and manage visitor flow. Can view all invitations but cannot modify them.",
                HierarchyLevel = 2,
                IsSystemRole = true,
                IsActive = true,
                DisplayOrder = 2,
                Color = "#10B981", // Green
                Icon = "ClipboardCheckIcon",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = null // System seed
            },
            new Role
            {
                Name = "Operator",
                DisplayName = "Operator",
                Description = "Operations staff who can view approved invitations, handle check-in/check-out operations, and manage visitor flow. Can only see approved invitations.",
                HierarchyLevel = 3,
                IsSystemRole = true,
                IsActive = true,
                DisplayOrder = 3,
                Color = "#F59E0B", // Amber
                Icon = "ClipboardDocumentCheckIcon",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = null // System seed
            },
            new Role
            {
                Name = "Administrator",
                DisplayName = "Administrator",
                Description = "Full system access with all permissions. Can manage users, roles, permissions, system configuration, and handle blacklist requests.",
                HierarchyLevel = 4,
                IsSystemRole = true,
                IsActive = true,
                DisplayOrder = 4,
                Color = "#EF4444", // Red
                Icon = "ShieldCheckIcon",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = null // System seed
            }
        };

        await context.Roles.AddRangeAsync(roles);
        await context.SaveChangesAsync();

        Console.WriteLine($"Successfully seeded {roles.Count} system roles.");
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
            3 => "Operator",
            4 => "Administrator",
            _ => "Staff"
        };
    }

    /// <summary>
    /// Validates role hierarchy rules
    /// </summary>
    public static bool CanModifyRole(int modifierLevel, int targetLevel)
    {
        // Only administrators (level 4) can modify any role
        // Receptionists (level 2) cannot modify any roles
        // Staff (level 1) cannot modify any roles
        return modifierLevel >= 4 && modifierLevel > targetLevel;
    }
}
