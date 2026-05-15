using MediatR;

namespace VisitorManagementSystem.Api.Application.Commands.Notifications;

/// <summary>
/// Command to acknowledge a notification alert
/// </summary>
public class AcknowledgeNotificationCommand : IRequest<bool>
{
    /// <summary>
    /// Notification ID to acknowledge
    /// </summary>
    public int NotificationId { get; set; }

    /// <summary>
    /// Optional acknowledgment notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// User acknowledging the notification
    /// </summary>
    public int AcknowledgedBy { get; set; }

    /// <summary>
    /// Whether the acknowledging user has administrative access to acknowledge notifications outside their own target role/user.
    /// </summary>
    public bool CanAcknowledgeAll { get; set; }
}
