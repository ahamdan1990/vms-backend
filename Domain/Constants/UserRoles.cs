using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Constants;

/// <summary>
/// Contains user role constants and related functionality
/// </summary>
public static class UserRoles
{
    /// <summary>
    /// Staff role name
    /// </summary>
    public const string Staff = "Staff";

    /// <summary>
    /// Receptionist role name
    /// </summary>
    public const string Receptionist = "Receptionist";

    /// <summary>
    /// Administrator role name
    /// </summary>
    public const string Administrator = "Administrator";

    /// <summary>
    /// Super administrator role (system level)
    /// </summary>
    public const string SuperAdministrator = "SuperAdministrator";

    /// <summary>
    /// System role for internal operations
    /// </summary>
    public const string System = "System";

    /// <summary>
    /// Guest role for limited access
    /// </summary>
    public const string Guest = "Guest";

    /// <summary>
    /// Gets all available role names
    /// </summary>
    /// <returns>List of all role names</returns>
    public static List<string> GetAllRoles()
    {
        return new List<string>
        {
            Staff,
            Receptionist,
            Administrator,
            SuperAdministrator,
            System,
            Guest
        };
    }

    /// <summary>
    /// Gets assignable roles (excludes system roles)
    /// </summary>
    /// <returns>List of assignable role names</returns>
    public static List<string> GetAssignableRoles()
    {
        return new List<string>
        {
            Staff,
            Receptionist,
            Administrator
        };
    }

    /// <summary>
    /// Gets the role hierarchy level
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <returns>Hierarchy level (higher number = more privileges)</returns>
    public static int GetRoleHierarchy(string roleName)
    {
        return roleName switch
        {
            Guest => 0,
            Staff => 1,
            Receptionist => 2,
            Administrator => 3,
            SuperAdministrator => 4,
            System => 5,
            _ => 0
        };
    }

    /// <summary>
    /// Checks if one role is higher than another in hierarchy
    /// </summary>
    /// <param name="role1">First role</param>
    /// <param name="role2">Second role</param>
    /// <returns>True if role1 is higher than role2</returns>
    public static bool IsRoleHigher(string role1, string role2)
    {
        return GetRoleHierarchy(role1) > GetRoleHierarchy(role2);
    }

    /// <summary>
    /// Checks if a role can manage another role
    /// </summary>
    /// <param name="managerRole">Manager's role</param>
    /// <param name="targetRole">Target role to be managed</param>
    /// <returns>True if manager can manage target role</returns>
    public static bool CanManageRole(string managerRole, string targetRole)
    {
        // System role can manage all
        if (managerRole == System)
            return true;

        // Super administrators can manage all except system
        if (managerRole == SuperAdministrator)
            return targetRole != System;

        // Administrators can manage staff and receptionists
        if (managerRole == Administrator)
            return targetRole == Staff || targetRole == Receptionist || targetRole == Guest;

        // Other roles cannot manage roles
        return false;
    }

    /// <summary>
    /// Gets the default permissions for a role
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <returns>List of default permissions</returns>
    public static List<string> GetDefaultPermissions(string roleName)
    {
        return roleName switch
        {
            Staff => GetStaffPermissions(),
            Receptionist => GetReceptionistPermissions(),
            Administrator => GetAdministratorPermissions(),
            SuperAdministrator => GetSuperAdministratorPermissions(),
            System => GetSystemPermissions(),
            Guest => GetGuestPermissions(),
            _ => new List<string>()
        };
    }

    /// <summary>
    /// Gets staff permissions
    /// </summary>
    /// <returns>List of staff permissions</returns>
    private static List<string> GetStaffPermissions()
    {
        return new List<string>
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

            // Calendar access
            Permissions.Calendar.ViewOwn,
            Permissions.Calendar.ViewAvailability,

            // Own notifications
            Permissions.Notification.ReadOwn,
            Permissions.Notification.Receive,
            Permissions.Notification.Acknowledge,

            // Basic dashboard
            Permissions.Dashboard.ViewBasic,

            // Template downloads
            Permissions.Template.DownloadSingle,

            // User activity
            Permissions.User.ViewActivity
        };
    }

    /// <summary>
    /// Gets receptionist permissions
    /// </summary>
    /// <returns>List of receptionist permissions</returns>
    private static List<string> GetReceptionistPermissions()
    {
        return new List<string>
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

            // System config read (for general system settings, NOT replaced by granular permissions)
            Permissions.SystemConfig.Read,

            // User activity
            Permissions.User.ViewActivity,

            // Limited audit access
            Permissions.Audit.ReadAll
        };
    }

    /// <summary>
    /// Gets administrator permissions (ALL permissions)
    /// </summary>
    /// <returns>List of administrator permissions</returns>
    private static List<string> GetAdministratorPermissions()
    {
        // Administrator gets ALL permissions dynamically
        return Permissions.GetAllPermissions();
    }

    /// <summary>
    /// Gets super administrator permissions
    /// </summary>
    /// <returns>List of super administrator permissions</returns>
    private static List<string> GetSuperAdministratorPermissions()
    {
        // Super admin gets all permissions
        return Permissions.GetAllPermissions();
    }

    /// <summary>
    /// Gets system permissions (for internal operations)
    /// </summary>
    /// <returns>List of system permissions</returns>
    private static List<string> GetSystemPermissions()
    {
        // System gets all permissions
        return Permissions.GetAllPermissions();
    }

    /// <summary>
    /// Gets guest permissions (very limited)
    /// </summary>
    /// <returns>List of guest permissions</returns>
    private static List<string> GetGuestPermissions()
    {
        return new List<string>
        {
            Permissions.Profile.ViewOwn,
            Permissions.Profile.UpdateOwn
        };
    }

    /// <summary>
    /// Converts UserRole enum to string
    /// </summary>
    /// <param name="userRole">UserRole enum value</param>
    /// <returns>Role name string</returns>
    public static string GetRoleName(UserRole userRole)
    {
        return userRole switch
        {
            UserRole.Staff => Staff,
            UserRole.Receptionist => Receptionist,
            UserRole.Administrator => Administrator,
            _ => Guest
        };
    }

    /// <summary>
    /// Converts role string to UserRole enum
    /// </summary>
    /// <param name="roleName">Role name string</param>
    /// <returns>UserRole enum value</returns>
    public static UserRole GetUserRole(string roleName)
    {
        return roleName switch
        {
            Staff => UserRole.Staff,
            Receptionist => UserRole.Receptionist,
            Administrator => UserRole.Administrator,
            _ => throw new ArgumentException($"Invalid role name: {roleName}")
        };
    }

    /// <summary>
    /// Gets role description
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <returns>Role description</returns>
    public static string GetRoleDescription(string roleName)
    {
        return roleName switch
        {
            Staff => "Staff members who can create and manage their own invitations",
            Receptionist => "Receptionists who handle check-in/check-out operations, walk-ins, and visitor flow management",
            Administrator => "Administrators with full system access and user management capabilities",
            SuperAdministrator => "Super administrators with complete system control",
            System => "System role for internal operations and automated processes",
            Guest => "Guest users with very limited access",
            _ => "Unknown role"
        };
    }

    /// <summary>
    /// Checks if a role name is valid
    /// </summary>
    /// <param name="roleName">Role name to validate</param>
    /// <returns>True if valid role name</returns>
    public static bool IsValidRole(string roleName)
    {
        return GetAllRoles().Contains(roleName);
    }

    /// <summary>
    /// Checks if a role is assignable to users
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <returns>True if role is assignable</returns>
    public static bool IsAssignableRole(string roleName)
    {
        return GetAssignableRoles().Contains(roleName);
    }

    /// <summary>
    /// Gets roles that have a specific permission
    /// </summary>
    /// <param name="permission">Permission to check</param>
    /// <returns>List of roles that have the permission</returns>
    public static List<string> GetRolesWithPermission(string permission)
    {
        var rolesWithPermission = new List<string>();

        foreach (var role in GetAllRoles())
        {
            var rolePermissions = GetDefaultPermissions(role);
            if (rolePermissions.Contains(permission))
            {
                rolesWithPermission.Add(role);
            }
        }

        return rolesWithPermission;
    }

    /// <summary>
    /// Gets the minimum role required for a permission
    /// </summary>
    /// <param name="permission">Permission to check</param>
    /// <returns>Minimum role name or null if permission not found</returns>
    public static string? GetMinimumRoleForPermission(string permission)
    {
        var rolesWithPermission = GetRolesWithPermission(permission);

        if (!rolesWithPermission.Any())
            return null;

        // Return the role with the lowest hierarchy level
        return rolesWithPermission
            .OrderBy(GetRoleHierarchy)
            .FirstOrDefault();
    }
}
