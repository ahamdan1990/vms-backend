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
        public const string ReadOwn = "Visitor.Read.Own";
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

    // REMOVED: Camera permissions - Camera management handled by external FR application

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
    /// Watchlist management permissions (simplified)
    /// </summary>
    public static class Watchlist
    {
        public const string View = "Watchlist.View";                 // View blacklist and VIP lists
        public const string ManageBlacklist = "Watchlist.ManageBlacklist"; // Add/remove from blacklist
        public const string ManageVIP = "Watchlist.ManageVIP";        // Add/remove VIP status
        // Note: FR sync permissions removed until FR integration implemented
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

    // REMOVED: Alert permissions - Merged into Notification class below

    /// <summary>
    /// Visit purpose management permissions
    /// </summary>
    public static class VisitPurpose
    {
        public const string Read = "VisitPurpose.Read";
        public const string ReadAll = "VisitPurpose.Read.All";
        public const string Create = "VisitPurpose.Create";
        public const string Update = "VisitPurpose.Update";
        public const string Delete = "VisitPurpose.Delete";
    }

    /// <summary>
    /// Location management permissions
    /// </summary>
    public static class Location
    {
        public const string Read = "Location.Read";
        public const string ReadAll = "Location.Read.All";
        public const string Create = "Location.Create";
        public const string Update = "Location.Update";
        public const string Delete = "Location.Delete";
    }

    /// <summary>
    /// System configuration permissions (simplified)
    /// </summary>
    public static class SystemConfig
    {
        public const string Read = "SystemConfig.Read";
        public const string Update = "SystemConfig.Update";
        // Note: Backup/Restore removed until properly implemented with safety checks
    }

    // REMOVED: Configuration permissions - Merged into SystemConfig (over-engineered for current needs)

    /// <summary>
    /// Report management permissions (simplified)
    /// </summary>
    public static class Report
    {
        public const string GenerateOwn = "Report.Generate.Own";
        public const string GenerateAll = "Report.Generate.All";
        public const string Export = "Report.Export";
        public const string ViewHistory = "Report.ViewHistory";
        // Note: Schedule and Templates removed until report scheduling is implemented
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
    /// Dashboard permissions (simplified)
    /// </summary>
    public static class Dashboard
    {
        public const string ViewBasic = "Dashboard.View.Basic";       // Staff - basic invitation stats
        public const string ViewOperations = "Dashboard.View.Operations"; // Receptionist - check-in queue, today's visitors
        public const string ViewAdmin = "Dashboard.View.Admin";       // Administrator - full system dashboard
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
    /// Notification and Alert permissions (consolidated)
    /// </summary>
    public static class Notification
    {
        // Basic notification permissions
        public const string ReadOwn = "Notification.Read.Own";
        public const string ReadAll = "Notification.Read.All";
        public const string SendSystem = "Notification.Send.System";
        public const string SendBulk = "Notification.SendBulk";

        // Alert permissions (merged from Alert class)
        public const string Receive = "Notification.Receive"; // Receive alerts/notifications
        public const string Acknowledge = "Notification.Acknowledge"; // Acknowledge alerts/notifications
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
    /// Badge management permissions (simplified)
    /// </summary>
    public static class Badge
    {
        public const string Print = "Badge.Print";                    // Print visitor badge at check-in
        public const string ReprintLost = "Badge.ReprintLost";        // Reprint lost/damaged badge
        // Note: Template management removed until badge designer implemented
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

    // REMOVED: Manual permissions - Merged into CheckIn.Override and CheckIn.ManualVerification

    // REMOVED: Integration permissions - Not implemented, defer to Phase 3

    // REMOVED: Sync permissions - No external integrations exist

    // REMOVED: Offline permissions - Not needed for cloud-based system

    /// <summary>
    /// Role management permissions
    /// </summary>
    public static class Role
    {
        public const string Create = "Role.Create";
        public const string Read = "Role.Read";
        public const string ReadAll = "Role.ReadAll";
        public const string Update = "Role.Update";
        public const string Delete = "Role.Delete";
        public const string ManagePermissions = "Role.ManagePermissions";
        public const string ViewUsers = "Role.ViewUsers";
    }

    /// <summary>
    /// Permission management permissions
    /// </summary>
    public static class Permission
    {
        public const string Read = "Permission.Read";
        public const string ReadAll = "Permission.ReadAll";
        public const string Grant = "Permission.Grant";
        public const string Revoke = "Permission.Revoke";
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