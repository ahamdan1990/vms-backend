using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Interface for audit log repository operations
/// </summary>
public interface IAuditLogRepository : IGenericRepository<AuditLog>
{
    /// <summary>
    /// Gets audit logs for a specific entity
    /// </summary>
    /// <param name="entityType">Entity type</param>
    /// <param name="entityId">Entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit logs</returns>
    Task<List<AuditLog>> GetByEntityAsync(string entityType, int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit logs</returns>
    Task<List<AuditLog>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs within a date range
    /// </summary>
    /// <param name="fromDate">Start date</param>
    /// <param name="toDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit logs</returns>
    Task<List<AuditLog>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old audit logs
    /// </summary>
    /// <param name="olderThanDays">Delete logs older than specified days</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of deleted records</returns>
    Task<int> CleanupOldLogsAsync(int olderThanDays, CancellationToken cancellationToken = default);
}
