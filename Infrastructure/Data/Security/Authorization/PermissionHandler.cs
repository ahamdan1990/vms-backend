using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using VisitorManagementSystem.Api.Domain.Constants;

namespace VisitorManagementSystem.Api.Infrastructure.Security.Authorization;



/// <summary>
/// Multiple permissions requirement (user must have ALL permissions)
/// </summary>
public class MultiplePermissionsRequirement : IAuthorizationRequirement
{
    public IEnumerable<string> Permissions { get; }

    public MultiplePermissionsRequirement(IEnumerable<string> permissions)
    {
        Permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
    }
}

/// <summary>
/// Any permission requirement (user must have ANY of the permissions)
/// </summary>
public class AnyPermissionRequirement : IAuthorizationRequirement
{
    public IEnumerable<string> Permissions { get; }

    public AnyPermissionRequirement(IEnumerable<string> permissions)
    {
        Permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
    }
}

/// <summary>
/// Permission-based authorization handler
/// </summary>
public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ILogger<PermissionHandler> _logger;

    public PermissionHandler(ILogger<PermissionHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        try
        {
            // Check if user is authenticated
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogDebug("User not authenticated for permission: {Permission}", requirement.Permission);
                context.Fail();
                return Task.CompletedTask;
            }

            // CRITICAL FIX: Check for permission CLAIMS instead of hardcoded role permissions
            // The PermissionClaimsMiddleware adds all user permissions from the database as claims
            var permissionClaims = context.User.Claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToList();

            // Get user's role for logging
            var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            // Check if user has the required permission claim
            if (permissionClaims.Contains(requirement.Permission))
            {
                _logger.LogDebug("✅ Permission granted: {Permission} for user {UserId} with role {Role}",
                    requirement.Permission, userId, userRole);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("❌ Permission denied: {Permission} for user {UserId} with role {Role}. User has {Count} permissions.",
                    requirement.Permission, userId, userRole, permissionClaims.Count);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during permission authorization for: {Permission}", requirement.Permission);
            context.Fail();
        }

        return Task.CompletedTask;
    }
}

// ALSO UPDATE the other handlers in the same file

/// <summary>
/// Handler for multiple permissions requirement
/// </summary>
public class MultiplePermissionsHandler : AuthorizationHandler<MultiplePermissionsRequirement>
{
    private readonly ILogger<MultiplePermissionsHandler> _logger;

    public MultiplePermissionsHandler(ILogger<MultiplePermissionsHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MultiplePermissionsRequirement requirement)
    {
        try
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            // CRITICAL FIX: Check permission CLAIMS instead of hardcoded role permissions
            var permissionClaims = context.User.Claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToList();

            var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            // Check if user has ALL required permissions
            var hasAllPermissions = requirement.Permissions.All(permission => permissionClaims.Contains(permission));

            if (hasAllPermissions)
            {
                _logger.LogDebug("✅ All permissions granted for user {UserId} with role {Role}", userId, userRole);
                context.Succeed(requirement);
            }
            else
            {
                var missingPermissions = requirement.Permissions.Where(p => !permissionClaims.Contains(p));
                _logger.LogWarning("❌ Missing permissions: {Permissions} for user {UserId} with role {Role}",
                    string.Join(", ", missingPermissions), userId, userRole);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during multiple permissions authorization");
            context.Fail();
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for any permission requirement
/// </summary>
public class AnyPermissionHandler : AuthorizationHandler<AnyPermissionRequirement>
{
    private readonly ILogger<AnyPermissionHandler> _logger;

    public AnyPermissionHandler(ILogger<AnyPermissionHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AnyPermissionRequirement requirement)
    {
        try
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            // CRITICAL FIX: Check permission CLAIMS instead of hardcoded role permissions
            var permissionClaims = context.User.Claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToList();

            var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            // Check if user has ANY of the required permissions
            var hasAnyPermission = requirement.Permissions.Any(permission => permissionClaims.Contains(permission));

            if (hasAnyPermission)
            {
                _logger.LogDebug("✅ At least one permission granted for user {UserId} with role {Role}", userId, userRole);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("❌ No matching permissions for user {UserId} with role {Role}, Required: {Permissions}",
                    userId, userRole, string.Join(", ", requirement.Permissions));
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during any permission authorization");
            context.Fail();
        }

        return Task.CompletedTask;
    }
}