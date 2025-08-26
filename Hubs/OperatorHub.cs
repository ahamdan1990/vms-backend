using Microsoft.AspNetCore.SignalR;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Hubs;

/// <summary>
/// SignalR hub for operator/receptionist real-time notifications
/// Handles visitor arrivals, check-ins, alerts, and queue management
/// </summary>
public class OperatorHub : BaseHub
{
    public OperatorHub(ILogger<OperatorHub> logger, IUnitOfWork unitOfWork) 
        : base(logger, unitOfWork)
    {
    }

    /// <summary>
    /// Client joins as an active operator for a specific location
    /// </summary>
    public async Task JoinAsOperator(int? locationId = null)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        // Check operator permissions
        if (!HasPermission(Permissions.CheckIn.Process))
        {
            await Clients.Caller.SendAsync("Error", "Insufficient permissions");
            return;
        }

        try
        {
            // Create or update operator session
            var existingSession = await UnitOfWork.Repository<OperatorSession>()
                .GetFirstOrDefaultAsync(
                    s => s.UserId == userId && s.SessionEnd == null,
                    cancellationToken: Context.ConnectionAborted);

            if (existingSession != null)
            {
                // Update existing session with new connection
                existingSession.ConnectionId = Context.ConnectionId;
                existingSession.LocationId = locationId;
                existingSession.UpdateActivity();
                UnitOfWork.Repository<OperatorSession>().Update(existingSession);
            }
            else
            {
                // Create new session
                var session = new OperatorSession
                {
                    UserId = userId.Value,
                    ConnectionId = Context.ConnectionId,
                    LocationId = locationId,
                    IpAddress = GetClientIpAddress(),
                    UserAgent = GetUserAgent(),
                    Status = OperatorStatus.Online
                };
                session.SetCreatedBy(userId.Value);
                
                await UnitOfWork.Repository<OperatorSession>().AddAsync(session, Context.ConnectionAborted);
            }

            await UnitOfWork.SaveChangesAsync(Context.ConnectionAborted);

            // Join appropriate groups
            await Groups.AddToGroupAsync(Context.ConnectionId, "Operators");
            
            if (locationId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Location_{locationId}");
            }

            await Clients.Caller.SendAsync("OperatorRegistered", new 
            { 
                UserId = userId.Value,
                LocationId = locationId,
                Status = "Online",
                Timestamp = DateTime.UtcNow
            });

            Logger.LogInformation(
                "Operator {UserId} joined OperatorHub for location {LocationId}",
                userId, locationId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error registering operator {UserId}", userId);
            await Clients.Caller.SendAsync("Error", "Failed to register as operator");
        }
    }

    /// <summary>
    /// Update operator status (Online, Busy, Away)
    /// </summary>
    public async Task UpdateStatus(OperatorStatus status)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return;

        try
        {
            var session = await UnitOfWork.Repository<OperatorSession>()
                .GetFirstOrDefaultAsync(
                    s => s.UserId == userId && s.ConnectionId == Context.ConnectionId,
                    cancellationToken: Context.ConnectionAborted);

            if (session != null)
            {
                session.SetStatus(status);
                UnitOfWork.Repository<OperatorSession>().Update(session);
                await UnitOfWork.SaveChangesAsync(Context.ConnectionAborted);

                // Notify other operators of status change
                await Clients.Group("Operators").SendAsync("OperatorStatusChanged", new
                {
                    UserId = userId.Value,
                    Status = status.ToString(),
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating operator status for {UserId}", userId);
        }
    }

    /// <summary>
    /// Acknowledge an alert
    /// </summary>
    public async Task AcknowledgeAlert(int alertId)
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

                // Notify that alert was acknowledged
                await Clients.Group("Operators").SendAsync("AlertAcknowledged", new
                {
                    AlertId = alertId,
                    AcknowledgedBy = userId.Value,
                    Timestamp = DateTime.UtcNow
                });

                Logger.LogInformation("Alert {AlertId} acknowledged by operator {UserId}", alertId, userId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error acknowledging alert {AlertId} by operator {UserId}", alertId, userId);
        }
    }

    /// <summary>
    /// Request current visitor queue status
    /// </summary>
    public async Task GetVisitorQueue()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || !HasPermission(Permissions.CheckIn.ViewQueue))
        {
            return;
        }

        try
        {
            // Get today's pending check-ins
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // This would need to be implemented based on your check-in system
            // For now, sending a placeholder response
            await Clients.Caller.SendAsync("VisitorQueueUpdate", new
            {
                PendingCheckIns = 0,
                WaitingVisitors = 0,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting visitor queue for operator {UserId}", userId);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        if (userId.HasValue)
        {
            try
            {
                // End operator session
                var session = await UnitOfWork.Repository<OperatorSession>()
                    .GetFirstOrDefaultAsync(
                        s => s.UserId == userId && s.ConnectionId == Context.ConnectionId,
                        cancellationToken: Context.ConnectionAborted);

                if (session != null)
                {
                    session.EndSession();
                    UnitOfWork.Repository<OperatorSession>().Update(session);
                    await UnitOfWork.SaveChangesAsync(Context.ConnectionAborted);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error ending operator session for {UserId}", userId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
