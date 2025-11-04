using MediatR;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Permissions;

/// <summary>
/// Handler for GrantPermissionsCommand
/// </summary>
public class GrantPermissionsCommandHandler : IRequestHandler<GrantPermissionsCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<GrantPermissionsCommandHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GrantPermissionsCommandHandler(
        IUnitOfWork unitOfWork,
        IPermissionService permissionService,
        ILogger<GrantPermissionsCommandHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _permissionService = permissionService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<int> Handle(GrantPermissionsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify role exists
            var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken);
            if (role == null)
            {
                throw new InvalidOperationException($"Role with ID {request.RoleId} not found");
            }

            // Get current user ID
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var grantedBy))
            {
                throw new UnauthorizedAccessException("User not authenticated");
            }

            // Grant permissions
            var grantedCount = await _unitOfWork.RolePermissions.BulkGrantPermissionsAsync(
                request.RoleId,
                request.GrantPermissionsDto.PermissionIds,
                grantedBy,
                cancellationToken);

            // Invalidate permission cache for this role
            _permissionService.InvalidateRolePermissionCache(request.RoleId);

            // CRITICAL: Invalidate cache for ALL users with this role to ensure immediate updates
            await InvalidateUserCachesForRoleAsync(request.RoleId, cancellationToken);

            _logger.LogInformation("Granted {Count} permissions to role {RoleName} (ID: {RoleId}) by user {UserId}. Invalidated caches for all users with this role.",
                grantedCount, role.Name, request.RoleId, grantedBy);

            return grantedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting permissions to role {RoleId}", request.RoleId);
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
