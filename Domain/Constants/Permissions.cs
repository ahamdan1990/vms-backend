namespace VisitorManagementSystem.Api.Domain.Constants;

/// <summary>
/// Contains all permission constants used throughout the application
/// </summary>
public static class Permissions
{

    /// <summary>
    /// User management permissions
    /// </summary>
    public static class User
    {
        public const string Create = "User.Create";
        public const string Read = "User.Read";
        public const string ReadAll = "User.Read.All";
        public const string Update = "User.Update";
        public const string UpdateAll = "User.Update.All";
        public const string Delete = "User.Delete";
        public const string DeleteAll = "User.Delete.All";
        public const string Activate = "User.Activate";
        public const string Deactivate = "User.Deactivate";
        public const string ManageRoles = "User.ManageRoles";
        public const string ManagePermissions = "User.ManagePermissions";
        public const string ViewActivity = "User.ViewActivity";
        public const string ResetPassword = "User.ResetPassword";
    }

    /// <summary>
    /// Invitation management permissions
    /// </summary>
    public static class Invitation
    {
        public const string CreateOwn = "Invitation.Create.Own";
        public const string CreateAll = "Invitation.Create.All";
        public const string Create = "Invitation.Create"; // General create permission
        public const string ReadOwn = "Invitation.Read.Own";
        public const string ReadAll = "Invitation.Read.All";
        public const string Read = "Invitation.Read"; // General read permission
        public const string UpdateOwn = "Invitation.Update.Own";
        public const string UpdateAll = "Invitation.Update.All";
        public const string Delete = "Invitation.Delete"; // Delete cancelled invitations
        public const string ApproveAll = "Invitation.Approve.All";
        public const string Approve = "Invitation.Approve"; // General approve permission
        public const string DenyAll = "Invitation.Deny.All";
        public const string CancelOwn = "Invitation.Cancel.Own";
        public const string CancelAll = "Invitation.Cancel.All";
        public const string ViewPending = "Invitation.View.Pending";
        public const string BulkApprove = "Invitation.BulkApprove";
        public const string ViewHistory = "Invitation.ViewHistory";
        public const string Export = "Invitation.Export";
    }

    /// <summary>
    /// Visitor management permissions
    /// </summary>
    public static class Visitor
    {
        public const string Create = "Visitor.Create";
        public const string Read = "Visitor.Read";
        public const string ReadToday = "Visitor.Read.Today";
        public const string ReadAll = "Visitor.Read.All";
        public const string Update = "Visitor.Update";
        public const string Delete = "Visitor.Delete";
        public const string Archive = "Visitor.Archive";
        public const string ViewHistory = "Visitor.ViewHistory";
        public const string Search = "Visitor.Search";
        public const string Export = "Visitor.Export";
        public const string ViewPersonalInfo = "Visitor.ViewPersonalInfo";
        public const string ManagePhotos = "Visitor.ManagePhotos";
        public const string Blacklist = "Visitor.Blacklist";
        public const string RemoveBlacklist = "Visitor.RemoveBlacklist";
        public const string MarkAsVip = "Visitor.MarkAsVip";
        public const string RemoveVipStatus = "Visitor.RemoveVipStatus";
        public const string ViewStatistics = "Visitor.ViewStatistics";
    }

    /// <summary>
    /// Visitor document permissions
    /// </summary>
    public static class VisitorDocument
    {
        public const string Create = "VisitorDocument.Create";
        public const string Read = "VisitorDocument.Read";
        public const string Update = "VisitorDocument.Update";
        public const string Delete = "VisitorDocument.Delete";
        public const string Download = "VisitorDocument.Download";
        public const string Upload = "VisitorDocument.Upload";
        public const string ViewSensitive = "VisitorDocument.ViewSensitive";
    }

    /// <summary>
    /// Visitor note permissions
    /// </summary>
    public static class VisitorNote
    {
        public const string Create = "VisitorNote.Create";
        public const string Read = "VisitorNote.Read";
        public const string Update = "VisitorNote.Update";
        public const string Delete = "VisitorNote.Delete";
        public const string ViewConfidential = "VisitorNote.ViewConfidential";
        public const string ViewFlagged = "VisitorNote.ViewFlagged";
    }

    /// <summary>
    /// Camera management permissions
    /// </summary>
    public static class Camera
    {
        public const string Create = "Camera.Create";
        public const string Read = "Camera.Read";
        public const string ReadAll = "Camera.Read.All";
        public const string Update = "Camera.Update";
        public const string Delete = "Camera.Delete";
        public const string TestConnection = "Camera.TestConnection";
        public const string ManageStreaming = "Camera.ManageStreaming";
        public const string StartStream = "Camera.StartStream";
        public const string StopStream = "Camera.StopStream";
        public const string ViewStream = "Camera.ViewStream";
        public const string ManageFacialRecognition = "Camera.ManageFacialRecognition";
        public const string ViewFrames = "Camera.ViewFrames";
        public const string CaptureFrame = "Camera.CaptureFrame";
        public const string Configure = "Camera.Configure";
        public const string ViewConfiguration = "Camera.ViewConfiguration";
        public const string ManageCredentials = "Camera.ManageCredentials";
        public const string ViewStatus = "Camera.ViewStatus";
        public const string HealthCheck = "Camera.HealthCheck";
        public const string ViewStatistics = "Camera.ViewStatistics";
        public const string BulkOperations = "Camera.BulkOperations";
        public const string Export = "Camera.Export";
        public const string ViewHistory = "Camera.ViewHistory";
        public const string Maintenance = "Camera.Maintenance";
        public const string ViewSensitiveData = "Camera.ViewSensitiveData";
        public const string AdministerAll = "Camera.AdministerAll";
    }

    /// <summary>
    /// Emergency contact permissions
    /// </summary>
    public static class EmergencyContact
    {
        public const string Create = "EmergencyContact.Create";
        public const string Read = "EmergencyContact.Read";
        public const string Update = "EmergencyContact.Update";
        public const string Delete = "EmergencyContact.Delete";
        public const string ViewPersonalInfo = "EmergencyContact.ViewPersonalInfo";
    }

    /// <summary>
    /// Check-in/Check-out permissions
    /// </summary>
    public static class CheckIn
    {
        public const string Process = "CheckIn.Process";
        public const string ProcessOut = "CheckOut.Process";
        public const string Override = "CheckIn.Override";
        public const string ViewQueue = "CheckIn.ViewQueue";
        public const string ManualEntry = "CheckIn.ManualEntry";
        public const string ViewHistory = "CheckIn.ViewHistory";
        public const string PrintBadge = "CheckIn.PrintBadge";
        public const string QRScan = "CheckIn.QRScan";
        public const string PhotoCapture = "CheckIn.PhotoCapture";
        public const string ManualVerification = "CheckIn.ManualVerification";
    }

    /// <summary>
    /// Walk-in management permissions
    /// </summary>
    public static class WalkIn
    {
        public const string Register = "WalkIn.Register";
        public const string CheckIn = "WalkIn.CheckIn";
        public const string Convert = "WalkIn.Convert";
        public const string ViewList = "WalkIn.ViewList";
        public const string Update = "WalkIn.Update";
        public const string Delete = "WalkIn.Delete";
        public const string FRSync = "WalkIn.FRSync";
        public const string QuickRegister = "WalkIn.QuickRegister";
        public const string ViewHistory = "WalkIn.ViewHistory";
    }

    /// <summary>
    /// Bulk import permissions
    /// </summary>
    public static class BulkImport
    {
        public const string Create = "BulkImport.Create";
        public const string Process = "BulkImport.Process";
        public const string Validate = "BulkImport.Validate";
        public const string Cancel = "BulkImport.Cancel";
        public const string ViewBatches = "BulkImport.ViewBatches";
        public const string ViewErrors = "BulkImport.ViewErrors";
        public const string CorrectErrors = "BulkImport.CorrectErrors";
        public const string ViewHistory = "BulkImport.ViewHistory";
        public const string Export = "BulkImport.Export";
        public const string RetryFailed = "BulkImport.RetryFailed";
    }

    /// <summary>
    /// Watchlist management permissions
    /// </summary>
    public static class Watchlist
    {
        public const string Create = "Watchlist.Create";
        public const string ReadAll = "Watchlist.Read.All";
        public const string Update = "Watchlist.Update";
        public const string Delete = "Watchlist.Delete";
        public const string Assign = "Watchlist.Assign";
        public const string Unassign = "Watchlist.Unassign";
        public const string ViewAssignments = "Watchlist.ViewAssignments";
        public const string SyncWithFR = "Watchlist.SyncWithFR";
        public const string ManageVIP = "Watchlist.ManageVIP";
        public const string ManageBlacklist = "Watchlist.ManageBlacklist";
        public const string ViewSyncStatus = "Watchlist.ViewSyncStatus";
    }

    /// <summary>
    /// Custom field management permissions
    /// </summary>
    public static class CustomField
    {
        public const string Create = "CustomField.Create";
        public const string ReadAll = "CustomField.Read.All";
        public const string Update = "CustomField.Update";
        public const string Delete = "CustomField.Delete";
        public const string Reorder = "CustomField.Reorder";
        public const string Configure = "CustomField.Configure";
        public const string ViewUsage = "CustomField.ViewUsage";
        public const string ManageValidation = "CustomField.ManageValidation";
        public const string BuildForms = "CustomField.BuildForms";
    }

    /// <summary>
    /// Facial recognition system permissions
    /// </summary>
    public static class FRSystem
    {
        public const string Configure = "FRSystem.Configure";
        public const string Sync = "FRSystem.Sync";
        public const string Monitor = "FRSystem.Monitor";
        public const string ViewHealth = "FRSystem.ViewHealth";
        public const string ManageProfiles = "FRSystem.ManageProfiles";
        public const string ProcessEvents = "FRSystem.ProcessEvents";
        public const string ViewSyncQueue = "FRSystem.ViewSyncQueue";
        public const string Reconcile = "FRSystem.Reconcile";
        public const string ViewLogs = "FRSystem.ViewLogs";
        public const string ConfigureWebhooks = "FRSystem.ConfigureWebhooks";
    }

    /// <summary>
    /// Alert management permissions
    /// </summary>
    public static class Alert
    {
        public const string Receive = "Alert.Receive";
        public const string Acknowledge = "Alert.Acknowledge";
        public const string ViewHistory = "Alert.ViewHistory";
        public const string Configure = "Alert.Configure";
        public const string Escalate = "Alert.Escalate";
        public const string Dismiss = "Alert.Dismiss";
        public const string ViewFRAlerts = "Alert.ViewFRAlerts";
        public const string ViewVIPAlerts = "Alert.ViewVIPAlerts";
        public const string ViewBlacklistAlerts = "Alert.ViewBlacklistAlerts";
        public const string ViewSystemAlerts = "Alert.ViewSystemAlerts";
    }

    /// <summary>
    /// System configuration permissions
    /// </summary>
    public static class SystemConfig
    {
        public const string Create = "SystemConfig.Create";
        public const string Read = "SystemConfig.Read";
        public const string Update = "SystemConfig.Update";
        public const string Delete = "SystemConfig.Delete";
        public const string ViewAll = "SystemConfig.ViewAll";
        public const string ManageIntegrations = "SystemConfig.ManageIntegrations";
        public const string ManageNotifications = "SystemConfig.ManageNotifications";
        public const string ManageSecurity = "SystemConfig.ManageSecurity";
        public const string ManageCapacity = "SystemConfig.ManageCapacity";
        public const string ViewLogs = "SystemConfig.ViewLogs";
        public const string Backup = "SystemConfig.Backup";
        public const string Restore = "SystemConfig.Restore";
    }

    /// <summary>
    /// Dynamic configuration management permissions
    /// </summary>
    public static class Configuration
    {
        public const string Read = "Configuration.Read";
        public const string ReadAll = "Configuration.Read.All";
        public const string Update = "Configuration.Update";
        public const string UpdateAll = "Configuration.Update.All";
        public const string Create = "Configuration.Create";
        public const string Delete = "Configuration.Delete";
        public const string ViewHistory = "Configuration.ViewHistory";
        public const string ViewAudit = "Configuration.ViewAudit";
        public const string ManageEncrypted = "Configuration.ManageEncrypted";
        public const string ManageSecurity = "Configuration.ManageSecurity";
        public const string ManageJWT = "Configuration.ManageJWT";
        public const string ManagePassword = "Configuration.ManagePassword";
        public const string ManageLockout = "Configuration.ManageLockout";
        public const string ManageRateLimit = "Configuration.ManageRateLimit";
        public const string ManageLogging = "Configuration.ManageLogging";
        public const string Export = "Configuration.Export";
        public const string Import = "Configuration.Import";
        public const string InvalidateCache = "Configuration.InvalidateCache";
        public const string ViewSensitive = "Configuration.ViewSensitive";
        public const string ResetToDefaults = "Configuration.ResetToDefaults";
    }

    /// <summary>
    /// Report management permissions
    /// </summary>
    public static class Report
    {
        public const string GenerateOwn = "Report.Generate.Own";
        public const string GenerateAll = "Report.Generate.All";
        public const string Schedule = "Report.Schedule";
        public const string Export = "Report.Export";
        public const string ViewHistory = "Report.ViewHistory";
        public const string CreateTemplates = "Report.CreateTemplates";
        public const string ManageSubscriptions = "Report.ManageSubscriptions";
        public const string ViewAnalytics = "Report.ViewAnalytics";
        public const string ExportData = "Report.ExportData";
        public const string ViewMetrics = "Report.ViewMetrics";
    }

    /// <summary>
    /// Audit management permissions
    /// </summary>
    public static class Audit
    {
        public const string ReadAll = "Audit.Read.All";
        public const string Export = "Audit.Export";
        public const string ViewUserActivity = "Audit.ViewUserActivity";
        public const string ViewSystemEvents = "Audit.ViewSystemEvents";
        public const string ViewSecurityEvents = "Audit.ViewSecurityEvents";
        public const string Search = "Audit.Search";
        public const string Review = "Audit.Review";
        public const string Archive = "Audit.Archive";
        public const string Purge = "Audit.Purge";
    }

    /// <summary>
    /// Template management permissions
    /// </summary>
    public static class Template
    {
        public const string DownloadSingle = "Template.Download.Single";
        public const string DownloadBulk = "Template.Download.Bulk";
        public const string Create = "Template.Create";
        public const string Update = "Template.Update";
        public const string Delete = "Template.Delete";
        public const string ViewAll = "Template.ViewAll";
        public const string Share = "Template.Share";
        public const string Customize = "Template.Customize";
        public const string ManageBadge = "Template.ManageBadge";
    }

    /// <summary>
    /// Dashboard permissions
    /// </summary>
    public static class Dashboard
    {
        public const string ViewBasic = "Dashboard.View.Basic";
        public const string ViewOperations = "Dashboard.View.Operations";
        public const string ViewAdmin = "Dashboard.View.Admin";
        public const string ViewAnalytics = "Dashboard.View.Analytics";
        public const string Customize = "Dashboard.Customize";
        public const string Export = "Dashboard.Export";
        public const string ViewRealTime = "Dashboard.ViewRealTime";
        public const string ViewMetrics = "Dashboard.ViewMetrics";
    }

    /// <summary>
    /// Profile management permissions
    /// </summary>
    public static class Profile
    {
        public const string UpdateOwn = "Profile.Update.Own";
        public const string UpdateAll = "Profile.Update.All";
        public const string ViewOwn = "Profile.View.Own";
        public const string ViewAll = "Profile.View.All";
        public const string ChangePassword = "Profile.ChangePassword";
        public const string ManagePreferences = "Profile.ManagePreferences";
        public const string UploadPhoto = "Profile.UploadPhoto";
        public const string ViewActivity = "Profile.ViewActivity";
    }

    /// <summary>
    /// Notification permissions
    /// </summary>
    public static class Notification
    {
        public const string ReadOwn = "Notification.Read.Own";
        public const string ReadAll = "Notification.Read.All";
        public const string SendHost = "Notification.Send.Host";
        public const string SendSystem = "Notification.Send.System";
        public const string Configure = "Notification.Configure";
        public const string ViewHistory = "Notification.ViewHistory";
        public const string ManageTemplates = "Notification.ManageTemplates";
        public const string SendBulk = "Notification.SendBulk";
        public const string ViewQueue = "Notification.ViewQueue";
    }

    /// <summary>
    /// Calendar permissions
    /// </summary>
    public static class Calendar
    {
        public const string ViewOwn = "Calendar.View.Own";
        public const string ViewAll = "Calendar.View.All";
        public const string Manage = "Calendar.Manage";
        public const string Export = "Calendar.Export";
        public const string ViewAvailability = "Calendar.ViewAvailability";
        public const string BookSlots = "Calendar.BookSlots";
        public const string ViewConflicts = "Calendar.ViewConflicts";
    }

    /// <summary>
    /// Emergency management permissions
    /// </summary>
    public static class Emergency
    {
        public const string Export = "Emergency.Export";
        public const string GenerateRoster = "Emergency.GenerateRoster";
        public const string ViewRoster = "Emergency.ViewRoster";
        public const string PrintRoster = "Emergency.PrintRoster";
        public const string Lockdown = "Emergency.Lockdown";
        public const string Evacuate = "Emergency.Evacuate";
        public const string ViewEvacuationList = "Emergency.ViewEvacuationList";
    }

    /// <summary>
    /// Badge management permissions
    /// </summary>
    public static class Badge
    {
        public const string Print = "Badge.Print";
        public const string Design = "Badge.Design";
        public const string Configure = "Badge.Configure";
        public const string ViewQueue = "Badge.ViewQueue";
        public const string ReprintLost = "Badge.ReprintLost";
        public const string ManageTemplates = "Badge.ManageTemplates";
        public const string ViewHistory = "Badge.ViewHistory";
    }

    /// <summary>
    /// QR Code permissions
    /// </summary>
    public static class QRCode
    {
        public const string Scan = "QRCode.Scan";
        public const string Generate = "QRCode.Generate";
        public const string Validate = "QRCode.Validate";
        public const string ViewHistory = "QRCode.ViewHistory";
        public const string Configure = "QRCode.Configure";
    }

    /// <summary>
    /// Manual operation permissions
    /// </summary>
    public static class Manual
    {
        public const string Override = "Manual.Override";
        public const string Verification = "Manual.Verification";
        public const string Entry = "Manual.Entry";
        public const string CreateOverrideLog = "Manual.CreateOverrideLog";
        public const string ViewOverrideLogs = "Manual.ViewOverrideLogs";
        public const string ApproveOverride = "Manual.ApproveOverride";
    }

    /// <summary>
    /// Integration permissions
    /// </summary>
    public static class Integration
    {
        public const string Configure = "Integration.Configure";
        public const string Monitor = "Integration.Monitor";
        public const string ViewLogs = "Integration.ViewLogs";
        public const string TestConnection = "Integration.TestConnection";
        public const string ManageKeys = "Integration.ManageKeys";
        public const string ViewHealth = "Integration.ViewHealth";
    }

    /// <summary>
    /// Sync operation permissions
    /// </summary>
    public static class Sync
    {
        public const string Monitor = "Sync.Monitor";
        public const string Reconcile = "Sync.Reconcile";
        public const string ForceSync = "Sync.ForceSync";
        public const string ViewQueue = "Sync.ViewQueue";
        public const string ResolvConflicts = "Sync.ResolveConflicts";
        public const string ViewHistory = "Sync.ViewHistory";
        public const string Configure = "Sync.Configure";
    }

    /// <summary>
    /// Offline operation permissions
    /// </summary>
    public static class Offline
    {
        public const string Monitor = "Offline.Monitor";
        public const string Process = "Offline.Process";
        public const string ViewQueue = "Offline.ViewQueue";
        public const string RetryFailed = "Offline.RetryFailed";
        public const string PurgeCompleted = "Offline.PurgeCompleted";
        public const string ViewStatus = "Offline.ViewStatus";
    }

    /// <summary>
    /// Gets all permissions as a flat list
    /// </summary>
    /// <returns>List of all permission strings</returns>
    public static List<string> GetAllPermissions()
    {
        var permissions = new List<string>();
        var permissionClasses = typeof(Permissions).GetNestedTypes()
            .Where(t => t.IsClass && t.IsSealed);

        foreach (var permissionClass in permissionClasses)
        {
            var fields = permissionClass.GetFields()
                .Where(f => f.IsStatic && f.FieldType == typeof(string));

            foreach (var field in fields)
            {
                var value = field.GetValue(null) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    permissions.Add(value);
                }
            }
        }

        return permissions.OrderBy(p => p).ToList();
    }

    /// <summary>
    /// Gets permissions grouped by category
    /// </summary>
    /// <returns>Dictionary of category to permissions</returns>
    public static Dictionary<string, List<string>> GetPermissionsByCategory()
    {
        var result = new Dictionary<string, List<string>>();
        var permissionClasses = typeof(Permissions).GetNestedTypes()
            .Where(t => t.IsClass && t.IsSealed);

        foreach (var permissionClass in permissionClasses)
        {
            var categoryName = permissionClass.Name;
            var permissions = new List<string>();

            var fields = permissionClass.GetFields()
                .Where(f => f.IsStatic && f.FieldType == typeof(string));

            foreach (var field in fields)
            {
                var value = field.GetValue(null) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    permissions.Add(value);
                }
            }

            if (permissions.Any())
            {
                result[categoryName] = permissions.OrderBy(p => p).ToList();
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if a permission string is valid
    /// </summary>
    /// <param name="permission">Permission string to validate</param>
    /// <returns>True if valid permission</returns>
    public static bool IsValidPermission(string permission)
    {
        return GetAllPermissions().Contains(permission);
    }

    /// <summary>
    /// Gets the category for a given permission
    /// </summary>
    /// <param name="permission">Permission string</param>
    /// <returns>Category name or null if not found</returns>
    public static string? GetPermissionCategory(string permission)
    {
        var categorizedPermissions = GetPermissionsByCategory();

        foreach (var category in categorizedPermissions)
        {
            if (category.Value.Contains(permission))
            {
                return category.Key;
            }
        }

        return null;
    }
}