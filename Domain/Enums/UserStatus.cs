using System.ComponentModel;

namespace VisitorManagementSystem.Api.Domain.Enums;

/// <summary>
/// Represents the different statuses a user can have
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// User account is active and can access the system
    /// </summary>
    [Description("Active")]
    Active = 1,

    /// <summary>
    /// User account is inactive and cannot access the system
    /// </summary>
    [Description("Inactive")]
    Inactive = 2,

    /// <summary>
    /// User account is pending activation
    /// </summary>
    [Description("Pending")]
    Pending = 3,

    /// <summary>
    /// User account is suspended temporarily
    /// </summary>
    [Description("Suspended")]
    Suspended = 4,

    /// <summary>
    /// User account is locked due to security reasons
    /// </summary>
    [Description("Locked")]
    Locked = 5,

    /// <summary>
    /// User account is archived (soft deleted)
    /// </summary>
    [Description("Archived")]
    Archived = 6,

    /// <summary>
    /// User password has expired
    /// </summary>
    [Description("Password Expired")]
    PasswordExpired = 7,

    /// <summary>
    /// User account requires password change
    /// </summary>
    [Description("Password Change Required")]
    PasswordChangeRequired = 8
}

/// <summary>
/// Extension methods for UserStatus enum
/// </summary>
public static class UserStatusExtensions
{
    /// <summary>
    /// Gets the display name for the user status
    /// </summary>
    /// <param name="status">User status</param>
    /// <returns>Display name</returns>
    public static string GetDisplayName(this UserStatus status)
    {
        var field = typeof(UserStatus).GetField(status.ToString());
        var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                              .FirstOrDefault() as DescriptionAttribute;
        return attribute?.Description ?? status.ToString();
    }

    /// <summary>
    /// Checks if the user status allows login
    /// </summary>
    /// <param name="status">User status</param>
    /// <returns>True if user can login</returns>
    public static bool CanLogin(this UserStatus status)
    {
        return status switch
        {
            UserStatus.Active => true,
            UserStatus.PasswordExpired => true, // Can login but forced to change password
            UserStatus.PasswordChangeRequired => true, // Can login but forced to change password
            _ => false
        };
    }

    /// <summary>
    /// Checks if the user status requires immediate action
    /// </summary>
    /// <param name="status">User status</param>
    /// <returns>True if immediate action is required</returns>
    public static bool RequiresImmediateAction(this UserStatus status)
    {
        return status switch
        {
            UserStatus.Pending => true,
            UserStatus.PasswordExpired => true,
            UserStatus.PasswordChangeRequired => true,
            UserStatus.Locked => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if the user status is temporary
    /// </summary>
    /// <param name="status">User status</param>
    /// <returns>True if status is temporary</returns>
    public static bool IsTemporaryStatus(this UserStatus status)
    {
        return status switch
        {
            UserStatus.Pending => true,
            UserStatus.Suspended => true,
            UserStatus.Locked => true,
            UserStatus.PasswordExpired => true,
            UserStatus.PasswordChangeRequired => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if the user status indicates the account is disabled
    /// </summary>
    /// <param name="status">User status</param>
    /// <returns>True if account is disabled</returns>
    public static bool IsDisabled(this UserStatus status)
    {
        return status switch
        {
            UserStatus.Inactive => true,
            UserStatus.Suspended => true,
            UserStatus.Locked => true,
            UserStatus.Archived => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets the CSS class for displaying the status
    /// </summary>
    /// <param name="status">User status</param>
    /// <returns>CSS class name</returns>
    public static string GetCssClass(this UserStatus status)
    {
        return status switch
        {
            UserStatus.Active => "status-active",
            UserStatus.Inactive => "status-inactive",
            UserStatus.Pending => "status-pending",
            UserStatus.Suspended => "status-suspended",
            UserStatus.Locked => "status-locked",
            UserStatus.Archived => "status-archived",
            UserStatus.PasswordExpired => "status-password-expired",
            UserStatus.PasswordChangeRequired => "status-password-change-required",
            _ => "status-unknown"
        };
    }

    /// <summary>
    /// Gets the color associated with the status
    /// </summary>
    /// <param name="status">User status</param>
    /// <returns>Color name or hex code</returns>
    public static string GetStatusColor(this UserStatus status)
    {
        return status switch
        {
            UserStatus.Active => "#28a745", // Green
            UserStatus.Inactive => "#6c757d", // Gray
            UserStatus.Pending => "#ffc107", // Yellow
            UserStatus.Suspended => "#fd7e14", // Orange
            UserStatus.Locked => "#dc3545", // Red
            UserStatus.Archived => "#343a40", // Dark gray
            UserStatus.PasswordExpired => "#e83e8c", // Pink
            UserStatus.PasswordChangeRequired => "#17a2b8", // Cyan
            _ => "#6c757d" // Default gray
        };
    }

    /// <summary>
    /// Gets the icon associated with the status
    /// </summary>
    /// <param name="status">User status</param>
    /// <returns>Icon name or class</returns>
    public static string GetStatusIcon(this UserStatus status)
    {
        return status switch
        {
            UserStatus.Active => "check-circle",
            UserStatus.Inactive => "minus-circle",
            UserStatus.Pending => "clock",
            UserStatus.Suspended => "pause-circle",
            UserStatus.Locked => "lock",
            UserStatus.Archived => "archive",
            UserStatus.PasswordExpired => "key",
            UserStatus.PasswordChangeRequired => "edit",
            _ => "question-circle"
        };
    }

    /// <summary>
    /// Gets valid status transitions from the current status
    /// </summary>
    /// <param name="currentStatus">Current user status</param>
    /// <returns>List of valid next statuses</returns>
    public static List<UserStatus> GetValidTransitions(this UserStatus currentStatus)
    {
        return currentStatus switch
        {
            UserStatus.Active => new List<UserStatus>
            {
                UserStatus.Inactive,
                UserStatus.Suspended,
                UserStatus.Locked,
                UserStatus.Archived,
                UserStatus.PasswordChangeRequired
            },

            UserStatus.Inactive => new List<UserStatus>
            {
                UserStatus.Active,
                UserStatus.Archived
            },

            UserStatus.Pending => new List<UserStatus>
            {
                UserStatus.Active,
                UserStatus.Inactive,
                UserStatus.Archived
            },

            UserStatus.Suspended => new List<UserStatus>
            {
                UserStatus.Active,
                UserStatus.Inactive,
                UserStatus.Locked,
                UserStatus.Archived
            },

            UserStatus.Locked => new List<UserStatus>
            {
                UserStatus.Active,
                UserStatus.Inactive,
                UserStatus.Suspended,
                UserStatus.Archived
            },

            UserStatus.Archived => new List<UserStatus>
            {
                UserStatus.Active,
                UserStatus.Inactive
            },

            UserStatus.PasswordExpired => new List<UserStatus>
            {
                UserStatus.Active,
                UserStatus.Inactive,
                UserStatus.PasswordChangeRequired
            },

            UserStatus.PasswordChangeRequired => new List<UserStatus>
            {
                UserStatus.Active,
                UserStatus.Inactive
            },

            _ => new List<UserStatus>()
        };
    }

    /// <summary>
    /// Checks if transition from one status to another is valid
    /// </summary>
    /// <param name="fromStatus">Current status</param>
    /// <param name="toStatus">Target status</param>
    /// <returns>True if transition is valid</returns>
    public static bool IsValidTransition(this UserStatus fromStatus, UserStatus toStatus)
    {
        return fromStatus.GetValidTransitions().Contains(toStatus);
    }

    /// <summary>
    /// Gets the reason/description for the status
    /// </summary>
    /// <param name="status">User status</param>
    /// <returns>Status description</returns>
    public static string GetStatusDescription(this UserStatus status)
    {
        return status switch
        {
            UserStatus.Active => "User account is active and fully functional",
            UserStatus.Inactive => "User account is inactive and cannot access the system",
            UserStatus.Pending => "User account is pending activation by an administrator",
            UserStatus.Suspended => "User account is temporarily suspended",
            UserStatus.Locked => "User account is locked due to security reasons",
            UserStatus.Archived => "User account has been archived and is no longer active",
            UserStatus.PasswordExpired => "User password has expired and must be changed",
            UserStatus.PasswordChangeRequired => "User must change their password before continuing",
            _ => "Unknown status"
        };
    }

    /// <summary>
    /// Gets all available user statuses
    /// </summary>
    /// <returns>List of all user statuses</returns>
    public static List<UserStatus> GetAllStatuses()
    {
        return Enum.GetValues<UserStatus>().ToList();
    }

    /// <summary>
    /// Gets statuses that are considered "active" for business purposes
    /// </summary>
    /// <returns>List of active statuses</returns>
    public static List<UserStatus> GetActiveStatuses()
    {
        return new List<UserStatus>
        {
            UserStatus.Active,
            UserStatus.PasswordExpired,
            UserStatus.PasswordChangeRequired
        };
    }

    /// <summary>
    /// Gets statuses that prevent user from accessing the system
    /// </summary>
    /// <returns>List of blocked statuses</returns>
    public static List<UserStatus> GetBlockedStatuses()
    {
        return new List<UserStatus>
        {
            UserStatus.Inactive,
            UserStatus.Suspended,
            UserStatus.Locked,
            UserStatus.Archived
        };
    }

    /// <summary>
    /// Gets statuses that require administrator intervention
    /// </summary>
    /// <returns>List of statuses requiring admin action</returns>
    public static List<UserStatus> GetAdminInterventionStatuses()
    {
        return new List<UserStatus>
        {
            UserStatus.Pending,
            UserStatus.Locked,
            UserStatus.Suspended
        };
    }

    /// <summary>
    /// Checks if the status allows password changes
    /// </summary>
    /// <param name="status">User status</param>
    /// <returns>True if password can be changed</returns>
    public static bool CanChangePassword(this UserStatus status)
    {
        return status switch
        {
            UserStatus.Active => true,
            UserStatus.PasswordExpired => true,
            UserStatus.PasswordChangeRequired => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets the priority level for status (higher number = higher priority)
    /// </summary>
    /// <param name="status">User status</param>
    /// <returns>Priority level</returns>
    public static int GetPriority(this UserStatus status)
    {
        return status switch
        {
            UserStatus.Locked => 5,
            UserStatus.PasswordExpired => 4,
            UserStatus.PasswordChangeRequired => 3,
            UserStatus.Pending => 2,
            UserStatus.Suspended => 1,
            _ => 0
        };
    }
}