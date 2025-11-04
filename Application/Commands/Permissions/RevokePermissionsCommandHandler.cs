using MediatR;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Permissions;

/// <summary>
/// Handler for RevokePermissionsCommand
/// </summary>
public class RevokePermissionsCommandHandler : IRequestHandler<RevokePermissionsCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<RevokePermissionsCommandHandler> _logger;

    public RevokePermissionsCommandHandler(
        IUnitOfWork unitOfWork,
        IPermissionService permissionService,
        ILogger<RevokePermissionsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task<int> Handle(RevokePermissionsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify role exists
            var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken);
            if (role == null)
            {
                throw new InvalidOperationException($"Role with ID {request.RoleId} not found");
            }

            // Revoke permissions
            var revokedCount = await _unitOfWork.RolePermissions.BulkRevokePermissionsAsync(
                request.RoleId,
                request.RevokePermissionsDto.PermissionIds,
                cancellationToken);

            // Invalidate permission cache for this role
            _permissionService.InvalidateRolePermissionCache(request.RoleId);

            // CRITICAL: Invalidate cache for ALL users with this role to ensure immediate updates
            await InvalidateUserCachesForRoleAsync(request.RoleId, cancellationToken);

            _logger.LogInformation("Revoked {Count} permissions from role {RoleName} (ID: {RoleId}). Invalidated caches for all users with this role.",
                revokedCount, role.Name, request.RoleId);

            return revokedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking permissions from role {RoleId}", request.RoleId);
            throw;
        }
    }

    /// <summary>
    /// Invalidates permission caches for all users who have the specified role
    /// This ensures permission changes are reflected immediately
    /// </summary>
    private async Task InvalidateUserCachesForRoleAsync(int roleId, CancellationToken cancellationToken)
    {
        try
        {
            // Get all users with this role
            var usersWithRole = await _unitOfWork.Users.GetByRoleIdAsync(roleId, cancellationToken);

            foreach (var user in usersWithRole)
            {
                _permissionService.InvalidateUserPermissionCache(user.Id);
            }

            _logger.LogInformation("Invalidated permission caches for {Count} users with role ID {RoleId}",
                usersWithRole.Count, roleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating user caches for role {RoleId}", roleId);
            // Don't throw - cache invalidation failure shouldn't fail the whole operation
        }
    }
}
