using MediatR;

namespace VisitorManagementSystem.Api.Application.Commands.Notifications;

/// <summary>
/// Command to delete a specific notification alert
/// </summary>
public class DeleteNotificationCommand : IRequest<bool>
{
    /// <summary>
    /// Notification ID to delete
    /// </summary>
    public int NotificationId { get; set; }

    /// <summary>
    /// User deleting the notification
    /// </summary>
    public int DeletedBy { get; set; }

    /// <summary>
    /// Whether to perform a permanent delete (true) or soft delete (false)
    /// </summary>
    public bool PermanentDelete { get; set; } = false;
}

/// <summary>
/// Command to delete all notifications for the current user
/// </summary>
public class DeleteAllNotificationsCommand : IRequest<bool>
{
    /// <summary>
    /// User deleting the notifications
    /// </summary>
    public int DeletedBy { get; set; }

    /// <summary>
    /// Whether to perform a permanent delete (true) or soft delete (false)
    /// </summary>
    public bool PermanentDelete { get; set; } = false;
}
