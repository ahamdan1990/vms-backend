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
    /// Administrator role name
    /// </summary>
    public const string Administrator = "Administrator";

    /// <summary>
    /// Operator role name
    /// </summary>
    public const string Operator = "Operator";

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
            Administrator,
            Operator,
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
            Administrator,
            Operator
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
            Operator => 2,
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

        // Administrators can manage staff and operators
        if (managerRole == Administrator)
            return targetRole == Staff || targetRole == Operator || targetRole == Guest;

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
            Operator => GetOperatorPermissions(),
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
            // View own activity
            Permissions.User.ViewActivity,

            // Own invitation management
            Permissions.Invitation.CreateOwn,
            Permissions.Invitation.ReadOwn,
            Permissions.Invitation.UpdateOwn,
            Permissions.Invitation.CancelOwn,
            
            // Basic dashboard and profile
            Permissions.Dashboard.ViewBasic,
            Permissions.Profile.UpdateOwn,
            Permissions.Profile.ViewOwn,
            Permissions.Profile.ChangePassword,
            Permissions.Profile.ManagePreferences,
            
            // Template downloads
            Permissions.Template.DownloadSingle,
            
            // Calendar access
            Permissions.Calendar.ViewOwn,
            Permissions.Calendar.ViewAvailability,
            
            // Own notifications
            Permissions.Notification.ReadOwn,
            
            // Basic reporting
            Permissions.Report.GenerateOwn,
            Permissions.Report.ViewHistory,
            
            // View own activity
            Permissions.Profile.ViewActivity
        };
    }

    /// <summary>
    /// Gets operator permissions
    /// </summary>
    /// <returns>List of operator permissions</returns>
    private static List<string> GetOperatorPermissions()
    {
        return new List<string>
        {
            // View own activity
            Permissions.User.ViewActivity,

            // Today's visitors
            Permissions.Visitor.ReadToday,
            Permissions.Visitor.Search,
            
            // Check-in operations
            Permissions.CheckIn.Process,
            Permissions.CheckIn.ProcessOut,
            Permissions.CheckIn.Override,
            Permissions.CheckIn.ViewQueue,
            Permissions.CheckIn.ManualEntry,
            Permissions.CheckIn.PrintBadge,
            Permissions.CheckIn.QRScan,
            Permissions.CheckIn.PhotoCapture,
            Permissions.CheckIn.ManualVerification,
            
            // Walk-in management
            Permissions.WalkIn.Register,
            Permissions.WalkIn.CheckIn,
            Permissions.WalkIn.ViewList,
            Permissions.WalkIn.QuickRegister,
            Permissions.WalkIn.FRSync,
            
            // Badge operations
            Permissions.Badge.Print,
            Permissions.Badge.ViewQueue,
            Permissions.Badge.ReprintLost,
            
            // Alert handling
            Permissions.Alert.Receive,
            Permissions.Alert.Acknowledge,
            Permissions.Alert.ViewFRAlerts,
            Permissions.Alert.ViewVIPAlerts,
            Permissions.Alert.ViewBlacklistAlerts,
            
            // Manual operations
            Permissions.Manual.Override,
            Permissions.Manual.Verification,
            Permissions.Manual.Entry,
            Permissions.Manual.CreateOverrideLog,
            
            // Emergency operations
            Permissions.Emergency.Export,
            Permissions.Emergency.GenerateRoster,
            Permissions.Emergency.ViewRoster,
            Permissions.Emergency.PrintRoster,
            
            // Host notifications
            Permissions.Notification.SendHost,
            
            // Operations dashboard
            Permissions.Dashboard.ViewOperations,
            Permissions.Dashboard.ViewRealTime,
            
            // QR Code operations
            Permissions.QRCode.Scan,
            Permissions.QRCode.Validate,
            
            // Profile management
            Permissions.Profile.UpdateOwn,
            Permissions.Profile.ViewOwn,
            Permissions.Profile.ChangePassword
        };
    }

    /// <summary>
    /// Gets administrator permissions
    /// </summary>
    /// <returns>List of administrator permissions</returns>
    private static List<string> GetAdministratorPermissions()
    {
        var permissions = new List<string>();

        // Include all staff and operator permissions
        permissions.AddRange(GetStaffPermissions());
        permissions.AddRange(GetOperatorPermissions());

        // Add administrator-specific permissions
        permissions.AddRange(new List<string>
        {
            // User management
            Permissions.User.Create,
            Permissions.User.ReadAll,
            Permissions.User.UpdateAll,
            Permissions.User.DeleteAll,
            Permissions.User.Activate,
            Permissions.User.Deactivate,
            Permissions.User.ManageRoles,
            Permissions.User.ManagePermissions,
            Permissions.User.ViewActivity,
            Permissions.User.ResetPassword,
            
            // All invitation management
            Permissions.Invitation.CreateAll,
            Permissions.Invitation.ReadAll,
            Permissions.Invitation.UpdateAll,
            Permissions.Invitation.ApproveAll,
            Permissions.Invitation.DenyAll,
            Permissions.Invitation.CancelAll,
            Permissions.Invitation.BulkApprove,
            Permissions.Invitation.ViewHistory,
            Permissions.Invitation.Export,
            Permissions.Invitation.CreateOwn,
            Permissions.Invitation.Create,
            Permissions.Invitation.ReadOwn,
            Permissions.Invitation.Read,
            Permissions.Invitation.UpdateOwn,
            Permissions.Invitation.Approve,
            Permissions.Invitation.CancelOwn,
            Permissions.Invitation.ViewPending,
            Permissions.Invitation.Delete,


            // All visitor management
            Permissions.Visitor.Read,
            Permissions.Visitor.ReadToday,
            Permissions.Visitor.Search,
            Permissions.Visitor.Blacklist,
            Permissions.Visitor.RemoveBlacklist,
            Permissions.Visitor.MarkAsVip,
            Permissions.Visitor.RemoveVipStatus,
            Permissions.Visitor.Create,
            Permissions.Visitor.ViewStatistics,
            Permissions.Visitor.ReadAll,
            Permissions.Visitor.Update,
            Permissions.Visitor.Delete,
            Permissions.Visitor.Archive,
            Permissions.Visitor.ViewHistory,
            Permissions.Visitor.Export,
            Permissions.Visitor.ViewPersonalInfo,
            Permissions.Visitor.ManagePhotos,

            Permissions.VisitorDocument.Create,
            Permissions.VisitorDocument.Read,
            Permissions.VisitorDocument.Update,
            Permissions.VisitorDocument.Delete,
            Permissions.VisitorDocument.Download,
            Permissions.VisitorDocument.Upload,
            Permissions.VisitorDocument.ViewSensitive,

            Permissions.VisitorNote.Create,
            Permissions.VisitorNote.Read,
            Permissions.VisitorNote.Update,
            Permissions.VisitorNote.Delete,
            Permissions.VisitorNote.ViewConfidential,
            Permissions.VisitorNote.ViewFlagged,

            // Emergency contacts
            Permissions.EmergencyContact.Create,
            Permissions.EmergencyContact.Read,
            Permissions.EmergencyContact.Update,
            Permissions.EmergencyContact.Delete,
            Permissions.EmergencyContact.ViewPersonalInfo,

            // Bulk import
            Permissions.BulkImport.Create,
            Permissions.BulkImport.Process,
            Permissions.BulkImport.Validate,
            Permissions.BulkImport.Cancel,
            Permissions.BulkImport.ViewBatches,
            Permissions.BulkImport.ViewErrors,
            Permissions.BulkImport.CorrectErrors,
            Permissions.BulkImport.ViewHistory,
            Permissions.BulkImport.Export,
            Permissions.BulkImport.RetryFailed,
            
            // Watchlist management
            Permissions.Watchlist.Create,
            Permissions.Watchlist.ReadAll,
            Permissions.Watchlist.Update,
            Permissions.Watchlist.Delete,
            Permissions.Watchlist.Assign,
            Permissions.Watchlist.Unassign,
            Permissions.Watchlist.ViewAssignments,
            Permissions.Watchlist.SyncWithFR,
            Permissions.Watchlist.ManageVIP,
            Permissions.Watchlist.ManageBlacklist,
            Permissions.Watchlist.ViewSyncStatus,
            
            // Custom fields
            Permissions.CustomField.Create,
            Permissions.CustomField.ReadAll,
            Permissions.CustomField.Update,
            Permissions.CustomField.Delete,
            Permissions.CustomField.Reorder,
            Permissions.CustomField.Configure,
            Permissions.CustomField.ViewUsage,
            Permissions.CustomField.ManageValidation,
            Permissions.CustomField.BuildForms,
            
            // FR System
            Permissions.FRSystem.Configure,
            Permissions.FRSystem.Sync,
            Permissions.FRSystem.Monitor,
            Permissions.FRSystem.ViewHealth,
            Permissions.FRSystem.ManageProfiles,
            Permissions.FRSystem.ProcessEvents,
            Permissions.FRSystem.ViewSyncQueue,
            Permissions.FRSystem.Reconcile,
            Permissions.FRSystem.ViewLogs,
            Permissions.FRSystem.ConfigureWebhooks,
            
            // System configuration
            Permissions.SystemConfig.Read,
            Permissions.SystemConfig.Update,
            Permissions.SystemConfig.ViewAll,
            Permissions.SystemConfig.ManageIntegrations,
            Permissions.SystemConfig.ManageNotifications,
            Permissions.SystemConfig.ManageSecurity,
            Permissions.SystemConfig.ManageCapacity,
            Permissions.SystemConfig.ViewLogs,
            Permissions.SystemConfig.Backup,
            Permissions.SystemConfig.Restore,
            Permissions.SystemConfig.Create,
            
            // All reporting
            Permissions.Report.GenerateAll,
            Permissions.Report.Schedule,
            Permissions.Report.Export,
            Permissions.Report.CreateTemplates,
            Permissions.Report.ManageSubscriptions,
            Permissions.Report.ViewAnalytics,
            Permissions.Report.ExportData,
            Permissions.Report.ViewMetrics,
            
            // Audit access
            Permissions.Audit.ReadAll,
            Permissions.Audit.Export,
            Permissions.Audit.ViewUserActivity,
            Permissions.Audit.ViewSystemEvents,
            Permissions.Audit.ViewSecurityEvents,
            Permissions.Audit.Search,
            Permissions.Audit.Review,
            Permissions.Audit.Archive,
            
            // Template management
            Permissions.Template.DownloadBulk,
            Permissions.Template.Create,
            Permissions.Template.Update,
            Permissions.Template.Delete,
            Permissions.Template.ViewAll,
            Permissions.Template.Share,
            Permissions.Template.Customize,
            Permissions.Template.ManageBadge,
            
            // Admin dashboard
            Permissions.Dashboard.ViewAdmin,
            Permissions.Dashboard.ViewAnalytics,
            Permissions.Dashboard.Customize,
            Permissions.Dashboard.Export,
            Permissions.Dashboard.ViewMetrics,
            
            // All profiles
            Permissions.Profile.UpdateAll,
            Permissions.Profile.ViewAll,
            
            // System notifications
            Permissions.Notification.SendSystem,
            Permissions.Notification.Configure,
            Permissions.Notification.ViewHistory,
            Permissions.Notification.ManageTemplates,
            Permissions.Notification.SendBulk,
            Permissions.Notification.ViewQueue,
            
            // All calendar access
            Permissions.Calendar.ViewAll,
            Permissions.Calendar.Manage,
            Permissions.Calendar.Export,
            Permissions.Calendar.BookSlots,
            Permissions.Calendar.ViewConflicts,
            
            // Integration management
            Permissions.Integration.Configure,
            Permissions.Integration.Monitor,
            Permissions.Integration.ViewLogs,
            Permissions.Integration.TestConnection,
            Permissions.Integration.ManageKeys,
            Permissions.Integration.ViewHealth,
            
            // Sync operations
            Permissions.Sync.Monitor,
            Permissions.Sync.Reconcile,
            Permissions.Sync.ForceSync,
            Permissions.Sync.ViewQueue,
            Permissions.Sync.ResolvConflicts,
            Permissions.Sync.ViewHistory,
            Permissions.Sync.Configure,
            
            // Offline operations
            Permissions.Offline.Monitor,
            Permissions.Offline.Process,
            Permissions.Offline.ViewQueue,
            Permissions.Offline.RetryFailed,
            Permissions.Offline.PurgeCompleted,
            Permissions.Offline.ViewStatus,

            // Dynamic configuration management permissions
            Permissions.Configuration.Read,
            Permissions.Configuration.ReadAll,
            Permissions.Configuration.Update,
            Permissions.Configuration.UpdateAll,
            Permissions.Configuration.Create,
            Permissions.Configuration.Delete,
            Permissions.Configuration.ViewHistory,
            Permissions.Configuration.ViewAudit,
            Permissions.Configuration.ManageEncrypted,
            Permissions.Configuration.ManageSecurity,
            Permissions.Configuration.ManageJWT,
            Permissions.Configuration.ManagePassword,
            Permissions.Configuration.ManageLockout,
            Permissions.Configuration.ManageRateLimit,
            Permissions.Configuration.ManageLogging,
            Permissions.Configuration.Export,
            Permissions.Configuration.Import,
            Permissions.Configuration.InvalidateCache,
            Permissions.Configuration.ViewSensitive,
            Permissions.Configuration.ResetToDefaults,

            Permissions.Camera.Read,
            Permissions.Camera.ReadAll,
            Permissions.Camera.Update,
            Permissions.Camera.Delete,
            Permissions.Camera.TestConnection,
            Permissions.Camera.ManageStreaming,
            Permissions.Camera.StartStream,
            Permissions.Camera.StopStream,
            Permissions.Camera.ViewStream,
            Permissions.Camera.ManageFacialRecognition,
            Permissions.Camera.ViewFrames,
            Permissions.Camera.CaptureFrame,
            Permissions.Camera.Configure,
            Permissions.Camera.ManageCredentials,
            Permissions.Camera.ViewConfiguration,
            Permissions.Camera.ViewStatus,
            Permissions.Camera.HealthCheck,
            Permissions.Camera.ViewStatistics,
            Permissions.Camera.BulkOperations,
            Permissions.Camera.Export,
            Permissions.Camera.Create,
            Permissions.Camera.ViewHistory,
            Permissions.Camera.Maintenance,
            Permissions.Camera.ViewSensitiveData,
            Permissions.Camera.AdministerAll,
});

        return permissions.Distinct().ToList();
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
            UserRole.Administrator => Administrator,
            UserRole.Operator => Operator,
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
            Administrator => UserRole.Administrator,
            Operator => UserRole.Operator,
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
            Administrator => "Administrators with full system access and user management capabilities",
            Operator => "Operators who manage visitor check-ins, walk-ins, and real-time operations",
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