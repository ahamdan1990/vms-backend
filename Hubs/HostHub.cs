using Microsoft.AspNetCore.SignalR;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Hubs;

/// <summary>
/// SignalR hub for host/employee notifications about their visitors
/// Handles visitor arrivals, invitation status updates, and host-specific alerts
/// </summary>
public class HostHub : BaseHub
{
    public HostHub(ILogger<HostHub> logger, IUnitOfWork unitOfWork) 
        : base(logger, unitOfWork)
    {
    }

    /// <summary>
    /// Client joins to receive notifications as a host
    /// </summary>
    public async Task JoinAsHost()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        // Check host permissions (staff can receive visitor notifications)
        if (!HasPermission(Permissions.Invitation.ReadAll))
        {
            await Clients.Caller.SendAsync("Error", "Insufficient permissions");
            return;
        }

        try
        {
            // Join user-specific group for targeted notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Host_{userId}");
            
            // Join general hosts group
            await Groups.AddToGroupAsync(Context.ConnectionId, "Hosts");

            await Clients.Caller.SendAsync("HostRegistered", new 
            { 
                UserId = userId.Value,
                Status = "Online",
                Timestamp = DateTime.UtcNow
            });

            Logger.LogInformation("Host {UserId} joined HostHub", userId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error registering host {UserId}", userId);
            await Clients.Caller.SendAsync("Error", "Failed to register as host");
        }
    }

    /// <summary>
    /// Acknowledge a visitor arrival or notification
    /// </summary>
    public async Task AcknowledgeNotification(int notificationId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return;

        try
        {
            var notification = await UnitOfWork.Repository<NotificationAlert>()
                .GetFirstOrDefaultAsync(
                    n => n.Id == notificationId && n.TargetUserId == userId,
                    cancellationToken: Context.ConnectionAborted);

            if (notification != null && !notification.IsAcknowledged)
            {
                notification.Acknowledge(userId.Value);
                UnitOfWork.Repository<NotificationAlert>().Update(notification);
                await UnitOfWork.SaveChangesAsync(Context.ConnectionAborted);

                await Clients.Caller.SendAsync("NotificationAcknowledged", new
                {
                    NotificationId = notificationId,
                    Timestamp = DateTime.UtcNow
                });

                Logger.LogInformation("Host notification {NotificationId} acknowledged by {UserId}", 
                    notificationId, userId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error acknowledging notification {NotificationId} by host {UserId}", 
                notificationId, userId);
        }
    }

    /// <summary>
    /// Request current day's visitor schedule for the host
    /// </summary>
    public async Task GetTodaysVisitors()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || !HasPermission(Permissions.Invitation.ReadOwn))
        {
            return;
        }

        try
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Get today's invitations for this host
            var todaysInvitations = await UnitOfWork.Invitations
                .GetTodaysInvitationsForHostAsync(userId.Value, today, tomorrow, Context.ConnectionAborted);

            var visitorSchedule = todaysInvitations.Select(inv => new
            {
                InvitationId = inv.Id,
                VisitorName = inv.Visitor?.FullName,
                Company = inv.Visitor?.Company,
                ScheduledTime = inv.ScheduledStartTime,
                Status = inv.Status.ToString(),
                Location = inv.Location?.Name,
                Purpose = inv.VisitPurpose?.Name
            }).ToList();

            await Clients.Caller.SendAsync("TodaysVisitors", new
            {
                Date = today,
                Visitors = visitorSchedule,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting today's visitors for host {UserId}", userId);
        }
    }

    /// <summary>
    /// Request notification history for the host
    /// </summary>
    public async Task GetNotificationHistory(int days = 7)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || !HasPermission(Permissions.Notification.ReadOwn))
        {
            return;
        }

        try
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);

            var notifications = await UnitOfWork.Repository<NotificationAlert>()
                .GetAllAsync(
                    n => n.TargetUserId == userId && n.CreatedOn >= fromDate,
                    orderBy: q => q.OrderByDescending(n => n.CreatedOn),
                    take: 50,
                    cancellationToken: Context.ConnectionAborted);

            var notificationHistory = notifications.Select(n => new
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type.ToString(),
                Priority = n.Priority.ToString(),
                IsAcknowledged = n.IsAcknowledged,
                CreatedOn = n.CreatedOn,
                AcknowledgedOn = n.AcknowledgedOn
            }).ToList();

            await Clients.Caller.SendAsync("NotificationHistory", new
            {
                Days = days,
                Notifications = notificationHistory,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting notification history for host {UserId}", userId);
        }
    }

    /// <summary>
    /// Update host availability status
    /// </summary>
    public async Task UpdateAvailability(bool isAvailable, string? reason = null)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return;

        try
        {
            // This could be stored in user preferences or a separate availability table
            // For now, just broadcast the status change
            await Clients.Group("Operators").SendAsync("HostAvailabilityChanged", new
            {
                HostId = userId.Value,
                IsAvailable = isAvailable,
                Reason = reason,
                Timestamp = DateTime.UtcNow
            });

            Logger.LogInformation("Host {UserId} updated availability: {IsAvailable}, Reason: {Reason}",
                userId, isAvailable, reason);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating availability for host {UserId}", userId);
        }
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        
        // Auto-join as host when connecting
        await JoinAsHost();
    }
}
