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

            _logger.LogInformation("Revoked {Count} permissions from role {RoleName} (ID: {RoleId})",
                revokedCount, role.Name, request.RoleId);

            return revokedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking permissions from role {RoleId}", request.RoleId);
            throw;
        }
    }
}
