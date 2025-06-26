using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Auth;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Auth
{
    /// <summary>
    /// Handler for get current user query
    /// UPDATED: Now works with UserId from JWT claims
    /// </summary>
    public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto?>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<GetCurrentUserQueryHandler> _logger;

        public GetCurrentUserQueryHandler(
            IUnitOfWork unitOfWork,
            IPermissionService permissionService,
            ILogger<GetCurrentUserQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _permissionService = permissionService;
            _logger = logger;
        }

        public async Task<CurrentUserDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing get current user query for UserId: {UserId}", request.UserId);

                var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", request.UserId);
                    return null;
                }

                // Check if user is still valid for authentication
                if (!user.IsValidForAuthentication())
                {
                    _logger.LogWarning("User account is not valid for authentication: {UserId}", request.UserId);
                    return null;
                }

                var permissions = await _permissionService.GetUserPermissionsAsync(user.Id, cancellationToken);

                var currentUser = new CurrentUserDto
                {
                    Id = user.Id,
                    Email = user.Email.Value,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    Role = user.Role.ToString(),
                    Status = user.Status.ToString(),
                    Department = user.Department,
                    JobTitle = user.JobTitle,
                    TimeZone = user.TimeZone,
                    Language = user.Language,
                    Theme = user.Theme,
                    Permissions = permissions,
                    LastLoginDate = user.LastLoginDate,
                    PasswordChangedDate = user.PasswordChangedDate
                };

                _logger.LogDebug("Current user retrieved successfully: {UserId}", currentUser.Id);
                return currentUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing get current user query for UserId: {UserId}", request.UserId);
                return null;
            }
        }
    }
}