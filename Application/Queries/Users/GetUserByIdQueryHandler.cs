using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.Queries.Users;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Application.DTOs.Auth;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Handler for GetUserByIdQuery
/// </summary>
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetUserByIdQueryHandler> _logger;

    public GetUserByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetUserByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserDetailDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing GetUserByIdQuery for UserId: {UserId}", request.UserId);

            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", request.UserId);
                return null;
            }

            var userDetailDto = new UserDetailDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Email = user.Email.Value,
                PhoneNumber = user.PhoneNumber?.Value,
                Role = user.Role.ToString(),
                Status = user.Status.ToString(),
                Department = user.Department,
                JobTitle = user.JobTitle,
                EmployeeId = user.EmployeeId,
                ProfilePhotoPath = user.ProfilePhotoPath,
                TimeZone = user.TimeZone,
                Language = user.Language,
                Theme = user.Theme,
                LastLoginDate = user.LastLoginDate,
                CreatedOn = user.CreatedOn,
                IsActive = user.Status == UserStatus.Active,
                IsLockedOut = user.IsCurrentlyLockedOut(),
                FailedLoginAttempts = user.FailedLoginAttempts,
                MustChangePassword = user.MustChangePassword,
                PasswordChangedDate = user.PasswordChangedDate,
                LockoutEnd = user.LockoutEnd,
                CreatedBy = user.CreatedBy,
                ModifiedOn = user.ModifiedOn,
                ModifiedBy = user.ModifiedBy
            };

            // Include activity summary if requested
            if (request.IncludeActivity)
            {
                var activitySummary = await _unitOfWork.Users.GetUserActivitySummaryAsync(request.UserId, 30, cancellationToken);
                userDetailDto.ActivitySummary = new UserActivityDto
                {
                    LoginCount = activitySummary.LoginCount,
                    LastLogin = activitySummary.LastLogin,
                    FailedLoginAttempts = activitySummary.FailedLoginAttempts,
                    LastFailedLogin = activitySummary.LastFailedLogin,
                    InvitationsCreated = activitySummary.InvitationsCreated,
                    PasswordChanges = activitySummary.PasswordChanges,
                    ActivityByType = activitySummary.ActivityByType,
                    RecentActions = activitySummary.RecentActions,
                    // Individual activity fields (not used for summary)
                    Action = string.Empty,
                    Description = string.Empty,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = null,
                    UserAgent = null,
                    IsSuccess = true
                };
            }

            // Include active sessions if requested (placeholder for now)
            if (request.IncludeSessions)
            {
                // TODO: Implement session retrieval
                userDetailDto.ActiveSessions = new List<UserSessionDto>();
            }

            return userDetailDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetUserByIdQuery for UserId: {UserId}", request.UserId);
            throw;
        }
    }
}