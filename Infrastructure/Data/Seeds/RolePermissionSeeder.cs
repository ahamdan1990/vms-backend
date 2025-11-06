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
            Permissions.Profile.ChangePassword,
            Permissions.Profile.ManagePreferences,
            Permissions.Profile.ViewActivity,
            Permissions.Profile.UploadPhoto,

            // Own invitation management
            Permissions.Invitation.CreateOwn,
            Permissions.Invitation.ReadOwn,
            Permissions.Invitation.UpdateOwn,
            Permissions.Invitation.CancelOwn,
            Permissions.Invitation.Read,
            Permissions.Invitation.ViewHistory,

            // Visitor management (own and mutual visitors)
            Permissions.Visitor.Create,
            Permissions.Visitor.Read,
            Permissions.Visitor.ReadOwn,
            Permissions.Visitor.Update,
            Permissions.Visitor.ViewStatistics,
            Permissions.Visitor.Search,
            Permissions.Visitor.ManagePhotos,

            // Visitor documents (for invitation-related documents)
            Permissions.VisitorDocument.Create,
            Permissions.VisitorDocument.Read,
            Permissions.VisitorDocument.Update,
            Permissions.VisitorDocument.Upload,
            Permissions.VisitorDocument.Download,

            // Visitor notes (for tracking visitor information)
            Permissions.VisitorNote.Create,
            Permissions.VisitorNote.Read,
            Permissions.VisitorNote.Update,

            // Emergency contacts (for visitor safety)
            Permissions.EmergencyContact.Create,
            Permissions.EmergencyContact.Read,
            Permissions.EmergencyContact.Update,

            // Visit purposes (read-only for creating invitations)
            Permissions.VisitPurpose.Read,

            // Locations (read-only for creating invitations)
            Permissions.Location.Read,

            // Calendar viewing
            Permissions.Calendar.ViewOwn,
            Permissions.Calendar.ViewAvailability,

            // Notifications
            Permissions.Notification.ReadOwn,
            Permissions.Notification.Receive,
            Permissions.Notification.Acknowledge,

            // Dashboard
            Permissions.Dashboard.ViewBasic,

            // Reports (own data only)
            Permissions.Report.GenerateOwn,
            Permissions.Report.ViewHistory,

            // Template downloads
            Permissions.Template.DownloadSingle,

            // User activity
            Permissions.User.ViewActivity
        };

        // Receptionist Permissions - Check-in/out, walk-ins, view all invitations (including pending)
        var receptionistPermissions = new[]
        {
            // Profile management
            Permissions.Profile.ViewOwn,
            Permissions.Profile.UpdateOwn,
            Permissions.Profile.ChangePassword,

            // Check-in/Check-out operations
            Permissions.CheckIn.Process,
            Permissions.CheckIn.ProcessOut,
            Permissions.CheckIn.ViewQueue,
            Permissions.CheckIn.ViewHistory,
            Permissions.CheckIn.PrintBadge,
            Permissions.CheckIn.QRScan,
            Permissions.CheckIn.Override,
            Permissions.CheckIn.ManualVerification,
            Permissions.CheckIn.ManualEntry,
            Permissions.CheckIn.PhotoCapture,

            // Walk-in registration
            Permissions.WalkIn.Register,
            Permissions.WalkIn.QuickRegister,
            Permissions.WalkIn.CheckIn,
            Permissions.WalkIn.ViewList,
            Permissions.WalkIn.ViewHistory,
            Permissions.WalkIn.FRSync,

            // View all invitations (read-only, including PENDING)
            Permissions.Invitation.ReadAll,
            Permissions.Invitation.ViewPending,
            Permissions.Invitation.ViewHistory,
            Permissions.Invitation.ReadOwn,
            Permissions.Invitation.Read,

            // View all visitors
            Permissions.Visitor.ReadAll,
            Permissions.Visitor.ReadToday,
            Permissions.Visitor.ViewHistory,
            Permissions.Visitor.Create,
            Permissions.Visitor.Read,
            Permissions.Visitor.Search,
            Permissions.Visitor.ViewStatistics,
            Permissions.Visitor.ManagePhotos,

            // Visitor documents (for walk-in registration and document verification)
            Permissions.VisitorDocument.Create,
            Permissions.VisitorDocument.Read,
            Permissions.VisitorDocument.Update,
            Permissions.VisitorDocument.Upload,
            Permissions.VisitorDocument.Download,

            // Visitor notes
            Permissions.VisitorNote.Create,
            Permissions.VisitorNote.Read,
            Permissions.VisitorNote.Update,

            // Emergency contacts
            Permissions.EmergencyContact.Create,
            Permissions.EmergencyContact.Read,
            Permissions.EmergencyContact.Update,

            // Visit purposes (read-only for viewing invitation details)
            Permissions.VisitPurpose.Read,

            // Locations (read-only for check-in operations)
            Permissions.Location.Read,

            // QR Code operations
            Permissions.QRCode.Scan,
            Permissions.QRCode.Validate,
            Permissions.QRCode.ViewHistory,

            // Badge printing
            Permissions.Badge.Print,
            Permissions.Badge.ReprintLost,

            // Notifications & Alerts (consolidated)
            Permissions.Notification.ReadOwn,
            Permissions.Notification.ReadAll,
            Permissions.Notification.Receive,
            Permissions.Notification.Acknowledge,

            // Dashboard
            Permissions.Dashboard.ViewBasic,
            Permissions.Dashboard.ViewOperations,

            // Calendar
            Permissions.Calendar.ViewAll,
            Permissions.Calendar.ViewAvailability,

            // Emergency operations
            Permissions.Emergency.Export,
            Permissions.Emergency.GenerateRoster,
            Permissions.Emergency.ViewRoster,
            Permissions.Emergency.PrintRoster,

            // Reports
            Permissions.Report.GenerateAll,
            Permissions.Report.ViewHistory,
            Permissions.Report.Export,

            // System config read (for general system settings)
            Permissions.SystemConfig.Read,

            // User activity
            Permissions.User.ViewActivity,

            // Limited audit access
            Permissions.Audit.ReadAll
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
                    GrantedBy = null // System seeded, no specific user
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
                    GrantedBy = null // System seeded, no specific user
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
                    GrantedBy = null // System seeded, no specific user
                });
            }
        }

        await context.RolePermissions.AddRangeAsync(rolePermissions);
        await context.SaveChangesAsync();

        Console.WriteLine($"Successfully seeded {rolePermissions.Count} role-permission mappings.");
        Console.WriteLine($"  - Staff: {staffPermissions.Length} permissions");
        Console.WriteLine($"  - Receptionist: {receptionistPermissions.Length} permissions");
        Console.WriteLine($"  - Administrator: {adminPermissions.Length} permissions (ALL)");
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
            Permissions.Profile.ChangePassword,
            Permissions.CheckIn.Process,
            Permissions.CheckIn.ProcessOut,
            Permissions.CheckIn.ViewQueue,
            Permissions.CheckIn.ViewHistory,
            Permissions.CheckIn.PrintBadge,
            Permissions.CheckIn.QRScan,
            Permissions.CheckIn.Override,
            Permissions.CheckIn.ManualVerification,
            Permissions.WalkIn.Register,
            Permissions.WalkIn.QuickRegister,
            Permissions.WalkIn.CheckIn,
            Permissions.WalkIn.ViewList,
            Permissions.WalkIn.ViewHistory,
            Permissions.Invitation.ReadAll,
            Permissions.Invitation.ViewPending, // Key: Can see pending invitations
            Permissions.Invitation.ViewHistory,
            Permissions.Visitor.ReadAll,
            Permissions.Visitor.ReadToday,
            Permissions.Visitor.ViewHistory,
            Permissions.Visitor.Create, // For walk-in creation
            Permissions.QRCode.Scan,
            Permissions.QRCode.Validate,
            Permissions.QRCode.ViewHistory,
            Permissions.Badge.Print,
            Permissions.Badge.ReprintLost,
            Permissions.Notification.ReadOwn,
            Permissions.Notification.ReadAll,
            Permissions.Notification.Receive, // Alerts consolidated into Notification
            Permissions.Notification.Acknowledge,
            Permissions.Dashboard.ViewBasic,
            Permissions.Dashboard.ViewOperations,
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
