using VisitorManagementSystem.Api.Application.DTOs.Users;

namespace VisitorManagementSystem.Api.Application.Services.Users;

/// <summary>
/// Interface for user preferences management
/// </summary>
public interface IUserPreferencesService
{
    /// <summary>
    /// Updates user preferences
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="preferences">Preferences to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task UpdatePreferencesAsync(int userId, UpdateUserPreferencesDto preferences, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user preferences
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User preferences</returns>
    Task<UpdateUserPreferencesDto> GetPreferencesAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets user preferences to defaults
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task ResetToDefaultsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user theme preference
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="theme">Theme name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task UpdateThemeAsync(int userId, string theme, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user language preference
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="language">Language code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task UpdateLanguageAsync(int userId, string language, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user timezone preference
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="timeZone">Timezone</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task UpdateTimeZoneAsync(int userId, string timeZone, CancellationToken cancellationToken = default);
}
