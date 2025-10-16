using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Infrastructure.Security.Authorization;

/// <summary>
/// Custom authorization policy provider
/// </summary>
public class PolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _defaultProvider;
    private readonly ILogger<PolicyProvider> _logger;

    public PolicyProvider(IOptions<AuthorizationOptions> options, ILogger<PolicyProvider> logger)
    {
        _defaultProvider = new DefaultAuthorizationPolicyProvider(options);
        _logger = logger;
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _defaultProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _defaultProvider.GetFallbackPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Handle permission-based policies
        if (policyName.StartsWith("Permission."))
        {
            var permission = policyName["Permission.".Length..];
            return CreatePermissionPolicy(permission);
        }

        // Handle role-based policies
        if (policyName.StartsWith("Role."))
        {
            var roleName = policyName["Role.".Length..];
            if (Enum.TryParse<UserRole>(roleName, out var role))
            {
                return CreateRolePolicy(role);
            }
        }

        // Handle multiple permissions policies
        if (policyName.StartsWith("Permissions.All."))
        {
            var permissionsString = policyName["Permissions.All.".Length..];
            var permissions = permissionsString.Split(',');
            return CreateMultiplePermissionsPolicy(permissions);
        }

        // Handle any permission policies
        if (policyName.StartsWith("Permissions.Any."))
        {
            var permissionsString = policyName["Permissions.Any.".Length..];
            var permissions = permissionsString.Split(',');
            return CreateAnyPermissionPolicy(permissions);
        }

        // Handle composite policies
        if (policyName.StartsWith("Composite."))
        {
            return CreateCompositePolicy(policyName);
        }

        // Fallback to default provider
        return _defaultProvider.GetPolicyAsync(policyName);
    }

    private Task<AuthorizationPolicy?> CreatePermissionPolicy(string permission)
    {
        try
        {
            // Validate permission exists
            if (!Permissions.IsValidPermission(permission))
            {
                _logger.LogWarning("Invalid permission in policy: {Permission}", permission);
                return Task.FromResult<AuthorizationPolicy?>(null);
            }

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission policy for: {Permission}", permission);
            return Task.FromResult<AuthorizationPolicy?>(null);
        }
    }

    private Task<AuthorizationPolicy?> CreateRolePolicy(UserRole role)
    {
        try
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new RoleRequirement(role))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role policy for: {Role}", role);
            return Task.FromResult<AuthorizationPolicy?>(null);
        }
    }

    private Task<AuthorizationPolicy?> CreateMultiplePermissionsPolicy(string[] permissions)
    {
        try
        {
            // Validate all permissions exist
            var invalidPermissions = permissions.Where(p => !Permissions.IsValidPermission(p)).ToList();
            if (invalidPermissions.Any())
            {
                _logger.LogWarning("Invalid permissions in policy: {Permissions}", string.Join(", ", invalidPermissions));
                return Task.FromResult<AuthorizationPolicy?>(null);
            }

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new MultiplePermissionsRequirement(permissions))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating multiple permissions policy for: {Permissions}", string.Join(", ", permissions));
            return Task.FromResult<AuthorizationPolicy?>(null);
        }
    }

    private Task<AuthorizationPolicy?> CreateAnyPermissionPolicy(string[] permissions)
    {
        try
        {
            var invalidPermissions = permissions.Where(p => !Permissions.IsValidPermission(p)).ToList();
            if (invalidPermissions.Any())
            {
                _logger.LogWarning("Invalid permissions in policy: {Permissions}", string.Join(", ", invalidPermissions));
                return Task.FromResult<AuthorizationPolicy?>(null);
            }

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new AnyPermissionRequirement(permissions))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating any permission policy for: {Permissions}", string.Join(", ", permissions));
            return Task.FromResult<AuthorizationPolicy?>(null);
        }
    }

    private Task<AuthorizationPolicy?> CreateCompositePolicy(string policyName)
    {
        try
        {
            var policyBuilder = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser();

            // Define composite policies
            switch (policyName)
            {
                case "Composite.AdminOrOperator":
                    policyBuilder.AddRequirements(new MultipleRolesRequirement(new[] { UserRole.Administrator, UserRole.Operator }));
                    break;

                case "Composite.InvitationManager":
                    policyBuilder.AddRequirements(new AnyPermissionRequirement(new[]
                    {
                        Permissions.Invitation.CreateAll,
                        Permissions.Invitation.UpdateAll,
                        Permissions.Invitation.ApproveAll
                    }));
                    break;

                case "Composite.VisitorOperations":
                    policyBuilder.AddRequirements(new MultiplePermissionsRequirement(new[]
                    {
                        Permissions.Visitor.ReadToday,
                        Permissions.CheckIn.Process
                    }));
                    break;

                case "Composite.SystemManager":
                    policyBuilder.AddRequirements(new AnyPermissionRequirement(new[]
                    {
                        Permissions.SystemConfig.Update,
                        Permissions.User.Create,
                        Permissions.FRSystem.Configure
                    }));
                    break;

                default:
                    _logger.LogWarning("Unknown composite policy: {PolicyName}", policyName);
                    return Task.FromResult<AuthorizationPolicy?>(null);
            }

            var policy = policyBuilder.Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating composite policy: {PolicyName}", policyName);
            return Task.FromResult<AuthorizationPolicy?>(null);
        }
    }
}

/// <summary>
/// Authorization policy names for easy reference
/// </summary>
public static class PolicyNames
{
    // Role-based policies
    public const string RequireAdministrator = "Role.Administrator";
    public const string RequireOperator = "Role.Operator";
    public const string RequireStaff = "Role.Staff";

    // Permission-based policies
    public const string ManageUsers = "Permission." + Permissions.User.Create;
    public const string ViewAllInvitations = "Permission." + Permissions.Invitation.ReadAll;
    public const string ProcessCheckIn = "Permission." + Permissions.CheckIn.Process;
    public const string ConfigureSystem = "Permission." + Permissions.SystemConfig.Update;

    // Composite policies
    public const string AdminOrOperator = "Composite.AdminOrOperator";
    public const string InvitationManager = "Composite.InvitationManager";
    public const string VisitorOperations = "Composite.VisitorOperations";
    public const string SystemManager = "Composite.SystemManager";

    // Multiple permissions policies
    public const string FullUserManagement = "Permissions.All." + Permissions.User.Create + "," + Permissions.User.Update + "," + Permissions.User.Delete;
    public const string InvitationOperations = "Permissions.Any." + Permissions.Invitation.CreateOwn + "," + Permissions.Invitation.CreateAll;
}