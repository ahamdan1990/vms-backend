using Microsoft.AspNetCore.SignalR;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Hubs;

/// <summary>
/// SignalR hub for security staff notifications
/// Handles security alerts, blacklist detections, and emergency management
/// </summary>
public class SecurityHub : BaseHub
{
    public SecurityHub(ILogger<SecurityHub> logger, IUnitOfWork unitOfWork) 
        : base(logger, unitOfWork)
    {
    }

    /// <summary>
    /// Client joins to receive security notifications
    /// </summary>
    public async Task JoinAsSecurity()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        // Check security permissions
        if (!HasPermission(Permissions.Notification.Receive) && 
            !HasPermission(Permissions.Notification.Receive) &&
            !HasPermission(Permissions.Notification.Receive))
        {
            await Clients.Caller.SendAsync("Error", "Insufficient security permissions");
            return;
        }

        try
        {
            // Join security groups based on permissions
            await Groups.AddToGroupAsync(Context.ConnectionId, "Security");
            
            if (HasPermission(Permissions.Notification.Receive))
                await Groups.AddToGroupAsync(Context.ConnectionId, "SecurityFR");
                
            if (HasPermission(Permissions.Notification.Receive))
                await Groups.AddToGroupAsync(Context.ConnectionId, "SecurityBlacklist");
                
            if (HasPermission(Permissions.Notification.Receive))
                await Groups.AddToGroupAsync(Context.ConnectionId, "SecurityVIP");
                
            if (HasPermission(Permissions.Emergency.ViewRoster))
                await Groups.AddToGroupAsync(Context.ConnectionId, "SecurityEmergency");

            await Clients.Caller.SendAsync("SecurityRegistered", new 
            { 
                UserId = userId.Value,
                Permissions = GetCurrentUserPermissions().Where(p => p.StartsWith("Alert.") || p.StartsWith("Emergency.")).ToList(),
                Timestamp = DateTime.UtcNow
            });

            Logger.LogInformation("Security user {UserId} joined SecurityHub", userId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error registering security user {UserId}", userId);
            await Clients.Caller.SendAsync("Error", "Failed to register for security notifications");
        }
    }

    /// <summary>
    /// Acknowledge a security alert
    /// </summary>
    public async Task AcknowledgeSecurityAlert(int alertId, string? response = null)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return;

        try
        {
            var alert = await UnitOfWork.Repository<NotificationAlert>()
                .GetByIdAsync(alertId, Context.ConnectionAborted);

            if (alert != null && !alert.IsAcknowledged)
            {
                alert.Acknowledge(userId.Value);
                UnitOfWork.Repository<NotificationAlert>().Update(alert);
                await UnitOfWork.SaveChangesAsync(Context.ConnectionAborted);

                // Notify other security staff of acknowledgment
                await Clients.Group("Security").SendAsync("SecurityAlertAcknowledged", new
                {
                    AlertId = alertId,
                    AcknowledgedBy = userId.Value,
                    Response = response,
                    Timestamp = DateTime.UtcNow
                });

                Logger.LogInformation("Security alert {AlertId} acknowledged by {UserId}: {Response}", 
                    alertId, userId, response);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error acknowledging security alert {AlertId} by {UserId}", alertId, userId);
        }
    }

    /// <summary>
    /// Request current security status dashboard
    /// </summary>
    public async Task GetSecurityStatus()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || !HasPermission(Permissions.Notification.Receive))
        {
            return;
        }

        try
        {
            var today = DateTime.Today;

            // Get today's security alerts
            var todayAlerts = await UnitOfWork.Repository<NotificationAlert>()
                .GetAllAsync(
                    a => a.CreatedOn >= today && 
                         (a.Type == NotificationAlertType.BlacklistAlert ||
                          a.Type == NotificationAlertType.UnknownFace ||
                          a.Type == NotificationAlertType.EmergencyAlert ||
                          a.Type == NotificationAlertType.FRSystemOffline),
                    orderBy: q => q.OrderByDescending(a => a.CreatedOn),
                    take: 50,
                    cancellationToken: Context.ConnectionAborted);

            var securityStatus = new
            {
                TotalAlertsToday = todayAlerts.Count(),
                UnacknowledgedAlerts = todayAlerts.Count(a => !a.IsAcknowledged),
                BlacklistAlerts = todayAlerts.Count(a => a.Type == NotificationAlertType.BlacklistAlert),
                UnknownFaceAlerts = todayAlerts.Count(a => a.Type == NotificationAlertType.UnknownFace),
                SystemAlerts = todayAlerts.Count(a => a.Type == NotificationAlertType.FRSystemOffline),
                EmergencyAlerts = todayAlerts.Count(a => a.Type == NotificationAlertType.EmergencyAlert),
                RecentAlerts = todayAlerts.Take(10).Select(a => new
                {
                    Id = a.Id,
                    Type = a.Type.ToString(),
                    Priority = a.Priority.ToString(),
                    Title = a.Title,
                    CreatedOn = a.CreatedOn,
                    IsAcknowledged = a.IsAcknowledged
                }).ToList(),
                Timestamp = DateTime.UtcNow
            };

            await Clients.Caller.SendAsync("SecurityStatus", securityStatus);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting security status for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Initiate emergency procedure (lockdown, evacuation, etc.)
    /// </summary>
    public async Task InitiateEmergencyProcedure(string procedureType, string reason)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || !HasPermission(Permissions.Emergency.Lockdown))
        {
            await Clients.Caller.SendAsync("Error", "Insufficient permissions for emergency procedures");
            return;
        }

        try
        {
            // Create emergency alert
            var emergencyAlert = NotificationAlert.CreateFRAlert(
                $"EMERGENCY: {procedureType}",
                $"Emergency procedure '{procedureType}' initiated by security. Reason: {reason}",
                NotificationAlertType.EmergencyAlert,
                AlertPriority.Emergency,
                createdBy: userId.Value);

            await UnitOfWork.Repository<NotificationAlert>().AddAsync(emergencyAlert, Context.ConnectionAborted);
            await UnitOfWork.SaveChangesAsync(Context.ConnectionAborted);

            // Broadcast to all hubs
            await Clients.All.SendAsync("EmergencyAlert", new
            {
                Type = procedureType,
                Reason = reason,
                InitiatedBy = userId.Value,
                Timestamp = DateTime.UtcNow,
                AlertId = emergencyAlert.Id
            });

            Logger.LogCritical("Emergency procedure {ProcedureType} initiated by {UserId}: {Reason}", 
                procedureType, userId, reason);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initiating emergency procedure by {UserId}", userId);
            await Clients.Caller.SendAsync("Error", "Failed to initiate emergency procedure");
        }
    }

    /// <summary>
    /// Request current visitor occupancy for emergency roster
    /// </summary>
    public async Task GetEmergencyRoster()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || !HasPermission(Permissions.Emergency.ViewRoster))
        {
            return;
        }

        try
        {
            // This would need integration with check-in system
            // For now, send placeholder data
            await Clients.Caller.SendAsync("EmergencyRoster", new
            {
                TotalOccupants = 0,
                Visitors = new object[0],
                Staff = new object[0],
                LastUpdated = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting emergency roster for {UserId}", userId);
        }
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        
        // Auto-join security groups if user has permissions
        await JoinAsSecurity();
    }
}
