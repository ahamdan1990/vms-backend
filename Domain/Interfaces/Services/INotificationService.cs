namespace VisitorManagementSystem.Api.Domain.Interfaces.Services;

/// <summary>
/// Interface for notification operations
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification message
    /// </summary>
    /// <param name="message">Notification message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SendNotificationAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="message">Notification message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SendNotificationAsync(int userId, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to multiple users
    /// </summary>
    /// <param name="userIds">User IDs</param>
    /// <param name="message">Notification message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SendNotificationAsync(IEnumerable<int> userIds, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a templated notification
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="templateName">Template name</param>
    /// <param name="templateData">Template data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SendTemplatedNotificationAsync(int userId, string templateName, object templateData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a system-wide notification
    /// </summary>
    /// <param name="message">Notification message</param>
    /// <param name="notificationType">Type of notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SendSystemNotificationAsync(string message, NotificationType notificationType = NotificationType.Info, CancellationToken cancellationToken = default);
}

/// <summary>
/// Notification types
/// </summary>
public enum NotificationType
{
    Info,
    Warning,
    Error,
    Success
}
