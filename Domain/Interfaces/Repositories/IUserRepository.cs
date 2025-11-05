using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Specifications;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for User entity operations
/// </summary>
public interface IUserRepository : IGenericRepository<User>
{
    /// <summary>
    /// Gets a user by email address
    /// </summary>
    /// <param name="email">Email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User if found, null otherwise</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by employee ID
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User if found, null otherwise</returns>
    Task<User?> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users by role (enum-based - legacy)
    /// </summary>
    /// <param name="role">User role</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users with the specified role</returns>
    Task<List<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users by role ID (dynamic role system)
    /// </summary>
    /// <param name="roleId">Role ID from Roles table</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users with the specified role</returns>
    Task<List<User>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users by status
    /// </summary>
    /// <param name="status">User status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users with the specified status</returns>
    Task<List<User>> GetByStatusAsync(UserStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users by department
    /// </summary>
    /// <param name="department">Department name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users in the specified department</returns>
    Task<List<User>> GetByDepartmentAsync(string department, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches users by various criteria
    /// </summary>
    /// <param name="searchTerm">Search term to match against name, email, employee ID</param>
    /// <param name="role">Optional role filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="department">Optional department filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching users</returns>
    Task<List<User>> SearchAsync(string searchTerm, UserRole? role = null, UserStatus? status = null,
        string? department = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated users with sorting
    /// </summary>
    /// <param name="pageIndex">Page index (0-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="sortBy">Field to sort by</param>
    /// <param name="sortDescending">Sort direction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of users</returns>
    Task<(List<User> Users, int TotalCount)> GetPaginatedAsync(int pageIndex, int pageSize,
        string? sortBy = null, bool sortDescending = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active users
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active users</returns>
    Task<List<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets locked out users
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of locked out users</returns>
    Task<List<User>> GetLockedOutUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users with expired passwords
    /// </summary>
    /// <param name="passwordExpiryDays">Number of days after which password expires</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users with expired passwords</returns>
    Task<List<User>> GetUsersWithExpiredPasswordsAsync(int passwordExpiryDays, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users who must change their password
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users who must change password</returns>
    Task<List<User>> GetUsersMustChangePasswordAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users created within a date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users created in the date range</returns>
    Task<List<User>> GetUsersCreatedInPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users with recent login activity
    /// </summary>
    /// <param name="recentDays">Number of recent days</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users with recent login</returns>
    Task<List<User>> GetUsersWithRecentLoginAsync(int recentDays, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users with failed login attempts above threshold
    /// </summary>
    /// <param name="minFailedAttempts">Minimum failed attempts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users with failed login attempts</returns>
    Task<List<User>> GetUsersWithFailedLoginAttemptsAsync(int minFailedAttempts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users requiring security attention
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users requiring security attention</returns>
    Task<List<User>> GetUsersRequiringSecurityAttentionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users with valid refresh tokens
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users with valid refresh tokens</returns>
    Task<List<User>> GetUsersWithValidRefreshTokensAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inactive users for cleanup
    /// </summary>
    /// <param name="inactiveDays">Number of days of inactivity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of inactive users</returns>
    Task<List<User>> GetInactiveUsersForCleanupAsync(int inactiveDays, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if email exists
    /// </summary>
    /// <param name="email">Email address</param>
    /// <param name="excludeUserId">User ID to exclude from check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email exists</returns>
    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if employee ID exists
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="excludeUserId">User ID to exclude from check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if employee ID exists</returns>
    Task<bool> EmployeeIdExistsAsync(string employeeId, int? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User statistics</returns>
    Task<UserStatistics> GetUserStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user activity summary
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="days">Number of days to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User activity summary</returns>
    Task<UserActivitySummary> GetUserActivitySummaryAsync(int userId, int days = 30, CancellationToken cancellationToken = default);

    Task<PagedResultDto<UserActivityDto>> GetUserAuditLogsAsync(int userId, int days, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk updates user status
    /// </summary>
    /// <param name="userIds">List of user IDs</param>
    /// <param name="status">New status</param>
    /// <param name="modifiedBy">User making the change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of users updated</returns>
    Task<int> BulkUpdateStatusAsync(List<int> userIds, UserStatus status, int modifiedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk updates user roles
    /// </summary>
    /// <param name="userIds">List of user IDs</param>
    /// <param name="role">New role</param>
    /// <param name="modifiedBy">User making the change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of users updated</returns>
    Task<int> BulkUpdateRoleAsync(List<int> userIds, UserRole role, int modifiedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk reset failed login attempts
    /// </summary>
    /// <param name="userIds">List of user IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of users updated</returns>
    Task<int> BulkResetFailedLoginAttemptsAsync(List<int> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users by multiple IDs efficiently
    /// </summary>
    /// <param name="userIds">List of user IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of user ID to user</returns>
    Task<Dictionary<int, User>> GetUsersByIdsAsync(List<int> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a user for deletion (hard delete)
    /// </summary>
    /// <param name="user">User to delete</param>
    new void Delete(User user);

    // Method moved from RepositoryExtensions
    /// <summary>
    /// Gets users by role name (string-based)
    /// </summary>
    /// <param name="role">Role name (Staff, Administrator, Operator)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users with the specified role</returns>
    Task<IEnumerable<User>> GetUsersByRoleAsync(
        string role,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// User statistics data transfer object
/// </summary>
public class UserStatistics
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int LockedUsers { get; set; }
    public int SuspendedUsers { get; set; }
    public int PendingUsers { get; set; }
    public int StaffUsers { get; set; }
    public int AdminUsers { get; set; }
    public int ReceptionistUsers { get; set; }
    public int UsersWithExpiredPasswords { get; set; }
    public int UsersRequiringPasswordChange { get; set; }
    public int UsersWithFailedLogins { get; set; }
    public DateTime? LastUserCreated { get; set; }
    public DateTime? LastSuccessfulLogin { get; set; }
    public Dictionary<string, int> UsersByDepartment { get; set; } = new();
    public Dictionary<string, int> LoginsByDay { get; set; } = new();
}

/// <summary>
/// User activity summary data transfer object
/// </summary>
public class UserActivitySummary
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int LoginCount { get; set; }
    public DateTime? LastLogin { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LastFailedLogin { get; set; }
    public int InvitationsCreated { get; set; }
    public int PasswordChanges { get; set; }
    public Dictionary<string, int> ActivityByType { get; set; } = new();
    public List<string> RecentActions { get; set; } = new();
}