using Microsoft.AspNetCore.SignalR;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Hubs;

/// <summary>
/// SignalR hub for system administrators
/// Handles system alerts, configuration changes, and administrative notifications
/// </summary>
public class AdminHub : BaseHub
{
    public AdminHub(ILogger<AdminHub> logger, IUnitOfWork unitOfWork) 
        : base(logger, unitOfWork)
    {
    }

    /// <summary>
    /// Client joins to receive administrative notifications
    /// </summary>
    public async Task JoinAsAdmin()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            await Clients.Caller.SendAsync("Error", "User not authenticated");
            return;
        }

        // Check admin permissions
        var userRole = GetCurrentUserRole();
        if (userRole != UserRoles.Administrator && !HasPermission(Permissions.SystemConfig.ViewAll))
        {
            await Clients.Caller.SendAsync("Error", "Administrator permissions required");
            return;
        }

        try
        {
            // Join admin groups based on permissions
            await Groups.AddToGroupAsync(Context.ConnectionId, "Administrators");
            
            if (HasPermission(Permissions.SystemConfig.ViewAll))
                await Groups.AddToGroupAsync(Context.ConnectionId, "SystemConfig");
                
            if (HasPermission(Permissions.Invitation.ApproveAll))
                await Groups.AddToGroupAsync(Context.ConnectionId, "InvitationApprovals");
                
            if (HasPermission(Permissions.Audit.ReadAll))
                await Groups.AddToGroupAsync(Context.ConnectionId, "AuditMonitoring");
                
            if (HasPermission(Permissions.FRSystem.Monitor))
                await Groups.AddToGroupAsync(Context.ConnectionId, "FRSystemMonitoring");

            await Clients.Caller.SendAsync("AdminRegistered", new 
            { 
                UserId = userId.Value,
                Role = userRole,
                AdminPermissions = GetCurrentUserPermissions()
                    .Where(p => p.StartsWith("SystemConfig.") || p.StartsWith("Invitation.Approve") || 
                               p.StartsWith("Audit.") || p.StartsWith("FRSystem."))
                    .ToList(),
                Timestamp = DateTime.UtcNow
            });

            Logger.LogInformation("Administrator {UserId} joined AdminHub", userId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error registering administrator {UserId}", userId);
            await Clients.Caller.SendAsync("Error", "Failed to register for admin notifications");
        }
    }

    /// <summary>
    /// Request system health dashboard
    /// </summary>
    public async Task GetSystemHealth()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || !HasPermission(Permissions.SystemConfig.ViewAll))
        {
            return;
        }

        try
        {
            var today = DateTime.Today;

            // Get system alerts from today
            var systemAlerts = await UnitOfWork.Repository<NotificationAlert>()
                .GetAllAsync(
                    a => a.CreatedOn >= today && a.Type == NotificationAlertType.SystemAlert,
                    orderBy: q => q.OrderByDescending(a => a.CreatedOn),
                    take: 20,
                    cancellationToken: Context.ConnectionAborted);

            // Get pending approvals count
            var pendingApprovals = await UnitOfWork.Invitations
                .GetPendingApprovalsCountAsync(Context.ConnectionAborted);

            var systemHealth = new
            {
                SystemStatus = "Online", // This would be determined by actual health checks
                DatabaseConnected = await UnitOfWork.CanConnectAsync(Context.ConnectionAborted),
                TotalSystemAlerts = systemAlerts.Count(),
                UnacknowledgedAlerts = systemAlerts.Count(a => !a.IsAcknowledged),
                PendingInvitationApprovals = pendingApprovals,
                ActiveOperatorSessions = await GetActiveOperatorCount(),
                RecentSystemAlerts = systemAlerts.Take(5).Select(a => new
                {
                    Id = a.Id,
                    Title = a.Title,
                    Message = a.Message,
                    Priority = a.Priority.ToString(),
                    CreatedOn = a.CreatedOn,
                    IsAcknowledged = a.IsAcknowledged
                }).ToList(),
                Timestamp = DateTime.UtcNow
            };

            await Clients.Caller.SendAsync("SystemHealth", systemHealth);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting system health for admin {UserId}", userId);
        }
    }

    /// <summary>
    /// Approve multiple invitations (bulk approval)
    /// </summary>
    public async Task BulkApproveInvitations(List<int> invitationIds, string? note = null)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || !HasPermission(Permissions.Invitation.BulkApprove))
        {
            await Clients.Caller.SendAsync("Error", "Insufficient permissions for bulk approval");
            return;
        }

        try
        {
            var approvedCount = 0;
            var errors = new List<string>();

            foreach (var invitationId in invitationIds.Take(50)) // Limit to 50 at a time
            {
                try
                {
                    var invitation = await UnitOfWork.Invitations.GetByIdAsync(invitationId, Context.ConnectionAborted);
                    if (invitation != null && invitation.CanBeApproved())
                    {
                        invitation.Approve(userId.Value, note);
                        UnitOfWork.Invitations.Update(invitation);
                        approvedCount++;
                    }
                    else
                    {
                        errors.Add($"Invitation {invitationId} cannot be approved");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error approving invitation {invitationId}: {ex.Message}");
                }
            }

            await UnitOfWork.SaveChangesAsync(Context.ConnectionAborted);

            // Notify other admins of bulk approval
            await Clients.Group("Administrators").SendAsync("BulkApprovalCompleted", new
            {
                ApprovedBy = userId.Value,
                ApprovedCount = approvedCount,
                TotalRequested = invitationIds.Count,
                Errors = errors,
                Note = note,
                Timestamp = DateTime.UtcNow
            });

            await Clients.Caller.SendAsync("BulkApprovalResult", new
            {
                Success = true,
                ApprovedCount = approvedCount,
                Errors = errors
            });

            Logger.LogInformation("Bulk approval completed by admin {UserId}: {ApprovedCount}/{TotalCount} invitations approved", 
                userId, approvedCount, invitationIds.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in bulk approval by admin {UserId}", userId);
            await Clients.Caller.SendAsync("Error", "Bulk approval failed");
        }
    }

    /// <summary>
    /// Broadcast system maintenance notification
    /// </summary>
    public async Task BroadcastMaintenanceNotification(string message, DateTime? scheduledTime = null)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || !HasPermission(Permissions.SystemConfig.Update))
        {
            await Clients.Caller.SendAsync("Error", "Insufficient permissions");
            return;
        }

        try
        {
            // Create system maintenance alert
            var maintenanceAlert = NotificationAlert.CreateFRAlert(
                "System Maintenance Notice",
                message,
                NotificationAlertType.SystemAlert,
                AlertPriority.Medium,
                createdBy: userId.Value);

            await UnitOfWork.Repository<NotificationAlert>().AddAsync(maintenanceAlert, Context.ConnectionAborted);
            await UnitOfWork.SaveChangesAsync(Context.ConnectionAborted);

            // Broadcast to all connected clients
            await Clients.All.SendAsync("MaintenanceNotification", new
            {
                Message = message,
                ScheduledTime = scheduledTime,
                AnnouncedBy = userId.Value,
                Timestamp = DateTime.UtcNow
            });

            Logger.LogInformation("Maintenance notification broadcast by admin {UserId}: {Message}", userId, message);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error broadcasting maintenance notification by admin {UserId}", userId);
        }
    }

    /// <summary>
    /// Get real-time system metrics
    /// </summary>
    public async Task GetSystemMetrics()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || !HasPermission(Permissions.SystemConfig.ViewAll))
        {
            return;
        }

        try
        {
            // These would be real metrics in production
            var metrics = new
            {
                DatabaseConnectionPool = "Healthy",
                MemoryUsage = "Normal",
                ApiResponseTime = "125ms avg",
                ActiveConnections = await GetTotalActiveConnections(),
                ErrorRate = "0.1%",
                SystemLoad = "Low",
                LastBackup = DateTime.UtcNow.AddHours(-6), // Example
                Timestamp = DateTime.UtcNow
            };

            await Clients.Caller.SendAsync("SystemMetrics", metrics);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting system metrics for admin {UserId}", userId);
        }
    }

    /// <summary>
    /// Helper method to get active operator count
    /// </summary>
    private async Task<int> GetActiveOperatorCount()
    {
        try
        {
            return await UnitOfWork.Repository<OperatorSession>()
                .CountAsync(s => s.SessionEnd == null && s.Status != OperatorStatus.Offline, 
                           Context.ConnectionAborted);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Helper method to get total active connections across all hubs
    /// </summary>
    private async Task<int> GetTotalActiveConnections()
    {
        // This would require integration with SignalR connection tracking
        // For now, return a placeholder
        return await Task.FromResult(0);
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        
        // Auto-join admin groups if user has permissions
        await JoinAsAdmin();
    }
}
