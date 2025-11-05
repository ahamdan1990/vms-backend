using Microsoft.AspNetCore.Authorization;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Infrastructure.Security.Authorization;

/// <summary>
/// Role-based authorization handler
/// </summary>
public class RoleHandler : AuthorizationHandler<RoleRequirement>
{
    private readonly ILogger<RoleHandler> _logger;

    public RoleHandler(ILogger<RoleHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleRequirement requirement)
    {
        try
        {
            // Check if user is authenticated
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogDebug("User not authenticated for role requirement: {Role}", requirement.Role);
                context.Fail();
                return Task.CompletedTask;
            }

            // Get user's role
            var userRoleClaim = context.User.FindFirst("role")?.Value;
            if (string.IsNullOrEmpty(userRoleClaim))
            {
                _logger.LogWarning("User has no role claim");
                context.Fail();
                return Task.CompletedTask;
            }

            // Parse user role
            if (!Enum.TryParse<UserRole>(userRoleClaim, out var userRole))
            {
                _logger.LogWarning("Invalid user role: {Role}", userRoleClaim);
                context.Fail();
                return Task.CompletedTask;
            }

            // Check if user has the required role or higher
            if (HasRequiredRoleOrHigher(userRole, requirement.Role))
            {
                _logger.LogDebug("Role access granted: {UserRole} for requirement: {RequiredRole}", userRole, requirement.Role);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("Role access denied: {UserRole} for requirement: {RequiredRole}", userRole, requirement.Role);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during role authorization for: {Role}", requirement.Role);
            context.Fail();
        }

        return Task.CompletedTask;
    }

    private static bool HasRequiredRoleOrHigher(UserRole userRole, UserRole requiredRole)
    {
        // Define role hierarchy: Staff < Operator < Administrator
        var roleHierarchy = new Dictionary<UserRole, int>
        {
            { UserRole.Staff, 1 },
            { UserRole.Receptionist, 2 },
            { UserRole.Administrator, 3 }
        };

        return roleHierarchy.GetValueOrDefault(userRole, 0) >= roleHierarchy.GetValueOrDefault(requiredRole, 0);
    }
}

/// <summary>
/// Role requirement for authorization
/// </summary>
public class RoleRequirement : IAuthorizationRequirement
{
    public UserRole Role { get; }

    public RoleRequirement(UserRole role)
    {
        Role = role;
    }
}

/// <summary>
/// Multiple roles requirement (user must have ANY of the roles)
/// </summary>
public class MultipleRolesRequirement : IAuthorizationRequirement
{
    public IEnumerable<UserRole> Roles { get; }

    public MultipleRolesRequirement(IEnumerable<UserRole> roles)
    {
        Roles = roles ?? throw new ArgumentNullException(nameof(roles));
    }
}

/// <summary>
/// Handler for multiple roles requirement
/// </summary>
public class MultipleRolesHandler : AuthorizationHandler<MultipleRolesRequirement>
{
    private readonly ILogger<MultipleRolesHandler> _logger;

    public MultipleRolesHandler(ILogger<MultipleRolesHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MultipleRolesRequirement requirement)
    {
        try
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var userRoleClaim = context.User.FindFirst("role")?.Value;
            if (string.IsNullOrEmpty(userRoleClaim))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            if (!Enum.TryParse<UserRole>(userRoleClaim, out var userRole))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            // Check if user has any of the required roles
            if (requirement.Roles.Contains(userRole))
            {
                _logger.LogDebug("Role access granted: {UserRole} matches one of: {RequiredRoles}",
                    userRole, string.Join(", ", requirement.Roles));
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("Role access denied: {UserRole} not in: {RequiredRoles}",
                    userRole, string.Join(", ", requirement.Roles));
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during multiple roles authorization");
            context.Fail();
        }

        return Task.CompletedTask;
    }
}