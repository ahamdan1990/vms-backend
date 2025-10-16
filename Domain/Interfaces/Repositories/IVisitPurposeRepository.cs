using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for visit purpose operations
/// </summary>
public interface IVisitPurposeRepository : IGenericRepository<VisitPurpose>
{
    /// <summary>
    /// Gets all visit purposes ordered by display order
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of visit purposes</returns>
    Task<List<VisitPurpose>> GetOrderedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets visit purpose by code
    /// </summary>
    /// <param name="code">Purpose code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Visit purpose if found</returns>
    Task<VisitPurpose?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a purpose code exists
    /// </summary>
    /// <param name="code">Purpose code</param>
    /// <param name="excludeId">ID to exclude from check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if code exists</returns>
    Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
}
