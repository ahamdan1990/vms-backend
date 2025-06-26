using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace VisitorManagementSystem.Api.Application.Services.Auth;

/// <summary>
/// Permission service implementation for managing user permissions and authorization
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PermissionService> _logger;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(15);

    public PermissionService(
        IUnitOfWork unitOfWork,
        ILogger<PermissionService> logger,
        IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cache = cache;
    }

    public async Task<List<string>> GetUserPermissionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"user_permissions_{userId}";

            if (_cache.TryGetValue(cacheKey, out List<string>? cachedPermissions))
            {
                return cachedPermissions ?? new List<string>();
            }

            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found for permissions check: {UserId}", userId);
                return new List<string>();
            }

            var permissions = GetRolePermissions(user.Role);

            _cache.Set(cacheKey, permissions, _cacheExpiry);

            _logger.LogDebug("Retrieved {Count} permissions for user {UserId}", permissions.Count, userId);
            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions for user {UserId}", userId);
            return new List<string>();
        }
    }

    public async Task<List<string>> GetUserPermissionsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found for permissions check: {Email}", email);
                return new List<string>();
            }

            return await GetUserPermissionsAsync(user.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions for user email {Email}", email);
            return new List<string>();
        }
    }

    public List<string> GetRolePermissions(UserRole role)
    {
        return UserRoles.GetDefaultPermissions(UserRoles.GetRoleName(role));
    }

    public async Task<bool> HasPermissionAsync(int userId, string permission, CancellationToken cancellationToken = default)
    {
        try
        {
            var permissions = await GetUserPermissionsAsync(userId, cancellationToken);
            return permissions.Contains(permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", permission, userId);
            return false;
        }
    }

    public async Task<bool> HasAnyPermissionAsync(int userId, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        try
        {
            var userPermissions = await GetUserPermissionsAsync(userId, cancellationToken);
            return permissions.Any(p => userPermissions.Contains(p));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking any permissions for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> HasAllPermissionsAsync(int userId, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        try
        {
            var userPermissions = await GetUserPermissionsAsync(userId, cancellationToken);
            return permissions.All(p => userPermissions.Contains(p));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking all permissions for user {UserId}", userId);
            return false;
        }
    }

    public async Task<AuthorizationResult> CanPerformActionAsync(int userId, string action, string resource,
        int? resourceId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return new AuthorizationResult
                {
                    IsAuthorized = false,
                    Reason = "User not found"
                };
            }

            var permission = $"{resource}.{action}";
            var hasPermission = await HasPermissionAsync(userId, permission, cancellationToken);

            if (!hasPermission)
            {
                // Check for broader permissions
                var broadPermission = $"{resource}.{action}.All";
                hasPermission = await HasPermissionAsync(userId, broadPermission, cancellationToken);

                if (!hasPermission && resourceId.HasValue)
                {
                    // Check for ownership-based permissions
                    var ownPermission = $"{resource}.{action}.Own";
                    var hasOwnPermission = await HasPermissionAsync(userId, ownPermission, cancellationToken);

                    if (hasOwnPermission)
                    {
                        var isOwner = await CheckResourceOwnershipAsync(userId, resource, resourceId.Value, cancellationToken);
                        hasPermission = isOwner;
                    }
                }
            }

            return new AuthorizationResult
            {
                IsAuthorized = hasPermission,
                Reason = hasPermission ? "Authorized" : "Insufficient permissions",
                RequiredPermissions = new List<string> { permission },
                MissingPermissions = hasPermission ? new List<string>() : new List<string> { permission }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authorization for user {UserId}, action {Action}, resource {Resource}",
                userId, action, resource);
            return new AuthorizationResult
            {
                IsAuthorized = false,
                Reason = "Authorization check failed"
            };
        }
    }

    public List<string> GetAllPermissions()
    {
        return Permissions.GetAllPermissions();
    }

    public Dictionary<string, List<string>> GetPermissionsByCategory()
    {
        return Permissions.GetPermissionsByCategory();
    }

    public bool IsValidPermission(string permission)
    {
        return Permissions.IsValidPermission(permission);
    }

    public string? GetPermissionCategory(string permission)
    {
        return Permissions.GetPermissionCategory(permission);
    }

    public string GetPermissionDescription(string permission)
    {
        var permissionType = Enum.GetValues<PermissionType>()
            .FirstOrDefault(p => p.GetPermissionString() == permission);

        return permissionType != PermissionType.UserCreate ?
            permissionType.GetDisplayName() :
            permission;
    }

    public async Task<bool> CanManageUserAsync(int managerId, int targetUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var manager = await _unitOfWork.Users.GetByIdAsync(managerId, cancellationToken);
            var targetUser = await _unitOfWork.Users.GetByIdAsync(targetUserId, cancellationToken);

            if (manager == null || targetUser == null)
            {
                return false;
            }

            // Administrators can manage all users except other administrators (unless they're the same person)
            if (manager.Role == UserRole.Administrator)
            {
                return targetUser.Role != UserRole.Administrator || managerId == targetUserId;
            }

            // Other roles cannot manage users
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {ManagerId} can manage user {TargetUserId}", managerId, targetUserId);
            return false;
        }
    }

    public async Task<List<UserRole>> GetAssignableRolesAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return new List<UserRole>();
            }

            return user.Role.GetAssignableRoles();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assignable roles for user {UserId}", userId);
            return new List<UserRole>();
        }
    }

    public async Task<bool> CanAssignRoleAsync(int userId, UserRole targetRole, CancellationToken cancellationToken = default)
    {
        try
        {
            var assignableRoles = await GetAssignableRolesAsync(userId, cancellationToken);
            return assignableRoles.Contains(targetRole);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} can assign role {Role}", userId, targetRole);
            return false;
        }
    }

    public async Task<List<string>> GetResourcePermissionsAsync(int userId, string resourceType, int resourceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var allPermissions = await GetUserPermissionsAsync(userId, cancellationToken);
            var resourcePermissions = allPermissions
                .Where(p => p.StartsWith($"{resourceType}.", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Filter based on ownership if applicable
            var isOwner = await CheckResourceOwnershipAsync(userId, resourceType, resourceId, cancellationToken);
            if (!isOwner)
            {
                // Remove "Own" permissions if user doesn't own the resource
                resourcePermissions = resourcePermissions
                    .Where(p => !p.EndsWith(".Own", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return resourcePermissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resource permissions for user {UserId}, resource {ResourceType}:{ResourceId}",
                userId, resourceType, resourceId);
            return new List<string>();
        }
    }

    public List<Claim> BuildPermissionClaims(List<string> permissions)
    {
        return permissions.Select(permission => new Claim("permission", permission)).ToList();
    }

    public List<string> ExtractPermissionsFromClaims(IEnumerable<Claim> claims)
    {
        return claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .ToList();
    }

    public async Task<bool> ValidateApiAccessAsync(int userId, string httpMethod, string endpoint,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requiredPermissions = GetRequiredPermissions(httpMethod, endpoint);
            if (!requiredPermissions.Any())
            {
                return true; // No specific permissions required
            }

            return await HasAnyPermissionAsync(userId, requiredPermissions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API access for user {UserId}, {Method} {Endpoint}",
                userId, httpMethod, endpoint);
            return false;
        }
    }

    public List<string> GetRequiredPermissions(string httpMethod, string endpoint)
    {
        // This would typically be configured via attributes or configuration
        // For now, implementing basic mapping
        var permissions = new List<string>();

        if (endpoint.Contains("/api/users", StringComparison.OrdinalIgnoreCase))
        {
            permissions.Add(httpMethod.ToUpper() switch
            {
                "GET" => Permissions.User.ReadAll,
                "POST" => Permissions.User.Create,
                "PUT" => Permissions.User.UpdateAll,
                "DELETE" => Permissions.User.DeleteAll,
                _ => ""
            });
        }
        else if (endpoint.Contains("/api/invitations", StringComparison.OrdinalIgnoreCase))
        {
            permissions.Add(httpMethod.ToUpper() switch
            {
                "GET" => Permissions.Invitation.ReadAll,
                "POST" => Permissions.Invitation.CreateAll,
                "PUT" => Permissions.Invitation.UpdateAll,
                "DELETE" => Permissions.Invitation.CancelAll,
                _ => ""
            });
        }

        return permissions.Where(p => !string.IsNullOrEmpty(p)).ToList();
    }

    public async Task<bool> HasElevatedPrivilegesAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            return user?.Role == UserRole.Administrator;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking elevated privileges for user {UserId}", userId);
            return false;
        }
    }

    public async Task<PermissionAuditInfo> GetPermissionAuditInfoAsync(int userId, string permission,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return new PermissionAuditInfo
                {
                    Permission = permission,
                    HasPermission = false,
                    Reason = "User not found"
                };
            }

            var hasPermission = await HasPermissionAsync(userId, permission, cancellationToken);

            return new PermissionAuditInfo
            {
                Permission = permission,
                HasPermission = hasPermission,
                Source = "Role",
                GrantedOn = user.CreatedOn,
                IsActive = user.IsActive && user.Status == UserStatus.Active,
                Reason = hasPermission ? $"Granted via role: {user.Role}" : "Permission not granted"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission audit info for user {UserId}, permission {Permission}",
                userId, permission);
            return new PermissionAuditInfo
            {
                Permission = permission,
                HasPermission = false,
                Reason = "Audit check failed"
            };
        }
    }

    public async Task<bool> ValidateTimeBasedAccessAsync(int userId, DateTime currentTime, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return false;
            }

            // For now, all users have 24/7 access
            // This could be extended to check user-specific time restrictions
            return user.IsActive && user.Status == UserStatus.Active;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating time-based access for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ValidateIpBasedAccessAsync(int userId, string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return false;
            }

            // For now, all IP addresses are allowed
            // This could be extended to check IP restrictions
            _logger.LogDebug("IP access validation for user {UserId} from IP {IpAddress}", userId, ipAddress);
            return user.IsActive && user.Status == UserStatus.Active;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating IP-based access for user {UserId}, IP {IpAddress}", userId, ipAddress);
            return false;
        }
    }

    private async Task<bool> CheckResourceOwnershipAsync(int userId, string resourceType, int resourceId, CancellationToken cancellationToken)
    {
        try
        {
            // This would check if the user owns the specific resource
            // Implementation depends on the resource type
            return resourceType.ToLower() switch
            {
                "invitation" => await CheckInvitationOwnershipAsync(userId, resourceId, cancellationToken),
                "user" => userId == resourceId, // Users own themselves
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking resource ownership for user {UserId}, resource {ResourceType}:{ResourceId}",
                userId, resourceType, resourceId);
            return false;
        }
    }

    private async Task<bool> CheckInvitationOwnershipAsync(int userId, int invitationId, CancellationToken cancellationToken)
    {
        try
        {
            // This would check if the user created the invitation
            // For now, returning false as invitation entity is not available in this chunk
            await Task.CompletedTask;
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking invitation ownership for user {UserId}, invitation {InvitationId}",
                userId, invitationId);
            return false;
        }
    }
}