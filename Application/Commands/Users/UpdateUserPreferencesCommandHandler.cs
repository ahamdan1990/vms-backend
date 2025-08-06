using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Users;

/// <summary>
/// Handler for update user preferences command
/// </summary>
public class UpdateUserPreferencesCommandHandler : IRequestHandler<UpdateUserPreferencesCommand, UserProfileDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateUserPreferencesCommandHandler> _logger;

    public UpdateUserPreferencesCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateUserPreferencesCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<UserProfileDto> Handle(UpdateUserPreferencesCommand request, CancellationToken cancellationToken)
    {        try
        {
            _logger.LogDebug("Processing update user preferences command for user: {UserId}", request.UserId);

            // Get existing user
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Attempt to update preferences for non-existent user: {UserId}", request.UserId);
                throw new InvalidOperationException($"User with ID '{request.UserId}' not found.");
            }

            // Update preferences
            user.UpdatePreferences(
                timeZone: request.TimeZone,
                language: request.Language,
                theme: request.Theme);

            // Update user in repository
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User preferences updated successfully: {UserId}", user.Id);

            // Map to DTO
            var userProfileDto = _mapper.Map<UserProfileDto>(user);
            return userProfileDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user preferences: {UserId}", request.UserId);
            throw;
        }
    }
}
