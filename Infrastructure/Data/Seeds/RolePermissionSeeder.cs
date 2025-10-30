using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Seeds;

/// <summary>
/// Seeder for role-permission mappings
/// </summary>
public static class RolePermissionSeeder
{
    /// <summary>
    /// Seeds role-permission mappings to the database
    /// </summary>
    public static async Task SeedRolePermissionsAsync(ApplicationDbContext context)
    {
        // Check if role permissions already exist
        if (await context.RolePermissions.AnyAsync())
        {
            Console.WriteLine("Role permissions already seeded. Skipping...");
            return;
        }

        Console.WriteLine("Seeding role permissions to database...");

        // Get roles from database
        var staffRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Staff");
        var receptionistRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Receptionist");
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");

        if (staffRole == null || receptionistRole == null || adminRole == null)
        {
            throw new InvalidOperationException("System roles must be seeded before role permissions.");
        }

        // Get all permissions from database
        var allPermissions = await context.Permissions.ToDictionaryAsync(p => p.Name, p => p.Id);

        var rolePermissions = new List<RolePermission>();

        // Staff (Host) Permissions - Own invitations and mutual visitors only
        var staffPermissions = new[]
        {
            // Profile management
            Permissions.Profile.ViewOwn,
            Permissions.Profile.UpdateOwn,

            // Own invitations
            Permissions.Invitation.CreateOwn,
            Permissions.Invitation.ReadOwn,
            Permissions.Invitation.UpdateOwn,
            Permissions.Invitation.CancelOwn,

            // Visitor management (own visitors)
            Permissions.Visitor.Create,
            Permissions.Visitor.Read,
            Permissions.Visitor.Update,

            // Calendar viewing
            Permissions.Calendar.ViewOwn,
            Permissions.Calendar.ViewAvailability, // See capacity/availability when creating invitations

            // Notifications
            Permissions.Notification.ReadOwn,

            // Dashboard
            Permissions.Dashboard.ViewBasic,

            // Reports (own data only)
            Permissions.Report.GenerateOwn,
            Permissions.Report.ViewHistory
        };

        // Receptionist Permissions - Check-in/out, walk-ins, view all invitations (read-only)
        var receptionistPermissions = new[]
        {
            // Profile management
            Permissions.Profile.ViewOwn,
            Permissions.Profile.UpdateOwn,

            // Check-in/Check-out operations
            Permissions.CheckIn.Process,
            Permissions.CheckIn.ProcessOut,
            Permissions.CheckIn.ViewQueue,
            Permissions.CheckIn.ViewHistory,
            Permissions.CheckIn.PrintBadge,
            Permissions.CheckIn.QRScan,

            // Walk-in registration
            Permissions.WalkIn.Register,
            Permissions.WalkIn.QuickRegister,
            Permissions.WalkIn.CheckIn,
            Permissions.WalkIn.ViewList,

            // View all invitations (read-only)
            Permissions.Invitation.ReadAll,
            Permissions.Invitation.ViewPending,
            Permissions.Invitation.ViewHistory,

            // View all visitors (read-only)
            Permissions.Visitor.ReadAll,
            Permissions.Visitor.ReadToday,
            Permissions.Visitor.ViewHistory,

            // QR Code operations
            Permissions.QRCode.Scan,
            Permissions.QRCode.Validate,
            Permissions.QRCode.ViewHistory,

            // Badge printing
            Permissions.Badge.Print,
            Permissions.Badge.ReprintLost,
            Permissions.Badge.ViewQueue,
            Permissions.Badge.ViewHistory,

            // Notifications
            Permissions.Notification.ReadOwn,
            Permissions.Notification.ReadAll,
            Permissions.Alert.Receive,
            Permissions.Alert.Acknowledge,
            Permissions.Alert.ViewHistory,

            // Dashboard
            Permissions.Dashboard.ViewOperations,
            Permissions.Dashboard.ViewRealTime,
            Permissions.Dashboard.ViewMetrics,

            // Calendar
            Permissions.Calendar.ViewAll,
            Permissions.Calendar.ViewAvailability,

            // Emergency export (for evacuation scenarios)
            Permissions.Emergency.Export,
            Permissions.Emergency.ViewRoster,
            Permissions.Emergency.PrintRoster,

            // Reports (view only)
            Permissions.Report.GenerateAll,
            Permissions.Report.ViewHistory,
            Permissions.Report.Export
        };

        // Administrator Permissions - ALL permissions
        var adminPermissions = allPermissions.Keys.ToArray();

        // Create role permission mappings for Staff
        foreach (var permissionName in staffPermissions)
        {
            if (allPermissions.TryGetValue(permissionName, out var permissionId))
            {
                rolePermissions.Add(new RolePermission
                {
                    RoleId = staffRole.Id,
                    PermissionId = permissionId,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = adminRole.Id // System seeded by admin role
                });
            }
            else
            {
                Console.WriteLine($"Warning: Permission '{permissionName}' not found for Staff role.");
            }
        }

        // Create role permission mappings for Receptionist
        foreach (var permissionName in receptionistPermissions)
        {
            if (allPermissions.TryGetValue(permissionName, out var permissionId))
            {
                rolePermissions.Add(new RolePermission
                {
                    RoleId = receptionistRole.Id,
                    PermissionId = permissionId,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = adminRole.Id // System seeded by admin role
                });
            }
            else
            {
                Console.WriteLine($"Warning: Permission '{permissionName}' not found for Receptionist role.");
            }
        }

        // Create role permission mappings for Administrator (ALL permissions)
        foreach (var permissionName in adminPermissions)
        {
            if (allPermissions.TryGetValue(permissionName, out var permissionId))
            {
                rolePermissions.Add(new RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = permissionId,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = adminRole.Id // System seeded by admin role
                });
            }
        }

        await context.RolePermissions.AddRangeAsync(rolePermissions);
        await context.SaveChangesAsync();

        Console.WriteLine($"Successfully seeded {rolePermissions.Count} role-permission mappings.");
        Console.WriteLine($"  - Staff: {staffPermissions.Length} permissions");
        Console.WriteLine($"  - Receptionist: {receptionistPermissions.Length} permissions");
        Console.WriteLine($"  - Administrator: {adminPermissions.Length} permissions");
    }

    /// <summary>
    /// Gets recommended permissions for a specific role
    /// </summary>
    public static List<string> GetRecommendedPermissionsForRole(string roleName)
    {
        return roleName switch
        {
            "Staff" => GetStaffPermissions(),
            "Receptionist" => GetReceptionistPermissions(),
            "Administrator" => Permissions.GetAllPermissions(),
            _ => new List<string>()
        };
    }

    /// <summary>
    /// Gets Staff role permissions
    /// </summary>
    private static List<string> GetStaffPermissions()
    {
        return new List<string>
        {
            Permissions.Profile.ViewOwn,
            Permissions.Profile.UpdateOwn,
            Permissions.Invitation.CreateOwn,
            Permissions.Invitation.ReadOwn,
            Permissions.Invitation.UpdateOwn,
            Permissions.Invitation.CancelOwn,
            Permissions.Visitor.Create,
            Permissions.Visitor.Read,
            Permissions.Visitor.Update,
            Permissions.Calendar.ViewOwn,
            Permissions.Calendar.ViewAvailability,
            Permissions.Notification.ReadOwn,
            Permissions.Dashboard.ViewBasic,
            Permissions.Report.GenerateOwn,
            Permissions.Report.ViewHistory
        };
    }

    /// <summary>
    /// Gets Receptionist role permissions
    /// </summary>
    private static List<string> GetReceptionistPermissions()
    {
        return new List<string>
        {
            Permissions.Profile.ViewOwn,
            Permissions.Profile.UpdateOwn,
            Permissions.CheckIn.Process,
            Permissions.CheckIn.ProcessOut,
            Permissions.CheckIn.ViewQueue,
            Permissions.CheckIn.ViewHistory,
            Permissions.CheckIn.PrintBadge,
            Permissions.CheckIn.QRScan,
            Permissions.WalkIn.Register,
            Permissions.WalkIn.QuickRegister,
            Permissions.WalkIn.CheckIn,
            Permissions.WalkIn.ViewList,
            Permissions.Invitation.ReadAll,
            Permissions.Invitation.ViewPending,
            Permissions.Invitation.ViewHistory,
            Permissions.Visitor.ReadAll,
            Permissions.Visitor.ReadToday,
            Permissions.Visitor.ViewHistory,
            Permissions.QRCode.Scan,
            Permissions.QRCode.Validate,
            Permissions.QRCode.ViewHistory,
            Permissions.Badge.Print,
            Permissions.Badge.ReprintLost,
            Permissions.Badge.ViewQueue,
            Permissions.Badge.ViewHistory,
            Permissions.Notification.ReadOwn,
            Permissions.Notification.ReadAll,
            Permissions.Alert.Receive,
            Permissions.Alert.Acknowledge,
            Permissions.Alert.ViewHistory,
            Permissions.Dashboard.ViewOperations,
            Permissions.Dashboard.ViewRealTime,
            Permissions.Dashboard.ViewMetrics,
            Permissions.Calendar.ViewAll,
            Permissions.Calendar.ViewAvailability,
            Permissions.Emergency.Export,
            Permissions.Emergency.ViewRoster,
            Permissions.Emergency.PrintRoster,
            Permissions.Report.GenerateAll,
            Permissions.Report.ViewHistory,
            Permissions.Report.Export
        };
    }
}
