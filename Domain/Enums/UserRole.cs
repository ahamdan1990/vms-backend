using System.ComponentModel;

namespace VisitorManagementSystem.Api.Domain.Enums;

/// <summary>
/// Represents the different user roles in the system
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Staff member who can create and manage their own invitations
    /// </summary>
    [Description("Staff")]
    Staff = 1,

    /// <summary>
    /// Administrator with full system access
    /// </summary>
    [Description("Administrator")]
    Administrator = 2,

    /// <summary>
    /// Operator who manages check-ins and walk-ins
    /// </summary>
    [Description("Operator")]
    Operator = 3
}

/// <summary>
/// Extension methods for UserRole enum
/// </summary>
public static class UserRoleExtensions
{
    /// <summary>
    /// Gets the display name for the user role
    /// </summary>
    /// <param name="role">User role</param>
    /// <returns>Display name</returns>
    public static string GetDisplayName(this UserRole role)
    {
        var field = typeof(UserRole).GetField(role.ToString());
        var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                              .FirstOrDefault() as DescriptionAttribute;
        return attribute?.Description ?? role.ToString();
    }

    /// <summary>
    /// Gets the permission level for the user role
    /// </summary>
    /// <param name="role">User role</param>
    /// <returns>Permission level (higher number = more permissions)</returns>
    public static int GetPermissionLevel(this UserRole role)
    {
        return role switch
        {
            UserRole.Staff => 1,
            UserRole.Operator => 2,
            UserRole.Administrator => 3,
            _ => 0
        };
    }

    /// <summary>
    /// Checks if the role has administrative privileges
    /// </summary>
    /// <param name="role">User role</param>
    /// <returns>True if administrative role</returns>
    public static bool IsAdministrative(this UserRole role)
    {
        return role == UserRole.Administrator;
    }

    /// <summary>
    /// Checks if the role can manage users
    /// </summary>
    /// <param name="role">User role</param>
    /// <returns>True if can manage users</returns>
    public static bool CanManageUsers(this UserRole role)
    {
        return role == UserRole.Administrator;
    }

    /// <summary>
    /// Checks if the role can approve invitations
    /// </summary>
    /// <param name="role">User role</param>
    /// <returns>True if can approve invitations</returns>
    public static bool CanApproveInvitations(this UserRole role)
    {
        return role == UserRole.Administrator;
    }

    /// <summary>
    /// Checks if the role can perform check-ins
    /// </summary>
    /// <param name="role">User role</param>
    /// <returns>True if can perform check-ins</returns>
    public static bool CanPerformCheckIns(this UserRole role)
    {
        return role == UserRole.Operator || role == UserRole.Administrator;
    }

    /// <summary>
    /// Checks if the role can create invitations
    /// </summary>
    /// <param name="role">User role</param>
    /// <returns>True if can create invitations</returns>
    public static bool CanCreateInvitations(this UserRole role)
    {
        return role == UserRole.Staff || role == UserRole.Administrator;
    }

    /// <summary>
    /// Checks if the role can access system configuration
    /// </summary>
    /// <param name="role">User role</param>
    /// <returns>True if can access system configuration</returns>
    public static bool CanAccessSystemConfig(this UserRole role)
    {
        return role == UserRole.Administrator;
    }

    /// <summary>
    /// Checks if the role can view reports
    /// </summary>
    /// <param name="role">User role</param>
    /// <returns>True if can view reports</returns>
    public static bool CanViewReports(this UserRole role)
    {
        return role == UserRole.Administrator;
    }

    /// <summary>
    /// Checks if the role can manage watchlists
    /// </summary>
    /// <param name="role">User role</param>
    /// <returns>True if can manage watchlists</returns>
    public static bool CanManageWatchlists(this UserRole role)
    {
        return role == UserRole.Administrator;
    }

    /// <summary>
    /// Checks if the role can perform bulk imports
    /// </summary>
    /// <param name="role">User role</param>
    /// <returns>True if can perform bulk imports</returns>
    public static bool CanPerformBulkImports(this UserRole role)
    {
        return role == UserRole.Administrator;
    }

    /// <summary>
    /// Gets all available user roles
    /// </summary>
    /// <returns>List of all user roles</returns>
    public static List<UserRole> GetAllRoles()
    {
        return Enum.GetValues<UserRole>().ToList();
    }

    /// <summary>
    /// Gets user roles that can be assigned by the specified role
    /// </summary>
    /// <param name="assignerRole">Role of the user doing the assignment</param>
    /// <returns>List of roles that can be assigned</returns>
    public static List<UserRole> GetAssignableRoles(this UserRole assignerRole)
    {
        return assignerRole switch
        {
            UserRole.Administrator => GetAllRoles(),
            _ => new List<UserRole>() // Non-administrators cannot assign roles
        };
    }

    /// <summary>
    /// Checks if one role can manage another role
    /// </summary>
    /// <param name="managerRole">Role of the manager</param>
    /// <param name="targetRole">Role being managed</param>
    /// <returns>True if the manager role can manage the target role</returns>
    public static bool CanManageRole(this UserRole managerRole, UserRole targetRole)
    {
        // Only administrators can manage roles
        if (managerRole != UserRole.Administrator)
            return false;

        // Administrators can manage all roles
        return true;
    }

    /// <summary>
    /// Gets the default permissions for a user role
    /// </summary>
    /// <param name="role">User role</param>
    /// <returns>List of default permissions</returns>
    public static List<string> GetDefaultPermissions(this UserRole role)
    {
        return role switch
        {
            UserRole.Staff => new List<string>
            {
                "Invitation.Create.Own",
                "Invitation.Read.Own",
                "Invitation.Update.Own",
                "Invitation.Cancel.Own",
                "Template.Download.Single",
                "Calendar.View.Own",
                "Dashboard.View.Basic",
                "Profile.Update.Own",
                "Notification.Read.Own",
                "Report.Generate.Own"
            },
            UserRole.Operator => new List<string>
            {
                "Visitor.Read.Today",
                "Visitor.Read",
                "Visitor.ViewStatistics",
                "CheckIn.Process",
                "CheckOut.Process",
                "WalkIn.Register",
                "WalkIn.CheckIn",
                "Badge.Print",
                "Host.Notify",
                "Emergency.Export",
                "Alert.Receive",
                "Alert.Acknowledge",
                "Manual.Override",
                "Dashboard.View.Operations",
                "Dashboard.ViewMetrics",
                "QRCode.Scan",
                "Manual.Verification",
                "Emergency.Roster.Generate",
                "Notification.Send.Host",
                "Override.Log.Create",
                "WalkIn.FRSync",
                "Invitation.Read.Own",
                "Invitation.Read",
                "Invitation.ViewHistory",
                "SystemConfig.Read",
                "Dashboard.View.Basic",
                "Audit.Read.All"
            },
            UserRole.Administrator => new List<string>
            {
                // All Staff permissions
                "Invitation.Create.Own",
                "Invitation.Read.Own",
                "Invitation.Update.Own",
                "Invitation.Cancel.Own",
                "Template.Download.Single",
                "Calendar.View.Own",
                "Dashboard.View.Basic",
                "Profile.Update.Own",
                "Notification.Read.Own",
                "Report.Generate.Own",
                
                // All Operator permissions
                "Visitor.Read.Today",
                "Visitor.Read",
                "Visitor.ViewStatistics",
                "CheckIn.Process",
                "CheckOut.Process",
                "WalkIn.Register",
                "WalkIn.CheckIn",
                "Badge.Print",
                "Host.Notify",
                "Emergency.Export",
                "Alert.Receive",
                "Alert.Acknowledge",
                "Manual.Override",
                "Dashboard.View.Operations",
                "Dashboard.ViewMetrics",
                "QRCode.Scan",
                "Manual.Verification",
                "Emergency.Roster.Generate",
                "Notification.Send.Host",
                "Override.Log.Create",
                "WalkIn.FRSync",
                "Invitation.Read.Own",
                "Invitation.Read",
                "Invitation.ViewHistory",
                
                // Additional Administrator permissions
                "Invitation.Create.All",
                "Invitation.Read.All",
                "Invitation.Update.All",
                "Invitation.Approve.All",
                "Invitation.Deny.All",
                "Invitation.Cancel.All",
                "BulkImport.Create",
                "BulkImport.Process",
                "BulkImport.Validate",
                "BulkImport.Cancel",
                "User.Create",
                "User.Read.All",
                "User.Update.All",
                "User.Delete.All",
                "User.Activate",
                "User.Deactivate",
                "CustomField.Create",
                "CustomField.Read.All",
                "CustomField.Update.All",
                "CustomField.Delete.All",
                "Watchlist.Create",
                "Watchlist.Read.All",
                "Watchlist.Update.All",
                "Watchlist.Delete.All",
                "Watchlist.Assign",
                "FRSystem.Configure",
                "FRSystem.Sync",
                "FRSystem.Monitor",
                "SystemConfig.Read",
                "SystemConfig.Update",
                
                // Configuration Management permissions
                "Configuration.Read",
                "Configuration.ReadAll",
                "Configuration.Update",
                "Configuration.UpdateAll",
                "Configuration.Create",
                "Configuration.Delete",
                "Configuration.ViewHistory",
                "Configuration.ViewAudit",
                "Configuration.ManageEncrypted",
                "Configuration.ManageSecurity",
                "Configuration.ManageJWT",
                "Configuration.ManagePassword",
                "Configuration.ManageLockout",
                "Configuration.ManageRateLimit",
                "Configuration.ManageLogging",
                "Configuration.Export",
                "Configuration.Import",
                "Configuration.InvalidateCache",
                "Configuration.ViewSensitive",
                "Configuration.ResetToDefaults",
                
                "Report.Generate.All",
                "Report.Schedule",
                "Report.Export",
                "Audit.Read.All",
                "Audit.Export",
                "Template.Create",
                "Template.Update",
                "Template.Delete",
                "Integration.Configure",
                "Sync.Monitor",
                "Sync.Reconcile",
                "Offline.Monitor",
                "Offline.Process",
                "Dashboard.View.Admin"
            },
            _ => new List<string>()
        };
    }

    /// <summary>
    /// Gets the role hierarchy level
    /// </summary>
    /// <param name="role">User role</param>
    /// <returns>Hierarchy level (1 = lowest, 3 = highest)</returns>
    public static int GetHierarchyLevel(this UserRole role)
    {
        return role switch
        {
            UserRole.Staff => 1,
            UserRole.Operator => 2,
            UserRole.Administrator => 3,
            _ => 0
        };
    }

    /// <summary>
    /// Checks if one role is higher than another in the hierarchy
    /// </summary>
    /// <param name="role">First role</param>
    /// <param name="otherRole">Second role</param>
    /// <returns>True if first role is higher than second role</returns>
    public static bool IsHigherThan(this UserRole role, UserRole otherRole)
    {
        return role.GetHierarchyLevel() > otherRole.GetHierarchyLevel();
    }

    /// <summary>
    /// Checks if one role is lower than another in the hierarchy
    /// </summary>
    /// <param name="role">First role</param>
    /// <param name="otherRole">Second role</param>
    /// <returns>True if first role is lower than second role</returns>
    public static bool IsLowerThan(this UserRole role, UserRole otherRole)
    {
        return role.GetHierarchyLevel() < otherRole.GetHierarchyLevel();
    }
}