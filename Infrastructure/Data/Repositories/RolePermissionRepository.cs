using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for RolePermission entity operations
/// </summary>
public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly ApplicationDbContext _context;

    public RolePermissionRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<RolePermission>> GetRolePermissionsWithDetailsAsync(int roleId, CancellationToken cancellationToken = default)
    {
        return await _context.RolePermissions
            .Include(rp => rp.Permission)
            .Include(rp => rp.Role)
            .Where(rp => rp.RoleId == roleId)
            .OrderBy(rp => rp.Permission.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RolePermission>> GetRolePermissionsByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return await _context.RolePermissions
            .Include(rp => rp.Permission)
            .Include(rp => rp.Role)
            .Where(rp => rp.Role.Name == roleName)
            .OrderBy(rp => rp.Permission.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RoleHasPermissionAsync(int roleId, int permissionId, CancellationToken cancellationToken = default)
    {
        return await _context.RolePermissions
            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);
    }

    public async Task<RolePermission> GrantPermissionToRoleAsync(int roleId, int permissionId, int grantedBy, CancellationToken cancellationToken = default)
    {
        // Check if already exists
        var existing = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);

        if (existing != null)
        {
            return existing; // Already granted
        }

        var rolePermission = new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
            GrantedBy = grantedBy,
            GrantedAt = DateTime.UtcNow
        };

        await _context.RolePermissions.AddAsync(rolePermission, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return rolePermission;
    }

    public async Task<bool> RevokePermissionFromRoleAsync(int roleId, int permissionId, CancellationToken cancellationToken = default)
    {
        var rolePermission = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);

        if (rolePermission == null)
        {
            return false; // Not found
        }

        _context.RolePermissions.Remove(rolePermission);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<List<RolePermission>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RolePermissions
            .Include(rp => rp.Permission)
            .Include(rp => rp.Role)
            .Include(rp => rp.GrantedByUser)
            .OrderBy(rp => rp.Role.HierarchyLevel)
            .ThenBy(rp => rp.Permission.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> BulkGrantPermissionsAsync(int roleId, List<int> permissionIds, int grantedBy, CancellationToken cancellationToken = default)
    {
        // Get existing permissions for this role
        var existingPermissionIds = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId && permissionIds.Contains(rp.PermissionId))
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);

        // Filter out already existing permissions
        var newPermissionIds = permissionIds.Except(existingPermissionIds).ToList();

        if (!newPermissionIds.Any())
        {
            return 0; // No new permissions to grant
        }

        var rolePermissions = newPermissionIds.Select(permissionId => new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
            GrantedBy = grantedBy,
            GrantedAt = DateTime.UtcNow
        }).ToList();

        await _context.RolePermissions.AddRangeAsync(rolePermissions, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return rolePermissions.Count;
    }

    public async Task<int> BulkRevokePermissionsAsync(int roleId, List<int> permissionIds, CancellationToken cancellationToken = default)
    {
        var rolePermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId && permissionIds.Contains(rp.PermissionId))
            .ToListAsync(cancellationToken);

        if (!rolePermissions.Any())
        {
            return 0; // No permissions to revoke
        }

        _context.RolePermissions.RemoveRange(rolePermissions);
        await _context.SaveChangesAsync(cancellationToken);

        return rolePermissions.Count;
    }
}
