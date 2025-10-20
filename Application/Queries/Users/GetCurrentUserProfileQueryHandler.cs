using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Users;

/// <summary>
/// Handler for getting current user profile
/// </summary>
public class GetCurrentUserProfileQueryHandler : IRequestHandler<GetCurrentUserProfileQuery, UserProfileDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCurrentUserProfileQueryHandler> _logger;

    public GetCurrentUserProfileQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetCurrentUserProfileQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<UserProfileDto> Handle(GetCurrentUserProfileQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting profile for user: {UserId}", request.UserId);

            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Profile requested for non-existent user: {UserId}", request.UserId);
                throw new InvalidOperationException($"User with ID '{request.UserId}' not found.");
            }

            // Use AutoMapper to map entity to DTO (includes ProfilePhotoUrl via UserProfilePhotoUrlResolver)
            var userProfileDto = _mapper.Map<UserProfileDto>(user);

            return userProfileDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile: {UserId}", request.UserId);
            throw;
        }
    }
}
