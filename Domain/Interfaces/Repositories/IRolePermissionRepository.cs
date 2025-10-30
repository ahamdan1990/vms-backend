using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for RolePermission entity
/// </summary>
public interface IRolePermissionRepository
{
    /// <summary>
    /// Gets role permissions with eager-loaded permission details
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of role permissions with permission details</returns>
    Task<List<RolePermission>> GetRolePermissionsWithDetailsAsync(int roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions for a role by role name
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of role permissions</returns>
    Task<List<RolePermission>> GetRolePermissionsByNameAsync(string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a role has a specific permission
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="permissionId">Permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if role has permission</returns>
    Task<bool> RoleHasPermissionAsync(int roleId, int permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Grants a permission to a role
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="permissionId">Permission ID</param>
    /// <param name="grantedBy">User ID who granted the permission</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created role permission</returns>
    Task<RolePermission> GrantPermissionToRoleAsync(int roleId, int permissionId, int grantedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a permission from a role
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="permissionId">Permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if revoked successfully</returns>
    Task<bool> RevokePermissionFromRoleAsync(int roleId, int permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all role permissions with details (role and permission info)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all role permissions</returns>
    Task<List<RolePermission>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk grants permissions to a role
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="permissionIds">List of permission IDs</param>
    /// <param name="grantedBy">User ID who granted the permissions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of permissions granted</returns>
    Task<int> BulkGrantPermissionsAsync(int roleId, List<int> permissionIds, int grantedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk revokes permissions from a role
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="permissionIds">List of permission IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of permissions revoked</returns>
    Task<int> BulkRevokePermissionsAsync(int roleId, List<int> permissionIds, CancellationToken cancellationToken = default);
}
