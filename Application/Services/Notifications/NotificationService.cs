using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Hubs;

namespace VisitorManagementSystem.Api.Application.Services.Notifications;

/// <summary>
/// Service for sending real-time notifications via SignalR hubs
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IHubContext<OperatorHub> _operatorHubContext;
    private readonly IHubContext<HostHub> _hostHubContext;
    private readonly IHubContext<SecurityHub> _securityHubContext;
    private readonly IHubContext<AdminHub> _adminHubContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHubContext<OperatorHub> operatorHubContext,
        IHubContext<HostHub> hostHubContext,
        IHubContext<SecurityHub> securityHubContext,
        IHubContext<AdminHub> adminHubContext,
        IUnitOfWork unitOfWork,
        ILogger<NotificationService> logger)
    {
        _operatorHubContext = operatorHubContext ?? throw new ArgumentNullException(nameof(operatorHubContext));
        _hostHubContext = hostHubContext ?? throw new ArgumentNullException(nameof(hostHubContext));
        _securityHubContext = securityHubContext ?? throw new ArgumentNullException(nameof(securityHubContext));
        _adminHubContext = adminHubContext ?? throw new ArgumentNullException(nameof(adminHubContext));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task NotifyUserAsync(int userId, string title, string message, NotificationAlertType type, 
        AlertPriority priority = AlertPriority.Medium, object? data = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create and persist alert
            var alert = NotificationAlert.CreateFRAlert(title, message, type, priority, 
                targetUserId: userId, payloadData: JsonSerializer.Serialize(data));
            
            await _unitOfWork.Repository<NotificationAlert>().AddAsync(alert, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Send to user via HostHub (assuming user is a host/staff member)
            var notificationPayload = CreateNotificationPayload(alert, data);
            await _hostHubContext.Clients.Group($"Host_{userId}")
                .SendAsync("UserNotification", notificationPayload, cancellationToken);

            _logger.LogInformation("Notification sent to user {UserId}: {Title}", userId, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
            throw;
        }
    }

    public async Task NotifyRoleAsync(string role, string title, string message, NotificationAlertType type, 
        AlertPriority priority = AlertPriority.Medium, object? data = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create and persist alert
            var alert = NotificationAlert.CreateFRAlert(title, message, type, priority, 
                targetRole: role, payloadData: JsonSerializer.Serialize(data));
            
            await _unitOfWork.Repository<NotificationAlert>().AddAsync(alert, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var notificationPayload = CreateNotificationPayload(alert, data);

            // Route to appropriate hub based on role
            switch (role)
            {
                case UserRoles.Administrator:
                    await _adminHubContext.Clients.Group("Administrators")
                        .SendAsync("RoleNotification", notificationPayload, cancellationToken);
                    break;

                case UserRoles.Staff:
                    await _hostHubContext.Clients.Group("Hosts")
                        .SendAsync("RoleNotification", notificationPayload, cancellationToken);
                    break;

                case UserRoles.Receptionist:
                    await _operatorHubContext.Clients.Group("Operators")
                        .SendAsync("RoleNotification", notificationPayload, cancellationToken);
                    break;

                default:
                    // Send to all hubs for unknown roles
                    await _hostHubContext.Clients.Group("Hosts")
                        .SendAsync("RoleNotification", notificationPayload, cancellationToken);
                    break;
            }

            _logger.LogInformation("Notification sent to role {Role}: {Title}", role, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to role {Role}", role);
            throw;
        }
    }

    public async Task NotifyOperatorsAsync(int? locationId, string title, string message, NotificationAlertType type, 
        AlertPriority priority = AlertPriority.Medium, object? data = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = NotificationAlert.CreateFRAlert(title, message, type, priority, 
                targetRole: UserRoles.Receptionist, targetLocationId: locationId, 
                payloadData: JsonSerializer.Serialize(data));
            
            await _unitOfWork.Repository<NotificationAlert>().AddAsync(alert, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var notificationPayload = CreateNotificationPayload(alert, data);

            if (locationId.HasValue)
            {
                // Send to operators at specific location
                await _operatorHubContext.Clients.Group($"Location_{locationId}")
                    .SendAsync("OperatorAlert", notificationPayload, cancellationToken);
            }
            else
            {
                // Send to all operators
                await _operatorHubContext.Clients.Group("Operators")
                    .SendAsync("OperatorAlert", notificationPayload, cancellationToken);
            }

            _logger.LogInformation("Operator notification sent (Location: {LocationId}): {Title}", locationId, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending operator notification (Location: {LocationId})", locationId);
            throw;
        }
    }

    public async Task NotifyHostOfVisitorArrivalAsync(int hostId, int visitorId, string visitorName, 
        DateTime arrivalTime, CancellationToken cancellationToken = default)
    {
        try
        {
            var title = "Visitor Arrival";
            var message = $"Your visitor {visitorName} has arrived at {arrivalTime:HH:mm}";

            var alert = NotificationAlert.CreateFRAlert(title, message, NotificationAlertType.VisitorArrival, 
                AlertPriority.Medium, targetUserId: hostId, relatedEntityType: "Visitor", 
                relatedEntityId: visitorId);

            await _unitOfWork.Repository<NotificationAlert>().AddAsync(alert, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var payload = new
            {
                AlertId = alert.Id,
                Type = "VisitorArrival",
                HostId = hostId,
                VisitorId = visitorId,
                VisitorName = visitorName,
                ArrivalTime = arrivalTime,
                Message = message,
                Priority = AlertPriority.Medium.ToString(),
                Timestamp = DateTime.UtcNow
            };

            await _hostHubContext.Clients.Group($"Host_{hostId}")
                .SendAsync("VisitorArrival", payload, cancellationToken);

            _logger.LogInformation("Host {HostId} notified of visitor arrival: {VisitorName}", hostId, visitorName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying host {HostId} of visitor arrival", hostId);
            throw;
        }
    }

    public async Task NotifyVipArrivalAsync(string visitorName, string location, CancellationToken cancellationToken = default)
    {
        try
        {
            var title = "VIP Visitor Arrival";
            var message = $"VIP visitor {visitorName} has arrived at {location}";

            var alert = NotificationAlert.CreateFRAlert(title, message, NotificationAlertType.VipArrival, 
                AlertPriority.High, targetRole: UserRoles.Receptionist);

            await _unitOfWork.Repository<NotificationAlert>().AddAsync(alert, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var payload = new
            {
                AlertId = alert.Id,
                Type = "VipArrival",
                VisitorName = visitorName,
                Location = location,
                Message = message,
                Priority = AlertPriority.High.ToString(),
                Timestamp = DateTime.UtcNow
            };

            // Notify operators and security
            await _operatorHubContext.Clients.Group("Operators")
                .SendAsync("VipAlert", payload, cancellationToken);
            await _securityHubContext.Clients.Group("SecurityVIP")
                .SendAsync("VipAlert", payload, cancellationToken);

            _logger.LogInformation("VIP arrival notification sent: {VisitorName} at {Location}", visitorName, location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending VIP arrival notification");
            throw;
        }
    }

    public async Task NotifyBlacklistDetectionAsync(string personDescription, string cameraLocation, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var title = "🚨 SECURITY ALERT: Blacklisted Person Detected";
            var message = $"Blacklisted individual detected at {cameraLocation}. Description: {personDescription}";

            var alert = NotificationAlert.CreateFRAlert(title, message, NotificationAlertType.BlacklistAlert, 
                AlertPriority.Critical);

            await _unitOfWork.Repository<NotificationAlert>().AddAsync(alert, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var payload = new
            {
                AlertId = alert.Id,
                Type = "BlacklistAlert",
                PersonDescription = personDescription,
                CameraLocation = cameraLocation,
                Message = message,
                Priority = AlertPriority.Critical.ToString(),
                Timestamp = DateTime.UtcNow
            };

            // Send to security and administrators immediately
            await _securityHubContext.Clients.Group("SecurityBlacklist")
                .SendAsync("SecurityAlert", payload, cancellationToken);
            await _adminHubContext.Clients.Group("Administrators")
                .SendAsync("CriticalAlert", payload, cancellationToken);
            await _operatorHubContext.Clients.Group("Operators")
                .SendAsync("SecurityAlert", payload, cancellationToken);

            _logger.LogCritical("Blacklist detection alert sent: {PersonDescription} at {CameraLocation}", 
                personDescription, cameraLocation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending blacklist detection alert");
            throw;
        }
    }
    public async Task NotifyUnknownFaceDetectionAsync(string cameraLocation, CancellationToken cancellationToken = default)
    {
        try
        {
            var title = "Unknown Face Detected";
            var message = $"Unknown person detected at {cameraLocation}. Manual verification required.";

            var alert = NotificationAlert.CreateFRAlert(title, message, NotificationAlertType.UnknownFace, 
                AlertPriority.Medium);

            await _unitOfWork.Repository<NotificationAlert>().AddAsync(alert, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var payload = new
            {
                AlertId = alert.Id,
                Type = "UnknownFace",
                CameraLocation = cameraLocation,
                Message = message,
                Priority = AlertPriority.Medium.ToString(),
                Timestamp = DateTime.UtcNow
            };

            // Notify operators for manual verification
            await _operatorHubContext.Clients.Group("Operators")
                .SendAsync("UnknownFaceAlert", payload, cancellationToken);

            _logger.LogInformation("Unknown face detection alert sent: {CameraLocation}", cameraLocation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending unknown face detection alert");
            throw;
        }
    }

    public async Task NotifyInvitationApprovalAsync(int invitationId, int hostId, bool approved, 
        string? note = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var title = approved ? "Invitation Approved" : "Invitation Rejected";
            var message = approved 
                ? $"Your invitation has been approved{(string.IsNullOrEmpty(note) ? "" : $". Note: {note}")}"
                : $"Your invitation has been rejected{(string.IsNullOrEmpty(note) ? "" : $". Reason: {note}")}";

            var alertType = approved ? NotificationAlertType.InvitationApproved : NotificationAlertType.InvitationRejected;
            
            var alert = NotificationAlert.CreateFRAlert(title, message, alertType, AlertPriority.Medium, 
                targetUserId: hostId, relatedEntityType: "Invitation", relatedEntityId: invitationId);

            await _unitOfWork.Repository<NotificationAlert>().AddAsync(alert, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var payload = new
            {
                AlertId = alert.Id,
                Type = approved ? "InvitationApproved" : "InvitationRejected",
                InvitationId = invitationId,
                HostId = hostId,
                Approved = approved,
                Note = note,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            await _hostHubContext.Clients.Group($"Host_{hostId}")
                .SendAsync("InvitationStatusUpdate", payload, cancellationToken);

            _logger.LogInformation("Invitation {InvitationId} {Status} notification sent to host {HostId}", 
                invitationId, approved ? "approval" : "rejection", hostId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invitation approval notification");
            throw;
        }
    }

    public async Task NotifySystemAlertAsync(string title, string message, AlertPriority priority, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = NotificationAlert.CreateFRAlert(title, message, NotificationAlertType.SystemAlert, 
                priority, targetRole: UserRoles.Administrator);

            await _unitOfWork.Repository<NotificationAlert>().AddAsync(alert, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var payload = CreateNotificationPayload(alert);

            await _adminHubContext.Clients.Group("Administrators")
                .SendAsync("SystemAlert", payload, cancellationToken);

            _logger.LogWarning("System alert sent to administrators: {Title}", title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending system alert");
            throw;
        }
    }

    public async Task NotifyFRSystemOfflineAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var title = "Facial Recognition System Offline";
            var message = "The facial recognition system is currently offline. Manual processing required.";

            var alert = NotificationAlert.CreateFRAlert(title, message, NotificationAlertType.FRSystemOffline, 
                AlertPriority.High);

            await _unitOfWork.Repository<NotificationAlert>().AddAsync(alert, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var payload = CreateNotificationPayload(alert);

            // Notify all relevant parties
            await _operatorHubContext.Clients.Group("Operators")
                .SendAsync("FRSystemOffline", payload, cancellationToken);
            await _adminHubContext.Clients.Group("FRSystemMonitoring")
                .SendAsync("FRSystemOffline", payload, cancellationToken);
            await _securityHubContext.Clients.Group("Security")
                .SendAsync("FRSystemOffline", payload, cancellationToken);

            _logger.LogWarning("FR System offline notification sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending FR system offline notification");
            throw;
        }
    }

    public async Task NotifyCapacityAlertAsync(string locationName, int currentOccupancy, int maxCapacity, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var title = "Capacity Alert";
            var message = $"Location '{locationName}' is at {currentOccupancy}/{maxCapacity} capacity ({(currentOccupancy * 100 / maxCapacity)}%)";

            var alert = NotificationAlert.CreateFRAlert(title, message, NotificationAlertType.CapacityAlert, 
                AlertPriority.Medium, targetRole: UserRoles.Receptionist);

            await _unitOfWork.Repository<NotificationAlert>().AddAsync(alert, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var payload = new
            {
                AlertId = alert.Id,
                Type = "CapacityAlert",
                LocationName = locationName,
                CurrentOccupancy = currentOccupancy,
                MaxCapacity = maxCapacity,
                PercentageFull = currentOccupancy * 100 / maxCapacity,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            await _operatorHubContext.Clients.Group("Operators")
                .SendAsync("CapacityAlert", payload, cancellationToken);
            await _adminHubContext.Clients.Group("Administrators")
                .SendAsync("CapacityAlert", payload, cancellationToken);

            _logger.LogInformation("Capacity alert sent for {LocationName}: {CurrentOccupancy}/{MaxCapacity}", 
                locationName, currentOccupancy, maxCapacity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending capacity alert");
            throw;
        }
    }

    public async Task NotifyEmergencyAlertAsync(string emergencyType, string message, int initiatedBy, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var title = $"🚨 EMERGENCY: {emergencyType}";

            var alert = NotificationAlert.CreateFRAlert(title, message, NotificationAlertType.EmergencyAlert, 
                AlertPriority.Emergency, createdBy: initiatedBy);

            await _unitOfWork.Repository<NotificationAlert>().AddAsync(alert, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var payload = new
            {
                AlertId = alert.Id,
                Type = "EmergencyAlert",
                EmergencyType = emergencyType,
                Message = message,
                InitiatedBy = initiatedBy,
                Priority = AlertPriority.Emergency.ToString(),
                Timestamp = DateTime.UtcNow
            };

            // Send to ALL connected clients
            await _operatorHubContext.Clients.All.SendAsync("EmergencyAlert", payload, cancellationToken);
            await _hostHubContext.Clients.All.SendAsync("EmergencyAlert", payload, cancellationToken);
            await _securityHubContext.Clients.All.SendAsync("EmergencyAlert", payload, cancellationToken);
            await _adminHubContext.Clients.All.SendAsync("EmergencyAlert", payload, cancellationToken);

            _logger.LogCritical("Emergency alert broadcast: {EmergencyType} initiated by {InitiatedBy}", 
                emergencyType, initiatedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending emergency alert");
            throw;
        }
    }

    public async Task NotifyVisitorCheckInAsync(int hostId, int visitorId, string visitorName, 
        string location, CancellationToken cancellationToken = default)
    {
        try
        {
            var title = "Visitor Checked In";
            var message = $"{visitorName} has checked in at {location}";

            var alert = NotificationAlert.CreateFRAlert(title, message, NotificationAlertType.VisitorCheckedIn, 
                AlertPriority.Low, targetUserId: hostId, relatedEntityType: "Visitor", relatedEntityId: visitorId);

            await _unitOfWork.Repository<NotificationAlert>().AddAsync(alert, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var payload = new
            {
                AlertId = alert.Id,
                Type = "VisitorCheckIn",
                HostId = hostId,
                VisitorId = visitorId,
                VisitorName = visitorName,
                Location = location,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            await _hostHubContext.Clients.Group($"Host_{hostId}")
                .SendAsync("VisitorCheckIn", payload, cancellationToken);

            _logger.LogInformation("Host {HostId} notified of visitor check-in: {VisitorName}", hostId, visitorName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending visitor check-in notification");
            throw;
        }
    }

    public async Task NotifyVisitorCheckOutAsync(int hostId, int visitorId, string visitorName, 
        DateTime checkOutTime, CancellationToken cancellationToken = default)
    {
        try
        {
            var title = "Visitor Checked Out";
            var message = $"{visitorName} has checked out at {checkOutTime:HH:mm}";

            var alert = NotificationAlert.CreateFRAlert(title, message, NotificationAlertType.VisitorCheckedOut, 
                AlertPriority.Low, targetUserId: hostId, relatedEntityType: "Visitor", relatedEntityId: visitorId);

            await _unitOfWork.Repository<NotificationAlert>().AddAsync(alert, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var payload = new
            {
                AlertId = alert.Id,
                Type = "VisitorCheckOut",
                HostId = hostId,
                VisitorId = visitorId,
                VisitorName = visitorName,
                CheckOutTime = checkOutTime,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            await _hostHubContext.Clients.Group($"Host_{hostId}")
                .SendAsync("VisitorCheckOut", payload, cancellationToken);

            _logger.LogInformation("Host {HostId} notified of visitor check-out: {VisitorName}", hostId, visitorName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending visitor check-out notification");
            throw;
        }
    }

    public async Task SendBulkNotificationAsync(NotificationAlert alert, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = CreateNotificationPayload(alert);

            // Route based on target role and location
            if (!string.IsNullOrEmpty(alert.TargetRole))
            {
                await NotifyRoleAsync(alert.TargetRole, alert.Title, alert.Message, alert.Type, 
                    alert.Priority, payload, cancellationToken);
            }
            else if (alert.TargetUserId.HasValue)
            {
                await _hostHubContext.Clients.Group($"Host_{alert.TargetUserId}")
                    .SendAsync("BulkNotification", payload, cancellationToken);
            }
            else if (alert.TargetLocationId.HasValue)
            {
                await _operatorHubContext.Clients.Group($"Location_{alert.TargetLocationId}")
                    .SendAsync("BulkNotification", payload, cancellationToken);
            }
            else
            {
                // Send to all hubs
                await _operatorHubContext.Clients.All.SendAsync("BulkNotification", payload, cancellationToken);
                await _hostHubContext.Clients.All.SendAsync("BulkNotification", payload, cancellationToken);
                await _securityHubContext.Clients.All.SendAsync("BulkNotification", payload, cancellationToken);
                await _adminHubContext.Clients.All.SendAsync("BulkNotification", payload, cancellationToken);
            }

            _logger.LogInformation("Bulk notification sent: {Title}", alert.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk notification");
            throw;
        }
    }

    public async Task UpdateOperatorQueueAsync(int waitingCount, int processingCount, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                WaitingCount = waitingCount,
                ProcessingCount = processingCount,
                TotalCount = waitingCount + processingCount,
                Timestamp = DateTime.UtcNow
            };

            await _operatorHubContext.Clients.Group("Operators")
                .SendAsync("QueueUpdate", payload, cancellationToken);

            _logger.LogDebug("Operator queue updated: {WaitingCount} waiting, {ProcessingCount} processing", 
                waitingCount, processingCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating operator queue");
            throw;
        }
    }

    public async Task UpdateSystemHealthAsync(object healthData, CancellationToken cancellationToken = default)
    {
        try
        {
            await _adminHubContext.Clients.Group("Administrators")
                .SendAsync("SystemHealthUpdate", healthData, cancellationToken);

            _logger.LogDebug("System health update sent to administrators");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system health");
            throw;
        }
    }

    /// <summary>
    /// Creates a standardized notification payload
    /// </summary>
    private static object CreateNotificationPayload(NotificationAlert alert, object? additionalData = null)
    {
        return new
        {
            AlertId = alert.Id,
            Title = alert.Title,
            Message = alert.Message,
            Type = alert.Type.ToString(),
            Priority = alert.Priority.ToString(),
            TargetRole = alert.TargetRole,
            TargetUserId = alert.TargetUserId,
            TargetLocationId = alert.TargetLocationId,
            RelatedEntityType = alert.RelatedEntityType,
            RelatedEntityId = alert.RelatedEntityId,
            PayloadData = alert.PayloadData,
            AdditionalData = additionalData,
            CreatedOn = alert.CreatedOn,
            ExpiresOn = alert.ExpiresOn,
            Timestamp = DateTime.UtcNow
        };
    }
}
