using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for emergency contact operations
/// </summary>
public interface IEmergencyContactRepository : IGenericRepository<EmergencyContact>
{
    /// <summary>
    /// Gets all emergency contacts for a specific visitor
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of emergency contacts</returns>
    Task<List<EmergencyContact>> GetByVisitorIdAsync(int visitorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the primary emergency contact for a visitor
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Primary emergency contact if exists</returns>
    Task<EmergencyContact?> GetPrimaryContactAsync(int visitorId, CancellationToken cancellationToken = default);
}
