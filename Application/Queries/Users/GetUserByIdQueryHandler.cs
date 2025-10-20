using AutoMapper;
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
    private readonly IMapper _mapper;
    private readonly ILogger<GetUserByIdQueryHandler> _logger;

    public GetUserByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetUserByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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

            // Use AutoMapper to map entity to DTO (includes ProfilePhotoUrl via UserProfilePhotoUrlResolver)
            var userDetailDto = _mapper.Map<UserDetailDto>(user);

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

            // Include active sessions if requested
            if (request.IncludeSessions)
            {
                // Session retrieval not implemented - would require session tracking mechanism
                userDetailDto.ActiveSessions = new List<UserSessionDto>();
                _logger.LogDebug("Session retrieval requested but not implemented for user {UserId}", request.UserId);
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