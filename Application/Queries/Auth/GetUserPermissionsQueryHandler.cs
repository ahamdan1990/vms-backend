using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.DTOs.Auth;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Auth
{
    /// <summary>
    /// Handler for get user permissions query
    /// </summary>
    public class GetUserPermissionsQueryHandler : IRequestHandler<GetUserPermissionsQuery, UserPermissionsDto>
    {
        private readonly IPermissionService _permissionService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetUserPermissionsQueryHandler> _logger;

        public GetUserPermissionsQueryHandler(
            IPermissionService permissionService,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetUserPermissionsQueryHandler> logger)
        {
            _permissionService = permissionService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserPermissionsDto> Handle(GetUserPermissionsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing get user permissions query for user: {UserId}", request.UserId);

                // Get user information
                var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("User not found for permissions query: {UserId}", request.UserId);
                    return new UserPermissionsDto
                    {
                        UserId = request.UserId,
                        Permissions = new List<string>(),
                        PermissionsByCategory = new Dictionary<string, List<string>>(),
                        Role = null
                    };
                }

                // Get user permissions
                var permissions = await _permissionService.GetUserPermissionsAsync(request.UserId, cancellationToken);

                // Group permissions by category
                var permissionsByCategory = new Dictionary<string, List<string>>();
                foreach (var permission in permissions)
                {
                    var category = _permissionService.GetPermissionCategory(permission) ?? "General";
                    if (!permissionsByCategory.ContainsKey(category))
                    {
                        permissionsByCategory[category] = new List<string>();
                    }
                    permissionsByCategory[category].Add(permission);
                }

                // Get assignable roles
                var assignableRoles = await _permissionService.GetAssignableRolesAsync(request.UserId, cancellationToken);

                var result = new UserPermissionsDto
                {
                    UserId = request.UserId,
                    Role = user.Role.ToString(),
                    Permissions = permissions,
                    PermissionsByCategory = permissionsByCategory,
                    HasElevatedPrivileges = await _permissionService.HasElevatedPrivilegesAsync(request.UserId, cancellationToken)
                };

                _logger.LogDebug("Retrieved {Count} permissions for user {UserId}", permissions.Count, request.UserId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing get user permissions query for user: {UserId}", request.UserId);
                return new UserPermissionsDto
                {
                    UserId = request.UserId,
                    Permissions = new List<string>(),
                    PermissionsByCategory = new Dictionary<string, List<string>>(),
                    Role = null
                };
            }
        }
    }

}
