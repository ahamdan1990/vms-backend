using System.ComponentModel;

namespace VisitorManagementSystem.Api.Domain.Enums;

/// <summary>
/// Represents the different types of events that can be logged in the system
/// </summary>
public enum EventType
{
    /// <summary>
    /// Authentication related events
    /// </summary>
    [Description("Authentication")]
    Authentication = 1,

    /// <summary>
    /// Authorization related events
    /// </summary>
    [Description("Authorization")]
    Authorization = 2,

    /// <summary>
    /// User management events
    /// </summary>
    [Description("User Management")]
    UserManagement = 3,

    /// <summary>
    /// Invitation related events
    /// </summary>
    [Description("Invitation")]
    Invitation = 4,

    /// <summary>
    /// Visitor related events
    /// </summary>
    [Description("Visitor")]
    Visitor = 5,

    /// <summary>
    /// Check-in/Check-out events
    /// </summary>
    [Description("Check-In/Out")]
    CheckInOut = 6,

    /// <summary>
    /// Walk-in related events
    /// </summary>
    [Description("Walk-In")]
    WalkIn = 7,

    /// <summary>
    /// Bulk import events
    /// </summary>
    [Description("Bulk Import")]
    BulkImport = 8,

    /// <summary>
    /// Watchlist management events
    /// </summary>
    [Description("Watchlist")]
    Watchlist = 9,

    /// <summary>
    /// Custom field management events
    /// </summary>
    [Description("Custom Field")]
    CustomField = 10,

    /// <summary>
    /// Facial recognition system events
    /// </summary>
    [Description("Facial Recognition")]
    FacialRecognition = 11,

    /// <summary>
    /// System configuration events
    /// </summary>
    [Description("System Configuration")]
    SystemConfiguration = 12,

    /// <summary>
    /// Security related events
    /// </summary>
    [Description("Security")]
    Security = 13,

    /// <summary>
    /// Data export/import events
    /// </summary>
    [Description("Data Export/Import")]
    DataExportImport = 14,

    /// <summary>
    /// Notification events
    /// </summary>
    [Description("Notification")]
    Notification = 15,

    /// <summary>
    /// Report generation events
    /// </summary>
    [Description("Report")]
    Report = 16,

    /// <summary>
    /// System maintenance events
    /// </summary>
    [Description("System Maintenance")]
    SystemMaintenance = 17,

    /// <summary>
    /// API access events
    /// </summary>
    [Description("API Access")]
    ApiAccess = 18,

    /// <summary>
    /// Error events
    /// </summary>
    [Description("Error")]
    Error = 19,

    /// <summary>
    /// Performance events
    /// </summary>
    [Description("Performance")]
    Performance = 20,

    /// <summary>
    /// Integration events (external systems)
    /// </summary>
    [Description("Integration")]
    Integration = 21,

    /// <summary>
    /// General events 
    /// </summary>
    [Description("General")]
    General = 22
}

/// <summary>
/// Extension methods for EventType enum
/// </summary>
public static class EventTypeExtensions
{
    /// <summary>
    /// Gets the display name for the event type
    /// </summary>
    /// <param name="eventType">Event type</param>
    /// <returns>Display name</returns>
    public static string GetDisplayName(this EventType eventType)
    {
        var field = typeof(EventType).GetField(eventType.ToString());
        var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                              .FirstOrDefault() as DescriptionAttribute;
        return attribute?.Description ?? eventType.ToString();
    }

    /// <summary>
    /// Gets the severity level for the event type
    /// </summary>
    /// <param name="eventType">Event type</param>
    /// <returns>Severity level (1 = Low, 5 = Critical)</returns>
    public static int GetSeverityLevel(this EventType eventType)
    {
        return eventType switch
        {
            EventType.Security => 5,
            EventType.Error => 4,
            EventType.Authentication => 4,
            EventType.Authorization => 4,
            EventType.UserManagement => 3,
            EventType.SystemConfiguration => 3,
            EventType.FacialRecognition => 3,
            EventType.Watchlist => 3,
            EventType.SystemMaintenance => 2,
            EventType.BulkImport => 2,
            EventType.DataExportImport => 2,
            EventType.Integration => 2,
            EventType.Performance => 2,
            EventType.Invitation => 1,
            EventType.Visitor => 1,
            EventType.CheckInOut => 1,
            EventType.WalkIn => 1,
            EventType.CustomField => 1,
            EventType.Notification => 1,
            EventType.Report => 1,
            EventType.ApiAccess => 1,
            _ => 1
        };
    }

    /// <summary>
    /// Checks if the event type is security-related
    /// </summary>
    /// <param name="eventType">Event type</param>
    /// <returns>True if security-related</returns>
    public static bool IsSecurityRelated(this EventType eventType)
    {
        return eventType switch
        {
            EventType.Authentication => true,
            EventType.Authorization => true,
            EventType.Security => true,
            EventType.UserManagement => true,
            EventType.Watchlist => true,
            EventType.FacialRecognition => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if the event type requires immediate attention
    /// </summary>
    /// <param name="eventType">Event type</param>
    /// <returns>True if requires immediate attention</returns>
    public static bool RequiresImmediateAttention(this EventType eventType)
    {
        return eventType switch
        {
            EventType.Security => true,
            EventType.Error => true,
            EventType.FacialRecognition => true, // For unknown faces or security alerts
            _ => false
        };
    }

    /// <summary>
    /// Gets the color associated with the event type for UI display
    /// </summary>
    /// <param name="eventType">Event type</param>
    /// <returns>Color hex code</returns>
    public static string GetEventColor(this EventType eventType)
    {
        return eventType switch
        {
            EventType.Security => "#dc3545", // Red
            EventType.Error => "#fd7e14", // Orange
            EventType.Authentication => "#6f42c1", // Purple
            EventType.Authorization => "#6610f2", // Indigo
            EventType.UserManagement => "#0d6efd", // Blue
            EventType.FacialRecognition => "#d63384", // Pink
            EventType.Watchlist => "#20c997", // Teal
            EventType.SystemConfiguration => "#198754", // Green
            EventType.BulkImport => "#ffc107", // Yellow
            EventType.CheckInOut => "#17a2b8", // Cyan
            EventType.Integration => "#6c757d", // Gray
            _ => "#212529" // Dark
        };
    }

    /// <summary>
    /// Gets the icon associated with the event type
    /// </summary>
    /// <param name="eventType">Event type</param>
    /// <returns>Icon class name</returns>
    public static string GetEventIcon(this EventType eventType)
    {
        return eventType switch
        {
            EventType.Authentication => "key",
            EventType.Authorization => "shield-check",
            EventType.UserManagement => "users",
            EventType.Invitation => "mail",
            EventType.Visitor => "user",
            EventType.CheckInOut => "log-in",
            EventType.WalkIn => "user-plus",
            EventType.BulkImport => "upload",
            EventType.Watchlist => "eye",
            EventType.CustomField => "edit-3",
            EventType.FacialRecognition => "camera",
            EventType.SystemConfiguration => "settings",
            EventType.Security => "shield-alert",
            EventType.DataExportImport => "download",
            EventType.Notification => "bell",
            EventType.Report => "file-text",
            EventType.SystemMaintenance => "tool",
            EventType.ApiAccess => "globe",
            EventType.Error => "alert-triangle",
            EventType.Performance => "activity",
            EventType.Integration => "link",
            _ => "info"
        };
    }

    /// <summary>
    /// Gets the category for grouping event types
    /// </summary>
    /// <param name="eventType">Event type</param>
    /// <returns>Category name</returns>
    public static string GetCategory(this EventType eventType)
    {
        return eventType switch
        {
            EventType.Authentication or EventType.Authorization or EventType.Security => "Security",
            EventType.UserManagement => "User Management",
            EventType.Invitation or EventType.Visitor or EventType.CheckInOut or EventType.WalkIn => "Visitor Management",
            EventType.BulkImport or EventType.DataExportImport => "Data Management",
            EventType.Watchlist or EventType.FacialRecognition => "Security Systems",
            EventType.CustomField or EventType.SystemConfiguration => "Configuration",
            EventType.Notification or EventType.Report => "Communication",
            EventType.SystemMaintenance or EventType.Performance or EventType.Error => "System",
            EventType.ApiAccess or EventType.Integration => "Integration",
            _ => "General"
        };
    }

    /// <summary>
    /// Checks if the event type should be retained for compliance
    /// </summary>
    /// <param name="eventType">Event type</param>
    /// <returns>True if should be retained for compliance</returns>
    public static bool ShouldRetainForCompliance(this EventType eventType)
    {
        return eventType switch
        {
            EventType.Authentication => true,
            EventType.Authorization => true,
            EventType.Security => true,
            EventType.UserManagement => true,
            EventType.Visitor => true,
            EventType.CheckInOut => true,
            EventType.Watchlist => true,
            EventType.FacialRecognition => true,
            EventType.DataExportImport => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets the default retention period for the event type in days
    /// </summary>
    /// <param name="eventType">Event type</param>
    /// <returns>Retention period in days</returns>
    public static int GetRetentionPeriodDays(this EventType eventType)
    {
        return eventType switch
        {
            EventType.Security => 2555, // 7 years
            EventType.Authentication => 1095, // 3 years
            EventType.Authorization => 1095, // 3 years
            EventType.UserManagement => 1095, // 3 years
            EventType.Visitor => 1095, // 3 years
            EventType.CheckInOut => 1095, // 3 years
            EventType.Watchlist => 1095, // 3 years
            EventType.FacialRecognition => 1095, // 3 years
            EventType.DataExportImport => 730, // 2 years
            EventType.BulkImport => 730, // 2 years
            EventType.SystemConfiguration => 730, // 2 years
            EventType.Error => 365, // 1 year
            EventType.Performance => 90, // 3 months
            EventType.ApiAccess => 90, // 3 months
            _ => 365 // 1 year default
        };
    }

    /// <summary>
    /// Gets all event types in a specific category
    /// </summary>
    /// <param name="category">Category name</param>
    /// <returns>List of event types in the category</returns>
    public static List<EventType> GetEventTypesByCategory(string category)
    {
        return Enum.GetValues<EventType>()
                   .Where(et => et.GetCategory().Equals(category, StringComparison.OrdinalIgnoreCase))
                   .ToList();
    }

    /// <summary>
    /// Gets all available event types
    /// </summary>
    /// <returns>List of all event types</returns>
    public static List<EventType> GetAllEventTypes()
    {
        return Enum.GetValues<EventType>().ToList();
    }

    /// <summary>
    /// Gets all security-related event types
    /// </summary>
    /// <returns>List of security-related event types</returns>
    public static List<EventType> GetSecurityEventTypes()
    {
        return Enum.GetValues<EventType>()
                   .Where(et => et.IsSecurityRelated())
                   .ToList();
    }

    /// <summary>
    /// Gets all event types that require immediate attention
    /// </summary>
    /// <returns>List of event types requiring immediate attention</returns>
    public static List<EventType> GetCriticalEventTypes()
    {
        return Enum.GetValues<EventType>()
                   .Where(et => et.RequiresImmediateAttention())
                   .ToList();
    }

    /// <summary>
    /// Gets all unique categories
    /// </summary>
    /// <returns>List of all categories</returns>
    public static List<string> GetAllCategories()
    {
        return Enum.GetValues<EventType>()
                   .Select(et => et.GetCategory())
                   .Distinct()
                   .OrderBy(c => c)
                   .ToList();
    }
}