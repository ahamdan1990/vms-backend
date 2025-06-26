using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Services.Auth;

/// <summary>
/// Permission service interface for managing user permissions and authorization
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Gets all permissions for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user permissions</returns>
    Task<List<string>> GetUserPermissionsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions for a user by email
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user permissions</returns>
    Task<List<string>> GetUserPermissionsByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions for a specific role
    /// </summary>
    /// <param name="role">User role</param>
    /// <returns>List of role permissions</returns>
    List<string> GetRolePermissions(UserRole role);

    /// <summary>
    /// Checks if user has a specific permission
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permission">Permission to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has permission</returns>
    Task<bool> HasPermissionAsync(int userId, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if user has any of the specified permissions
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permissions">Permissions to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has any of the permissions</returns>
    Task<bool> HasAnyPermissionAsync(int userId, IEnumerable<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if user has all of the specified permissions
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permissions">Permissions to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has all permissions</returns>
    Task<bool> HasAllPermissionsAsync(int userId, IEnumerable<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if user can perform an action on a resource
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="action">Action to perform</param>
    /// <param name="resource">Resource type</param>
    /// <param name="resourceId">Resource ID (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization result</returns>
    Task<AuthorizationResult> CanPerformActionAsync(int userId, string action, string resource,
        int? resourceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available permissions in the system
    /// </summary>
    /// <returns>List of all permissions</returns>
    List<string> GetAllPermissions();

    /// <summary>
    /// Gets permissions grouped by category
    /// </summary>
    /// <returns>Dictionary of category to permissions</returns>
    Dictionary<string, List<string>> GetPermissionsByCategory();

    /// <summary>
    /// Validates if a permission exists
    /// </summary>
    /// <param name="permission">Permission to validate</param>
    /// <returns>True if permission exists</returns>
    bool IsValidPermission(string permission);

    /// <summary>
    /// Gets permission category
    /// </summary>
    /// <param name="permission">Permission</param>
    /// <returns>Category name or null</returns>
    string? GetPermissionCategory(string permission);

    /// <summary>
    /// Gets permission description
    /// </summary>
    /// <param name="permission">Permission</param>
    /// <returns>Permission description</returns>
    string GetPermissionDescription(string permission);

    /// <summary>
    /// Checks if user can manage another user (role hierarchy)
    /// </summary>
    /// <param name="managerId">Manager user ID</param>
    /// <param name="targetUserId">Target user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if manager can manage target user</returns>
    Task<bool> CanManageUserAsync(int managerId, int targetUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets roles that can be assigned by a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of assignable roles</returns>
    Task<List<UserRole>> GetAssignableRolesAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if user can assign a specific role
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="targetRole">Role to assign</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user can assign the role</returns>
    Task<bool> CanAssignRoleAsync(int userId, UserRole targetRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets resource-specific permissions for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="resourceType">Resource type</param>
    /// <param name="resourceId">Resource ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of permissions for the specific resource</returns>
    Task<List<string>> GetResourcePermissionsAsync(int userId, string resourceType, int resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds permission claims for JWT token
    /// </summary>
    /// <param name="permissions">User permissions</param>
    /// <returns>Permission claims</returns>
    List<System.Security.Claims.Claim> BuildPermissionClaims(List<string> permissions);

    /// <summary>
    /// Extracts permissions from claims
    /// </summary>
    /// <param name="claims">Claims collection</param>
    /// <returns>List of permissions</returns>
    List<string> ExtractPermissionsFromClaims(IEnumerable<System.Security.Claims.Claim> claims);

    /// <summary>
    /// Validates permission-based access to API endpoint
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="httpMethod">HTTP method</param>
    /// <param name="endpoint">API endpoint</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if access is allowed</returns>
    Task<bool> ValidateApiAccessAsync(int userId, string httpMethod, string endpoint,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permissions required for an API endpoint
    /// </summary>
    /// <param name="httpMethod">HTTP method</param>
    /// <param name="endpoint">API endpoint</param>
    /// <returns>List of required permissions</returns>
    List<string> GetRequiredPermissions(string httpMethod, string endpoint);

    /// <summary>
    /// Checks if user has elevated privileges
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has elevated privileges</returns>
    Task<bool> HasElevatedPrivilegesAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets permission audit information
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permission">Permission</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permission audit information</returns>
    Task<PermissionAuditInfo> GetPermissionAuditInfoAsync(int userId, string permission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates time-based access restrictions
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="currentTime">Current time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if access is allowed at current time</returns>
    Task<bool> ValidateTimeBasedAccessAsync(int userId, DateTime currentTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if user can access from current IP address
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if access is allowed from IP</returns>
    Task<bool> ValidateIpBasedAccessAsync(int userId, string ipAddress, CancellationToken cancellationToken = default);
}

/// <summary>
/// Authorization result
/// </summary>
public class AuthorizationResult
{
    public bool IsAuthorized { get; set; }
    public string? Reason { get; set; }
    public List<string> RequiredPermissions { get; set; } = new();
    public List<string> MissingPermissions { get; set; } = new();
    public string? ResourceOwner { get; set; }
    public bool IsResourceOwner { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Permission audit information
/// </summary>
public class PermissionAuditInfo
{
    public string Permission { get; set; } = string.Empty;
    public bool HasPermission { get; set; }
    public string Source { get; set; } = string.Empty; // Role, Custom, Inherited
    public DateTime GrantedOn { get; set; }
    public string? GrantedBy { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public string? Conditions { get; set; }
    public bool IsActive { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public string? Reason { get; set; }
}

/// <summary>
/// Permission requirement attribute data
/// </summary>
public class PermissionRequirement
{
    public string Permission { get; set; } = string.Empty;
    public string? Resource { get; set; }
    public string? Action { get; set; }
    public bool RequireOwnership { get; set; }
    public bool AllowSuperuser { get; set; } = true;
    public List<string> AlternativePermissions { get; set; } = new();
}

/// <summary>
/// API endpoint permission mapping
/// </summary>
public class ApiEndpointPermission
{
    public string HttpMethod { get; set; } = string.Empty;
    public string EndpointPattern { get; set; } = string.Empty;
    public List<string> RequiredPermissions { get; set; } = new();
    public bool RequireAuthentication { get; set; } = true;
    public bool AllowAnonymous { get; set; } = false;
    public List<UserRole> AllowedRoles { get; set; } = new();
    public string? Description { get; set; }
}