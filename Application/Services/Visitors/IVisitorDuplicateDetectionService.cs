using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Application.Services.Visitors;

/// <summary>
/// Service for detecting duplicate visitors
/// </summary>
public interface IVisitorDuplicateDetectionService
{
    /// <summary>
    /// Finds an existing visitor that matches the provided criteria
    /// Used during visitor creation to prevent duplicates
    /// </summary>
    /// <param name="email">Visitor email (primary match criterion)</param>
    /// <param name="phoneNumber">Optional phone number for secondary matching</param>
    /// <param name="firstName">Optional first name for enhanced matching</param>
    /// <param name="lastName">Optional last name for enhanced matching</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Existing visitor if duplicate found, otherwise null</returns>
    Task<Visitor?> FindDuplicateVisitorAsync(
        string email,
        string? phoneNumber = null,
        string? firstName = null,
        string? lastName = null,
        CancellationToken cancellationToken = default);
}
