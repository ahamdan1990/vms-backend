using System.ComponentModel;

namespace VisitorManagementSystem.Api.Domain.Enums;

/// <summary>
/// Represents the different types of permissions that can be granted to users
/// </summary>
public enum PermissionType
{
    // User Management Permissions
    [Description("Create Users")]
    UserCreate = 1,

    [Description("Read Users")]
    UserRead = 2,

    [Description("Update Users")]
    UserUpdate = 3,

    [Description("Delete Users")]
    UserDelete = 4,

    [Description("Activate Users")]
    UserActivate = 5,

    [Description("Deactivate Users")]
    UserDeactivate = 6,

    // Invitation Management Permissions
    [Description("Create Own Invitations")]
    InvitationCreateOwn = 10,

    [Description("Create All Invitations")]
    InvitationCreateAll = 11,

    [Description("Read Own Invitations")]
    InvitationReadOwn = 12,

    [Description("Read All Invitations")]
    InvitationReadAll = 13,

    [Description("Update Own Invitations")]
    InvitationUpdateOwn = 14,

    [Description("Update All Invitations")]
    InvitationUpdateAll = 15,

    [Description("Approve Invitations")]
    InvitationApprove = 16,

    [Description("Deny Invitations")]
    InvitationDeny = 17,

    [Description("Cancel Own Invitations")]
    InvitationCancelOwn = 18,

    [Description("Cancel All Invitations")]
    InvitationCancelAll = 19,

    // Visitor Management Permissions
    [Description("Read Today's Visitors")]
    VisitorReadToday = 20,

    [Description("Read All Visitors")]
    VisitorReadAll = 21,

    [Description("Create Visitors")]
    VisitorCreate = 22,

    [Description("Update Visitors")]
    VisitorUpdate = 23,

    [Description("Delete Visitors")]
    VisitorDelete = 24,

    // Check-In/Out Permissions
    [Description("Process Check-In")]
    CheckInProcess = 30,

    [Description("Process Check-Out")]
    CheckOutProcess = 31,

    [Description("Manual Override")]
    CheckInOverride = 32,

    [Description("Print Badges")]
    BadgePrint = 33,

    // Walk-In Permissions
    [Description("Register Walk-Ins")]
    WalkInRegister = 40,

    [Description("Process Walk-In Check-In")]
    WalkInCheckIn = 41,

    [Description("Convert Walk-In to Visitor")]
    WalkInConvert = 42,

    [Description("Sync Walk-In with FR System")]
    WalkInFRSync = 43,

    // Bulk Import Permissions
    [Description("Create Bulk Import")]
    BulkImportCreate = 50,

    [Description("Process Bulk Import")]
    BulkImportProcess = 51,

    [Description("Validate Bulk Import")]
    BulkImportValidate = 52,

    [Description("Cancel Bulk Import")]
    BulkImportCancel = 53,

    // Watchlist Permissions
    [Description("Create Watchlists")]
    WatchlistCreate = 60,

    [Description("Read All Watchlists")]
    WatchlistReadAll = 61,

    [Description("Update Watchlists")]
    WatchlistUpdate = 62,

    [Description("Delete Watchlists")]
    WatchlistDelete = 63,

    [Description("Assign to Watchlists")]
    WatchlistAssign = 64,

    // Custom Field Permissions
    [Description("Create Custom Fields")]
    CustomFieldCreate = 70,

    [Description("Read All Custom Fields")]
    CustomFieldReadAll = 71,

    [Description("Update Custom Fields")]
    CustomFieldUpdate = 72,

    [Description("Delete Custom Fields")]
    CustomFieldDelete = 73,

    // Facial Recognition Permissions
    [Description("Configure FR System")]
    FRSystemConfigure = 80,

    [Description("Sync with FR System")]
    FRSystemSync = 81,

    [Description("Monitor FR System")]
    FRSystemMonitor = 82,

    [Description("Receive FR Alerts")]
    FRAlertReceive = 83,

    [Description("Acknowledge FR Alerts")]
    FRAlertAcknowledge = 84,

    // System Configuration Permissions
    [Description("Read System Configuration")]
    SystemConfigRead = 90,

    [Description("Update System Configuration")]
    SystemConfigUpdate = 91,

    // Report Permissions
    [Description("Generate Own Reports")]
    ReportGenerateOwn = 100,

    [Description("Generate All Reports")]
    ReportGenerateAll = 101,

    [Description("Schedule Reports")]
    ReportSchedule = 102,

    [Description("Export Reports")]
    ReportExport = 103,

    // Audit Permissions
    [Description("Read All Audit Logs")]
    AuditReadAll = 110,

    [Description("Export Audit Logs")]
    AuditExport = 111,

    // Template Permissions
    [Description("Download Single Template")]
    TemplateDownloadSingle = 120,

    [Description("Download Bulk Template")]
    TemplateDownloadBulk = 121,

    [Description("Create Templates")]
    TemplateCreate = 122,

    [Description("Update Templates")]
    TemplateUpdate = 123,

    [Description("Delete Templates")]
    TemplateDelete = 124,

    // Dashboard Permissions
    [Description("View Basic Dashboard")]
    DashboardViewBasic = 130,

    [Description("View Operations Dashboard")]
    DashboardViewOperations = 131,

    [Description("View Admin Dashboard")]
    DashboardViewAdmin = 132,

    // Profile Permissions
    [Description("Update Own Profile")]
    ProfileUpdateOwn = 140,

    [Description("Update All Profiles")]
    ProfileUpdateAll = 141,

    // Notification Permissions
    [Description("Read Own Notifications")]
    NotificationReadOwn = 150,

    [Description("Send Host Notifications")]
    NotificationSendHost = 151,

    [Description("Send System Notifications")]
    NotificationSendSystem = 152,

    // Calendar Permissions
    [Description("View Own Calendar")]
    CalendarViewOwn = 160,

    [Description("View All Calendars")]
    CalendarViewAll = 161,

    // Emergency Permissions
    [Description("Export Emergency Roster")]
    EmergencyExport = 170,

    [Description("Generate Emergency Roster")]
    EmergencyRosterGenerate = 171,

    // QR Code Permissions
    [Description("Scan QR Codes")]
    QRCodeScan = 180,

    [Description("Generate QR Codes")]
    QRCodeGenerate = 181,

    // Manual Verification Permissions
    [Description("Manual Verification")]
    ManualVerification = 190,

    [Description("Create Override Logs")]
    OverrideLogCreate = 191,

    // Integration Permissions
    [Description("Configure Integrations")]
    IntegrationConfigure = 200,

    [Description("Monitor Sync Operations")]
    SyncMonitor = 201,

    [Description("Reconcile Data")]
    SyncReconcile = 202,

    // Offline Operations Permissions
    [Description("Monitor Offline Operations")]
    OfflineMonitor = 210,

    [Description("Process Offline Queue")]
    OfflineProcess = 211
}

/// <summary>
/// Extension methods for PermissionType enum
/// </summary>
public static class PermissionTypeExtensions
{
    /// <summary>
    /// Gets the display name for the permission type
    /// </summary>
    /// <param name="permissionType">Permission type</param>
    /// <returns>Display name</returns>
    public static string GetDisplayName(this PermissionType permissionType)
    {
        var field = typeof(PermissionType).GetField(permissionType.ToString());
        var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                              .FirstOrDefault() as DescriptionAttribute;
        return attribute?.Description ?? permissionType.ToString();
    }

    /// <summary>
    /// Gets the permission string used in authorization policies
    /// </summary>
    /// <param name="permissionType">Permission type</param>
    /// <returns>Permission string</returns>
    public static string GetPermissionString(this PermissionType permissionType)
    {
        return permissionType.ToString();
    }

    /// <summary>
    /// Gets the category for grouping permissions
    /// </summary>
    /// <param name="permissionType">Permission type</param>
    /// <returns>Category name</returns>
    public static string GetCategory(this PermissionType permissionType)
    {
        return permissionType switch
        {
            >= PermissionType.UserCreate and <= PermissionType.UserDeactivate => "User Management",
            >= PermissionType.InvitationCreateOwn and <= PermissionType.InvitationCancelAll => "Invitation Management",
            >= PermissionType.VisitorReadToday and <= PermissionType.VisitorDelete => "Visitor Management",
            >= PermissionType.CheckInProcess and <= PermissionType.BadgePrint => "Check-In/Out",
            >= PermissionType.WalkInRegister and <= PermissionType.WalkInFRSync => "Walk-In Management",
            >= PermissionType.BulkImportCreate and <= PermissionType.BulkImportCancel => "Bulk Import",
            >= PermissionType.WatchlistCreate and <= PermissionType.WatchlistAssign => "Watchlist Management",
            >= PermissionType.CustomFieldCreate and <= PermissionType.CustomFieldDelete => "Custom Fields",
            >= PermissionType.FRSystemConfigure and <= PermissionType.FRAlertAcknowledge => "Facial Recognition",
            >= PermissionType.SystemConfigRead and <= PermissionType.SystemConfigUpdate => "System Configuration",
            >= PermissionType.ReportGenerateOwn and <= PermissionType.ReportExport => "Reports",
            >= PermissionType.AuditReadAll and <= PermissionType.AuditExport => "Audit",
            >= PermissionType.TemplateDownloadSingle and <= PermissionType.TemplateDelete => "Templates",
            >= PermissionType.DashboardViewBasic and <= PermissionType.DashboardViewAdmin => "Dashboard",
            >= PermissionType.ProfileUpdateOwn and <= PermissionType.ProfileUpdateAll => "Profile",
            >= PermissionType.NotificationReadOwn and <= PermissionType.NotificationSendSystem => "Notifications",
            >= PermissionType.CalendarViewOwn and <= PermissionType.CalendarViewAll => "Calendar",
            >= PermissionType.EmergencyExport and <= PermissionType.EmergencyRosterGenerate => "Emergency",
            >= PermissionType.QRCodeScan and <= PermissionType.QRCodeGenerate => "QR Code",
            >= PermissionType.ManualVerification and <= PermissionType.OverrideLogCreate => "Manual Operations",
            >= PermissionType.IntegrationConfigure and <= PermissionType.SyncReconcile => "Integration",
            >= PermissionType.OfflineMonitor and <= PermissionType.OfflineProcess => "Offline Operations",
            _ => "General"
        };
    }

    /// <summary>
    /// Gets the risk level associated with the permission
    /// </summary>
    /// <param name="permissionType">Permission type</param>
    /// <returns>Risk level (1 = Low, 5 = Critical)</returns>
    public static int GetRiskLevel(this PermissionType permissionType)
    {
        return permissionType switch
        {
            PermissionType.UserDelete => 5,
            PermissionType.SystemConfigUpdate => 5,
            PermissionType.FRSystemConfigure => 4,
            PermissionType.WatchlistDelete => 4,
            PermissionType.UserCreate => 4,
            PermissionType.UserUpdate => 4,
            PermissionType.UserDeactivate => 4,
            PermissionType.BulkImportProcess => 3,
            PermissionType.AuditReadAll => 3,
            PermissionType.IntegrationConfigure => 3,
            PermissionType.CustomFieldDelete => 3,
            PermissionType.CheckInOverride => 3,
            PermissionType.ManualVerification => 3,
            _ => 1
        };
    }

    /// <summary>
    /// Checks if the permission is considered administrative
    /// </summary>
    /// <param name="permissionType">Permission type</param>
    /// <returns>True if administrative permission</returns>
    public static bool IsAdministrative(this PermissionType permissionType)
    {
        return permissionType.GetRiskLevel() >= 3;
    }

    /// <summary>
    /// Gets permissions by category
    /// </summary>
    /// <param name="category">Category name</param>
    /// <returns>List of permissions in the category</returns>
    public static List<PermissionType> GetPermissionsByCategory(string category)
    {
        return Enum.GetValues<PermissionType>()
                   .Where(p => p.GetCategory().Equals(category, StringComparison.OrdinalIgnoreCase))
                   .ToList();
    }

    /// <summary>
    /// Gets all permission categories
    /// </summary>
    /// <returns>List of all categories</returns>
    public static List<string> GetAllCategories()
    {
        return Enum.GetValues<PermissionType>()
                   .Select(p => p.GetCategory())
                   .Distinct()
                   .OrderBy(c => c)
                   .ToList();
    }

    /// <summary>
    /// Gets all permissions for a specific user role
    /// </summary>
    /// <param name="role">User role</param>
    /// <returns>List of permissions for the role</returns>
    public static List<PermissionType> GetPermissionsForRole(UserRole role)
    {
        var permissionStrings = role.GetDefaultPermissions();
        var permissions = new List<PermissionType>();

        foreach (var permissionString in permissionStrings)
        {
            // Convert permission string to enum
            var parts = permissionString.Split('.');
            var enumName = string.Join("", parts.Select(p => char.ToUpper(p[0]) + p.Substring(1).ToLower()));

            if (Enum.TryParse<PermissionType>(enumName, out var permission))
            {
                permissions.Add(permission);
            }
        }

        return permissions;
    }

    /// <summary>
    /// Checks if a permission requires another permission as a prerequisite
    /// </summary>
    /// <param name="permissionType">Permission to check</param>
    /// <param name="prerequisite">Potential prerequisite permission</param>
    /// <returns>True if prerequisite is required</returns>
    public static bool RequiresPermission(this PermissionType permissionType, PermissionType prerequisite)
    {
        // Define permission dependencies
        var dependencies = new Dictionary<PermissionType, List<PermissionType>>
        {
            { PermissionType.UserUpdate, new() { PermissionType.UserRead } },
            { PermissionType.UserDelete, new() { PermissionType.UserRead } },
            { PermissionType.InvitationUpdateAll, new() { PermissionType.InvitationReadAll } },
            { PermissionType.InvitationUpdateOwn, new() { PermissionType.InvitationReadOwn } },
            { PermissionType.VisitorUpdate, new() { PermissionType.VisitorReadAll } },
            { PermissionType.WatchlistUpdate, new() { PermissionType.WatchlistReadAll } },
            { PermissionType.CustomFieldUpdate, new() { PermissionType.CustomFieldReadAll } },
            { PermissionType.SystemConfigUpdate, new() { PermissionType.SystemConfigRead } }
        };

        return dependencies.ContainsKey(permissionType) &&
               dependencies[permissionType].Contains(prerequisite);
    }

    /// <summary>
    /// Gets all permissions that are mutually exclusive with the given permission
    /// </summary>
    /// <param name="permissionType">Permission type</param>
    /// <returns>List of mutually exclusive permissions</returns>
    public static List<PermissionType> GetMutuallyExclusivePermissions(this PermissionType permissionType)
    {
        // Define mutually exclusive permissions
        return permissionType switch
        {
            PermissionType.InvitationReadOwn => new() { PermissionType.InvitationReadAll },
            PermissionType.InvitationReadAll => new() { PermissionType.InvitationReadOwn },
            PermissionType.ReportGenerateOwn => new() { PermissionType.ReportGenerateAll },
            PermissionType.ReportGenerateAll => new() { PermissionType.ReportGenerateOwn },
            PermissionType.CalendarViewOwn => new() { PermissionType.CalendarViewAll },
            PermissionType.CalendarViewAll => new() { PermissionType.CalendarViewOwn },
            _ => new List<PermissionType>()
        };
    }

    /// <summary>
    /// Gets all available permissions
    /// </summary>
    /// <returns>List of all permissions</returns>
    public static List<PermissionType> GetAllPermissions()
    {
        return Enum.GetValues<PermissionType>().ToList();
    }

    /// <summary>
    /// Gets high-risk permissions that require special approval
    /// </summary>
    /// <returns>List of high-risk permissions</returns>
    public static List<PermissionType> GetHighRiskPermissions()
    {
        return Enum.GetValues<PermissionType>()
                   .Where(p => p.GetRiskLevel() >= 4)
                   .ToList();
    }

    /// <summary>
    /// Gets administrative permissions
    /// </summary>
    /// <returns>List of administrative permissions</returns>
    public static List<PermissionType> GetAdministrativePermissions()
    {
        return Enum.GetValues<PermissionType>()
                   .Where(p => p.IsAdministrative())
                   .ToList();
    }
}