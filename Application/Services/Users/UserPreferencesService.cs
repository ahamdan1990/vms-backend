using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Services.Users;

/// <summary>
/// User preferences service implementation
/// </summary>
public class UserPreferencesService : IUserPreferencesService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserPreferencesService> _logger;

    public UserPreferencesService(IUnitOfWork unitOfWork, ILogger<UserPreferencesService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task UpdatePreferencesAsync(int userId, UpdateUserPreferencesDto preferences, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found.");
        }

        user.UpdatePreferences(preferences.TimeZone, preferences.Language, preferences.Theme);
        
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Updated preferences for user {UserId}", userId);
    }
    public async Task<UpdateUserPreferencesDto> GetPreferencesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found.");
        }

        return new UpdateUserPreferencesDto
        {
            TimeZone = user.TimeZone,
            Language = user.Language,
            Theme = user.Theme
        };
    }

    public async Task ResetToDefaultsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found.");
        }

        user.UpdatePreferences("UTC", "en-US", "light");
        
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Reset preferences to defaults for user {UserId}", userId);
    }

    public async Task UpdateThemeAsync(int userId, string theme, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found.");
        }

        user.UpdatePreferences(theme: theme);
        
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Updated theme to {Theme} for user {UserId}", theme, userId);
    }

    public async Task UpdateLanguageAsync(int userId, string language, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found.");
        }

        user.UpdatePreferences(language: language);
        
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Updated language to {Language} for user {UserId}", language, userId);
    }

    public async Task UpdateTimeZoneAsync(int userId, string timeZone, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found.");
        }

        user.UpdatePreferences(timeZone: timeZone);
        
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Updated timezone to {TimeZone} for user {UserId}", timeZone, userId);
    }
}
