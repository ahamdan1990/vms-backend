using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Seeds;

/// <summary>
/// Seeds Civil Defense roles, permissions, and default configuration keys.
/// Idempotent — safe to run multiple times; only adds what is missing.
/// </summary>
public static class CivilDefenseSeeder
{
    private const string ReceptionistRoleName = "civil_defense_receptionist";
    private const string ManagerRoleName      = "civil_defense_manager";

    private static readonly string[] ReceptionistPermissions =
    [
        Permissions.CivilDefense.ViewLiveBuilding,
        Permissions.CivilDefense.CheckIn,
        Permissions.CivilDefense.CheckOut,
        Permissions.CivilDefense.RegisterVisitor,
        Permissions.CivilDefense.StaffCheckIn,
        Permissions.CivilDefense.ScanDocument,
        Permissions.CivilDefense.ProcessFaceFrame,
        // Reuse existing visitor/invitation permissions
        Permissions.Visitor.Create,
        Permissions.Visitor.ReadAll,
        Permissions.CheckIn.Process,
        Permissions.CheckIn.ProcessOut,
        Permissions.WalkIn.Register,
    ];

    private static readonly string[] ManagerPermissions =
    [
        Permissions.CivilDefense.ViewLiveBuilding,
        Permissions.CivilDefense.ViewReports,
        Permissions.CivilDefense.ExportData,
        Permissions.CivilDefense.ManageCameras,
        // Read-only visitor access
        Permissions.Visitor.ReadAll,
        Permissions.Visitor.ViewHistory,
        Permissions.Report.GenerateAll,
        Permissions.Report.Export,
    ];

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedRolesAsync(context);
        await SeedDefaultConfigAsync(context);
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        var existingRoleNames = await context.Roles.Select(r => r.Name).ToListAsync();

        var rolesToAdd = new List<(string Name, string Display, string Description, int Order, string Color)>
        {
            (ReceptionistRoleName, "Civil Defense Receptionist",
             "Receptionist for Civil Defense facility. Handles visitor check-in/out via camera recognition and manual entry. Arabic RTL interface.",
             10, "#F59E0B"),

            (ManagerRoleName, "Civil Defense Manager",
             "Manager for Civil Defense facility. Read-only live building view plus full reporting and analytics.",
             11, "#6366F1"),
        };

        var newRoles = new List<Role>();
        foreach (var (name, display, desc, order, color) in rolesToAdd)
        {
            if (existingRoleNames.Contains(name)) continue;

            newRoles.Add(new Role
            {
                Name        = name,
                DisplayName = display,
                Description = desc,
                HierarchyLevel = 1,
                IsSystemRole   = false,
                IsActive       = true,
                DisplayOrder   = order,
                Color          = color,
                Icon           = "ShieldIcon",
                CreatedAt      = DateTime.UtcNow,
                CreatedBy      = null
            });
        }

        if (newRoles.Any())
        {
            await context.Roles.AddRangeAsync(newRoles);
            await context.SaveChangesAsync();
            Console.WriteLine($"Civil Defense seeder: added {newRoles.Count} role(s).");
        }

        // Assign permissions to each role
        await AssignPermissionsAsync(context, ReceptionistRoleName, ReceptionistPermissions);
        await AssignPermissionsAsync(context, ManagerRoleName,      ManagerPermissions);
    }

    private static async Task AssignPermissionsAsync(
        ApplicationDbContext context, string roleName, string[] permissionNames)
    {
        var role = await context.Roles
            .FirstOrDefaultAsync(r => r.Name == roleName);

        if (role == null) return;

        var existingPermissionIds = await context.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        var permissionIds = await context.Permissions
            .Where(p => permissionNames.Contains(p.Name) && p.IsActive)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync();

        var newAssignments = permissionIds
            .Where(p => !existingPermissionIds.Contains(p.Id))
            .Select(p => new RolePermission
            {
                RoleId       = role.Id,
                PermissionId = p.Id,
                GrantedAt    = DateTime.UtcNow,
                GrantedBy    = null
            })
            .ToList();

        if (newAssignments.Any())
        {
            await context.RolePermissions.AddRangeAsync(newAssignments);
            await context.SaveChangesAsync();
            Console.WriteLine($"Civil Defense seeder: assigned {newAssignments.Count} permission(s) to '{roleName}'.");
        }
    }

    private static async Task SeedDefaultConfigAsync(ApplicationDbContext context)
    {
        var defaults = new Dictionary<string, string>
        {
            ["CivilDefense.AutoCheckInEnabled"]           = "false",
            ["CivilDefense.AutoCheckOutEnabled"]          = "false",
            ["CivilDefense.RecognitionConfidenceThreshold"] = "0.80",
            ["CivilDefense.OcrLanguage"]                  = "ara+eng",
            ["CivilDefense.EntryCameraId"]                = "",
            ["CivilDefense.ExitCameraId"]                 = "",
        };

        foreach (var (key, value) in defaults)
        {
            var parts    = key.Split('.', 2);
            var category = parts[0];
            var keyPart  = parts.Length > 1 ? parts[1] : key;

            var exists = await context.SystemConfigurations
                .AnyAsync(c => c.Category == category && c.Key == keyPart);

            if (!exists)
            {
                context.SystemConfigurations.Add(new SystemConfiguration
                {
                    Category    = category,
                    Key         = keyPart,
                    Value       = value,
                    Description = $"Civil Defense module: {keyPart}",
                    IsActive    = true,
                    CreatedOn   = DateTime.UtcNow,
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
