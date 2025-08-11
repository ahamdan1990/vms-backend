using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for visitor document operations
/// </summary>
public interface IVisitorDocumentRepository : IGenericRepository<VisitorDocument>
{
    /// <summary>
    /// Gets all documents for a specific visitor
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of visitor documents</returns>
    Task<List<VisitorDocument>> GetByVisitorIdAsync(int visitorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents by type for a visitor
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="documentType">Document type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of documents of the specified type</returns>
    Task<List<VisitorDocument>> GetByVisitorAndTypeAsync(
        int visitorId, 
        string documentType, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expired documents
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of expired documents</returns>
    Task<List<VisitorDocument>> GetExpiredDocumentsAsync(CancellationToken cancellationToken = default);
}
