using VisitorManagementSystem.Api.Application.DTOs.Users;

namespace VisitorManagementSystem.Api.Application.Services.Users;

/// <summary>
/// Interface for user activity tracking and management
/// </summary>
public interface IUserActivityService
{
    /// <summary>
    /// Gets user activity history
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user activities</returns>
    Task<List<UserActivityDto>> GetUserActivityAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated user activity history
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated user activities</returns>
    Task<List<UserActivityDto>> GetUserActivityAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a user activity
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="activityType">Type of activity</param>
    /// <param name="description">Activity description</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="userAgent">User agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task RecordActivityAsync(int userId, string activityType, string description, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user activity statistics
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="fromDate">Start date</param>
    /// <param name="toDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Activity statistics</returns>
    Task<UserActivityStatsDto> GetActivityStatsAsync(int userId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears old activity records
    /// </summary>
    /// <param name="olderThanDays">Delete records older than specified days</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of deleted records</returns>
    Task<int> CleanupOldActivitiesAsync(int olderThanDays = 365, CancellationToken cancellationToken = default);
}

/// <summary>
/// User activity statistics
/// </summary>
public class UserActivityStatsDto
{
    public int TotalActivities { get; set; }
    public DateTime? LastActivity { get; set; }
    public Dictionary<string, int> ActivitiesByType { get; set; } = new();
    public Dictionary<string, int> ActivitiesByDay { get; set; } = new();
}
