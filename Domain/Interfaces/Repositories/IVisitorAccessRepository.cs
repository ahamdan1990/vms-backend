using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for visitor access operations
/// </summary>
public interface IVisitorAccessRepository : IGenericRepository<VisitorAccess>
{
    /// <summary>
    /// Checks if a user has access to a visitor
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has access</returns>
    Task<bool> HasAccessAsync(
        int userId,
        int visitorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all visitor IDs accessible by a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of accessible visitor IDs</returns>
    Task<List<int>> GetAccessibleVisitorIdsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Grants access to a visitor for a user
    /// </summary>
    /// <param name="userId">User ID to grant access to</param>
    /// <param name="visitorId">Visitor ID to grant access for</param>
    /// <param name="accessType">Type of access being granted</param>
    /// <param name="grantedBy">User ID granting the access</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task GrantAccessAsync(
        int userId,
        int visitorId,
        VisitorAccessType accessType,
        int grantedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all access records for a visitor
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of visitor access records</returns>
    Task<List<VisitorAccess>> GetByVisitorIdAsync(
        int visitorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all access records for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of visitor access records</returns>
    Task<List<VisitorAccess>> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes access to a visitor for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RevokeAccessAsync(
        int userId,
        int visitorId,
        CancellationToken cancellationToken = default);
}
