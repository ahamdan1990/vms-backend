using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Models;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for visitor operations
/// </summary>
public interface IVisitorRepository : IGenericRepository<Visitor>
{
    /// <summary>
    /// Checks if a visitor with the specified email exists
    /// </summary>
    /// <param name="email">Email address</param>
    /// <param name="excludeId">ID to exclude from check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if visitor exists</returns>
    Task<bool> EmailExistsAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a visitor by email address
    /// </summary>
    /// <param name="email">Email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Visitor if found</returns>
    Task<Visitor?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches visitors by name, email, or company
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated search results</returns>
    Task<(List<Visitor> Visitors, int TotalCount)> SearchVisitorsAsync(
        string searchTerm, 
        int pageIndex, 
        int pageSize, 
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets visitors by company name
    /// </summary>
    /// <param name="company">Company name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of visitors from the company</returns>
    Task<List<Visitor>> GetByCompanyAsync(string company, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all VIP visitors
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of VIP visitors</returns>
    Task<List<Visitor>> GetVipVisitorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all blacklisted visitors
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of blacklisted visitors</returns>
    Task<List<Visitor>> GetBlacklistedVisitorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets visitors with incomplete information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of visitors with missing required information</returns>
    Task<List<Visitor>> GetIncompleteProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets duplicate visitors based on email and name matching
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Groups of potential duplicate visitors</returns>
    Task<List<List<Visitor>>> GetPotentialDuplicatesAsync(CancellationToken cancellationToken = default);    /// <summary>
    /// Gets visitor statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Visitor statistics</returns>
    Task<VisitorStatistics> GetVisitorStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets visitors created within a date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of visitors created in the date range</returns>
    Task<List<Visitor>> GetVisitorsByDateRangeAsync(
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top companies by visitor count
    /// </summary>
    /// <param name="limit">Number of top companies to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of companies with visitor counts</returns>
    Task<List<CompanyVisitorCount>> GetTopCompaniesByVisitorCountAsync(
        int limit = 10, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates visitor visit statistics
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task UpdateVisitStatisticsAsync(int visitorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if visitor exists by government ID
    /// </summary>
    /// <param name="governmentId">Government ID</param>
    /// <param name="excludeId">ID to exclude from check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if visitor exists with the government ID</returns>
    Task<bool> GovernmentIdExistsAsync(
        string governmentId, 
        int? excludeId = null, 
        CancellationToken cancellationToken = default);

    // Method moved from RepositoryExtensions
    /// <summary>
    /// Get visitor by FR person ID
    /// </summary>
    /// <param name="frPersonId">FR person ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Visitor if found</returns>
    Task<Visitor?> GetByFRPersonIdAsync(
        string frPersonId,
        CancellationToken cancellationToken = default);
}

