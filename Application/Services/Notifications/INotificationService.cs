using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Services.Notifications;

/// <summary>
/// Service interface for real-time notifications via SignalR
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send notification to specific user
    /// </summary>
    Task NotifyUserAsync(int userId, string title, string message, NotificationAlertType type, 
        AlertPriority priority = AlertPriority.Medium, object? data = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send notification to all users with specific role
    /// </summary>
    Task NotifyRoleAsync(string role, string title, string message, NotificationAlertType type, 
        AlertPriority priority = AlertPriority.Medium, object? data = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send notification to operators at specific location
    /// </summary>
    Task NotifyOperatorsAsync(int? locationId, string title, string message, NotificationAlertType type, 
        AlertPriority priority = AlertPriority.Medium, object? data = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send visitor arrival notification to host
    /// </summary>
    Task NotifyHostOfVisitorArrivalAsync(int hostId, int visitorId, string visitorName, 
        DateTime arrivalTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send VIP arrival alert to security and operators
    /// </summary>
    Task NotifyVipArrivalAsync(string visitorName, string location, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send blacklist detection alert to security
    /// </summary>
    Task NotifyBlacklistDetectionAsync(string personDescription, string cameraLocation, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send unknown face detection alert to operators
    /// </summary>
    Task NotifyUnknownFaceDetectionAsync(string cameraLocation, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send invitation approval notification
    /// </summary>
    Task NotifyInvitationApprovalAsync(int invitationId, int hostId, bool approved, 
        string? note = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send system alert to administrators
    /// </summary>
    Task NotifySystemAlertAsync(string title, string message, AlertPriority priority, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send FR system offline alert
    /// </summary>
    Task NotifyFRSystemOfflineAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Send capacity limit alert
    /// </summary>
    Task NotifyCapacityAlertAsync(string locationName, int currentOccupancy, int maxCapacity, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send emergency alert to all connected clients
    /// </summary>
    Task NotifyEmergencyAlertAsync(string emergencyType, string message, int initiatedBy, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send visitor check-in notification to host
    /// </summary>
    Task NotifyVisitorCheckInAsync(int hostId, int visitorId, string visitorName, 
        string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send visitor check-out notification to host
    /// </summary>
    Task NotifyVisitorCheckOutAsync(int hostId, int visitorId, string visitorName, 
        DateTime checkOutTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send bulk notification with custom targeting
    /// </summary>
    Task SendBulkNotificationAsync(NotificationAlert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update operator queue status for all operators
    /// </summary>
    Task UpdateOperatorQueueAsync(int waitingCount, int processingCount, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update system health status for administrators
    /// </summary>
    Task UpdateSystemHealthAsync(object healthData, CancellationToken cancellationToken = default);
}
