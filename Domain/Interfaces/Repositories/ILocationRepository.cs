using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for location operations
/// </summary>
public interface ILocationRepository : IGenericRepository<Location>
{
    /// <summary>
    /// Gets all locations ordered by display order
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of locations</returns>
    Task<List<Location>> GetOrderedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets location by code
    /// </summary>
    /// <param name="code">Location code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Location if found</returns>
    Task<Location?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets locations by type
    /// </summary>
    /// <param name="locationType">Location type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of locations of the specified type</returns>
    Task<List<Location>> GetByTypeAsync(string locationType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets root locations (no parent)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of root locations</returns>
    Task<List<Location>> GetRootLocationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets child locations of a parent location
    /// </summary>
    /// <param name="parentId">Parent location ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of child locations</returns>
    Task<List<Location>> GetChildLocationsAsync(int parentId, CancellationToken cancellationToken = default);

    // Method moved from RepositoryExtensions
    /// <summary>
    /// Get active locations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active locations</returns>
    Task<IEnumerable<Location>> GetActiveLocationsAsync(
        CancellationToken cancellationToken = default);
}
