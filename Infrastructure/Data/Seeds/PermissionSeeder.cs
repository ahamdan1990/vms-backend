using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Seeds;

/// <summary>
/// Seeder for permission and role-based access control data
/// </summary>
public static class PermissionSeeder
{
    /// <summary>
    /// Seeds all permissions to the database
    /// </summary>
    public static async Task SeedPermissionsToDbAsync(ApplicationDbContext context)
    {
        Console.WriteLine("Checking for missing permissions...");

        var permissionsByCategory = GetPermissionsByCategory();
        var allPermissionsFromCode = permissionsByCategory.SelectMany(c => c.Value).ToList();

        // Get existing permissions from database
        var existingPermissions = await context.Permissions
            .Select(p => p.Name)
            .ToListAsync();

        // Find permissions that exist in code but not in database
        var missingPermissions = allPermissionsFromCode
            .Where(p => !existingPermissions.Contains(p))
            .ToList();

        if (!missingPermissions.Any())
        {
            Console.WriteLine("All permissions already seeded. No new permissions to add.");
            return;
        }

        Console.WriteLine($"Found {missingPermissions.Count} new permissions to seed...");

        var permissionsToAdd = new List<Permission>();
        var maxDisplayOrder = await context.Permissions.AnyAsync()
            ? await context.Permissions.MaxAsync(p => p.DisplayOrder)
            : 0;
        var displayOrder = maxDisplayOrder + 1;

        foreach (var category in permissionsByCategory.OrderBy(c => c.Key))
        {
            foreach (var permissionName in category.Value)
            {
                // Only add if it's a missing permission
                if (!missingPermissions.Contains(permissionName))
                    continue;

                var permission = new Permission
                {
                    Name = permissionName,
                    Category = category.Key,
                    Description = GetPermissionDescription(permissionName, category.Key),
                    DisplayName = GetPermissionDisplayName(permissionName, category.Key),
                    RiskLevel = GetPermissionRiskLevelFromString(permissionName),
                    IsActive = true,
                    IsSystemPermission = true,
                    DisplayOrder = displayOrder++,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = null // System seed
                };

                permissionsToAdd.Add(permission);
            }
        }

        await context.Permissions.AddRangeAsync(permissionsToAdd);
        await context.SaveChangesAsync();

        Console.WriteLine($"Successfully seeded {permissionsToAdd.Count} new permissions.");
    }

    /// <summary>
    /// Gets a human-readable display name for a permission
    /// </summary>
    private static string GetPermissionDisplayName(string permissionName, string category)
    {
        var parts = permissionName.Split('.');
        if (parts.Length < 2) return permissionName;

        var action = string.Join(" ", parts.Skip(1));
        return $"{action} - {category}";
    }

    /// <summary>
    /// Gets a descriptive explanation of what the permission allows
    /// </summary>
    private static string GetPermissionDescription(string permissionName, string category)
    {
        var parts = permissionName.Split('.');
        if (parts.Length < 2) return permissionName;

        var action = string.Join(" ", parts.Skip(1)).ToLower();
        return $"Allows user to {action} {category.ToLower()} in the system";
    }

    /// <summary>
    /// Determines the risk level of a permission based on its nature
    /// </summary>
    private static int GetPermissionRiskLevelFromString(string permissionName)
    {
        // Critical (5)
        if (permissionName.Contains("User.Delete") ||
            permissionName.Contains("SystemConfig.Update"))
        {
            return 5;
        }

        // Very High (4)
        if (permissionName.Contains("User.Create") ||
            permissionName.Contains("User.Update") ||
            permissionName.Contains("User.Deactivate") ||
            permissionName.Contains("User.ManagePermissions") ||
            permissionName.Contains("User.ManageRoles") ||
            permissionName.Contains("FRSystem.Configure") ||
            permissionName.Contains("Watchlist.Delete") ||
            permissionName.Contains("Integration.Configure"))
        {
            return 4;
        }

        // High (3)
        if (permissionName.Contains("Blacklist") ||
            permissionName.Contains("Override") ||
            permissionName.Contains("BulkImport.Process") ||
            permissionName.Contains("Audit.Read") ||
            permissionName.Contains("CustomField.Delete") ||
            permissionName.Contains("Manual.") ||
            permissionName.Contains("SystemConfig.Read") ||
            permissionName.Contains(".Delete.All") ||
            permissionName.Contains(".Export"))
        {
            return 3;
        }

        // Medium (2)
        if (permissionName.Contains(".Update") ||
            permissionName.Contains(".Approve") ||
            permissionName.Contains(".Reject") ||
            permissionName.Contains(".Deny") ||
            permissionName.Contains(".Cancel.All") ||
            permissionName.Contains(".Create.All") ||
            permissionName.Contains("Bulk"))
        {
            return 2;
        }

        // Low (1)
        return 1;
    }


    /// <summary>
    /// Gets all available permissions in the system
    /// </summary>
    /// <returns>List of all permissions</returns>
    public static List<string> GetAllPermissions()
    {
        return Permissions.GetAllPermissions();
    }

    /// <summary>
    /// Gets permissions grouped by category
    /// </summary>
    /// <returns>Dictionary of permissions by category</returns>
    public static Dictionary<string, List<string>> GetPermissionsByCategory()
    {
        return Permissions.GetPermissionsByCategory();
    }

    /// <summary>
    /// Gets default permissions for each role
    /// </summary>
    /// <returns>Dictionary of role to permissions mapping</returns>
    public static Dictionary<string, List<string>> GetRolePermissions()
    {
        return new Dictionary<string, List<string>>
        {
            [UserRoles.Staff] = UserRoles.GetDefaultPermissions(UserRoles.Staff),
            [UserRoles.Operator] = UserRoles.GetDefaultPermissions(UserRoles.Operator),
            [UserRoles.Administrator] = UserRoles.GetDefaultPermissions(UserRoles.Administrator)
        };
    }

    /// <summary>
    /// Validates that all role permissions are valid
    /// </summary>
    /// <returns>True if all permissions are valid</returns>
    public static bool ValidateRolePermissions()
    {
        var allPermissions = GetAllPermissions();
        var rolePermissions = GetRolePermissions();

        foreach (var role in rolePermissions)
        {
            foreach (var permission in role.Value)
            {
                if (!allPermissions.Contains(permission))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Gets permission hierarchy (which permissions depend on others)
    /// </summary>
    /// <returns>Dictionary of permission dependencies</returns>
    public static Dictionary<string, List<string>> GetPermissionDependencies()
    {
        return new Dictionary<string, List<string>>
        {
            [Permissions.User.Update] = new() { Permissions.User.Read },
            [Permissions.User.Delete] = new() { Permissions.User.Read },
            [Permissions.Invitation.UpdateAll] = new() { Permissions.Invitation.ReadAll },
            [Permissions.Invitation.UpdateOwn] = new() { Permissions.Invitation.ReadOwn },
            [Permissions.Visitor.Update] = new() { Permissions.Visitor.ReadAll },
            [Permissions.Watchlist.Update] = new() { Permissions.Watchlist.ReadAll },
            [Permissions.CustomField.Update] = new() { Permissions.CustomField.ReadAll },
            [Permissions.SystemConfig.Update] = new() { Permissions.SystemConfig.Read }
        };
    }

    /// <summary>
    /// Gets high-risk permissions that require special approval
    /// </summary>
    /// <returns>List of high-risk permissions</returns>
    public static List<string> GetHighRiskPermissions()
    {
        return new List<string>
        {
            Permissions.User.Delete,
            Permissions.User.DeleteAll,
            Permissions.SystemConfig.Update,
            Permissions.FRSystem.Configure,
            Permissions.Watchlist.Delete,
            Permissions.BulkImport.Process,
            Permissions.Audit.ReadAll,
            Permissions.Integration.Configure,
            Permissions.CustomField.Delete,
            Permissions.Manual.Override
        };
    }

    /// <summary>
    /// Gets permissions by risk level
    /// </summary>
    /// <returns>Dictionary of risk level to permissions</returns>
    public static Dictionary<int, List<string>> GetPermissionsByRiskLevel()
    {
        var allPermissions = GetAllPermissions();
        var riskLevels = new Dictionary<int, List<string>>();

        for (int level = 1; level <= 5; level++)
        {
            riskLevels[level] = new List<string>();
        }

        // Categorize permissions by risk level
        var highRiskPermissions = GetHighRiskPermissions();

        foreach (var permission in allPermissions)
        {
            if (highRiskPermissions.Contains(permission))
            {
                riskLevels[5].Add(permission); // Critical
            }
            else if (permission.Contains("Delete") || permission.Contains("Configure"))
            {
                riskLevels[4].Add(permission); // High
            }
            else if (permission.Contains("Update") || permission.Contains("Create"))
            {
                riskLevels[3].Add(permission); // Medium
            }
            else if (permission.Contains("Read") || permission.Contains("View"))
            {
                riskLevels[2].Add(permission); // Low
            }
            else
            {
                riskLevels[1].Add(permission); // Very Low
            }
        }

        return riskLevels;
    }

    /// <summary>
    /// Gets permissions that are mutually exclusive
    /// </summary>
    /// <returns>Dictionary of mutually exclusive permission groups</returns>
    public static Dictionary<string, List<string>> GetMutuallyExclusivePermissions()
    {
        return new Dictionary<string, List<string>>
        {
            ["InvitationRead"] = new()
            {
                Permissions.Invitation.ReadOwn,
                Permissions.Invitation.ReadAll
            },
            ["ReportGenerate"] = new()
            {
                Permissions.Report.GenerateOwn,
                Permissions.Report.GenerateAll
            },
            ["CalendarView"] = new()
            {
                Permissions.Calendar.ViewOwn,
                Permissions.Calendar.ViewAll
            },
            ["ProfileUpdate"] = new()
            {
                Permissions.Profile.UpdateOwn,
                Permissions.Profile.UpdateAll
            }
        };
    }

    /// <summary>
    /// Gets permission templates for common scenarios
    /// </summary>
    /// <returns>Dictionary of permission templates</returns>
    public static Dictionary<string, List<string>> GetPermissionTemplates()
    {
        return new Dictionary<string, List<string>>
        {
            ["ReadOnly"] = new()
            {
                Permissions.User.Read,
                Permissions.Invitation.ReadOwn,
                Permissions.Visitor.ReadToday,
                Permissions.Dashboard.ViewBasic,
                Permissions.Profile.ViewOwn,
                Permissions.Calendar.ViewOwn
            },
            ["BasicStaff"] = UserRoles.GetDefaultPermissions(UserRoles.Staff),
            ["PowerUser"] = new()
            {
                Permissions.Invitation.CreateAll,
                Permissions.Invitation.ReadAll,
                Permissions.Invitation.UpdateAll,
                Permissions.Visitor.ReadAll,
                Permissions.Report.GenerateAll,
                Permissions.Dashboard.ViewAdmin,
                Permissions.CustomField.ReadAll
            },
            ["SecurityOfficer"] = new()
            {
                Permissions.CheckIn.Process,
                Permissions.CheckIn.ProcessOut,
                Permissions.WalkIn.Register,
                Permissions.Alert.Receive,
                Permissions.Alert.Acknowledge,
                Permissions.Emergency.Export,
                Permissions.Manual.Override,
                Permissions.Badge.Print
            }
        };
    }

    /// <summary>
    /// Validates permission templates
    /// </summary>
    /// <returns>True if all templates are valid</returns>
    public static bool ValidatePermissionTemplates()
    {
        var allPermissions = GetAllPermissions();
        var templates = GetPermissionTemplates();

        foreach (var template in templates)
        {
            foreach (var permission in template.Value)
            {
                if (!allPermissions.Contains(permission))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Gets recommended permissions for a role upgrade
    /// </summary>
    /// <param name="currentRole">Current role</param>
    /// <param name="targetRole">Target role</param>
    /// <returns>List of additional permissions needed</returns>
    public static List<string> GetRoleUpgradePermissions(UserRole currentRole, UserRole targetRole)
    {
        var currentPermissions = UserRoles.GetDefaultPermissions(UserRoles.GetRoleName(currentRole));
        var targetPermissions = UserRoles.GetDefaultPermissions(UserRoles.GetRoleName(targetRole));

        return targetPermissions.Except(currentPermissions).ToList();
    }

    /// <summary>
    /// Gets permission audit information
    /// </summary>
    /// <returns>Permission audit data</returns>
    public static PermissionAuditInfo GetPermissionAuditInfo()
    {
        var allPermissions = GetAllPermissions();
        var rolePermissions = GetRolePermissions();
        var riskLevels = GetPermissionsByRiskLevel();

        return new PermissionAuditInfo
        {
            TotalPermissions = allPermissions.Count,
            PermissionsByCategory = GetPermissionsByCategory(),
            PermissionsByRole = rolePermissions,
            PermissionsByRiskLevel = riskLevels,
            HighRiskPermissions = GetHighRiskPermissions(),
            MutuallyExclusiveGroups = GetMutuallyExclusivePermissions(),
            PermissionDependencies = GetPermissionDependencies(),
            LastUpdated = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Permission audit information
/// </summary>
public class PermissionAuditInfo
{
    public int TotalPermissions { get; set; }
    public Dictionary<string, List<string>> PermissionsByCategory { get; set; } = new();
    public Dictionary<string, List<string>> PermissionsByRole { get; set; } = new();
    public Dictionary<int, List<string>> PermissionsByRiskLevel { get; set; } = new();
    public List<string> HighRiskPermissions { get; set; } = new();
    public Dictionary<string, List<string>> MutuallyExclusiveGroups { get; set; } = new();
    public Dictionary<string, List<string>> PermissionDependencies { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}