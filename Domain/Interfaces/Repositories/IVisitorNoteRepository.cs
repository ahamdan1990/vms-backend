using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for visitor note operations
/// </summary>
public interface IVisitorNoteRepository : IGenericRepository<VisitorNote>
{
    /// <summary>
    /// Gets all notes for a specific visitor
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of visitor notes</returns>
    Task<List<VisitorNote>> GetByVisitorIdAsync(int visitorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets flagged notes that require follow-up
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of flagged notes</returns>
    Task<List<VisitorNote>> GetFlaggedNotesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets overdue follow-up notes
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of overdue notes</returns>
    Task<List<VisitorNote>> GetOverdueFollowUpsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notes by category
    /// </summary>
    /// <param name="category">Note category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of notes in the category</returns>
    Task<List<VisitorNote>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
}
