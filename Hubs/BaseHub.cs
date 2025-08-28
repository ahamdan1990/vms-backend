using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Hubs;

/// <summary>
/// Base hub providing common functionality for all SignalR hubs
/// </summary>
[Authorize]
public abstract class BaseHub : Hub
{
    protected readonly ILogger<BaseHub> Logger;
    protected readonly IUnitOfWork UnitOfWork;

    protected BaseHub(ILogger<BaseHub> logger, IUnitOfWork unitOfWork)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        UnitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Gets the current user ID from the connection context
    /// </summary>
    protected int? GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                         Context.User?.FindFirst("sub")?.Value ??
                         Context.User?.FindFirst("user_id")?.Value;

        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Gets the current user's email from the connection context
    /// </summary>
    protected string? GetCurrentUserEmail()
    {
        return Context.User?.FindFirst(ClaimTypes.Email)?.Value ??
               Context.User?.FindFirst("email")?.Value;
    }

    /// <summary>
    /// Gets the current user's role from the connection context
    /// </summary>
    protected string? GetCurrentUserRole()
    {
        return Context.User?.FindFirst(ClaimTypes.Role)?.Value ??
               Context.User?.FindFirst("role")?.Value;
    }

    /// <summary>
    /// Gets all user permissions from the connection context
    /// </summary>
    protected List<string> GetCurrentUserPermissions()
    {
        return Context.User?.FindAll("permission")?.Select(c => c.Value).ToList() ?? new List<string>();
    }

    /// <summary>
    /// Checks if current user has a specific permission
    /// </summary>
    protected bool HasPermission(string permission)
    {
        if (string.IsNullOrEmpty(permission))
            return false;

        return GetCurrentUserPermissions().Contains(permission);
    }

    /// <summary>
    /// Gets the client IP address
    /// </summary>
    protected string? GetClientIpAddress()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext == null) return null;

        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Gets the user agent string
    /// </summary>
    protected string? GetUserAgent()
    {
        var httpContext = Context.GetHttpContext();
        return httpContext?.Request.Headers["User-Agent"].FirstOrDefault();
    }

    /// <summary>
    /// Standard connection handling with logging and authentication validation
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        var fingerprint = GetDeviceFingerprint();
        var userRole = GetCurrentUserRole();
        var ipAddress = GetClientIpAddress();

        Logger.LogInformation(
            "User {UserId} ({Role}) connected to {HubType} from {IpAddress} - Connection: {ConnectionId}",
            userId, userRole, GetType().Name, ipAddress, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Standard disconnection handling with cleanup
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        if (exception != null)
        {
            Logger.LogWarning(exception,
                "User {UserId} ({Role}) disconnected from {HubType} with error - Connection: {ConnectionId}",
                userId, userRole, GetType().Name, Context.ConnectionId);
        }
        else
        {
            Logger.LogInformation(
                "User {UserId} ({Role}) disconnected from {HubType} - Connection: {ConnectionId}",
                userId, userRole, GetType().Name, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a SignalR group (with permission check)
    /// </summary>
    protected async Task<bool> JoinGroupSafe(string groupName, string requiredPermission)
    {
        if (!HasPermission(requiredPermission))
        {
            Logger.LogWarning(
                "User {UserId} attempted to join group {GroupName} without permission {Permission}",
                GetCurrentUserId(), groupName, requiredPermission);
            return false;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        Logger.LogDebug("User {UserId} joined group {GroupName}", GetCurrentUserId(), groupName);
        return true;
    }

    /// <summary>
    /// Leave a SignalR group
    /// </summary>
    protected async Task LeaveGroupSafe(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        Logger.LogDebug("User {UserId} left group {GroupName}", GetCurrentUserId(), groupName);
    }

    protected string? GetDeviceFingerprint()
    {
        var httpContext = Context.GetHttpContext();
        return httpContext?.Request.Query["deviceFingerprint"].FirstOrDefault();
    }
}
