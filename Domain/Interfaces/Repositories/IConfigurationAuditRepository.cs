using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for ConfigurationAudit entity
/// </summary>
public interface IConfigurationAuditRepository : IGenericRepository<ConfigurationAudit>
{
    /// <summary>
    /// Gets audit history for a specific configuration
    /// </summary>
    /// <param name="category">Configuration category</param>
    /// <param name="key">Configuration key</param>
    /// <param name="pageSize">Number of records to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit entries</returns>
    Task<List<ConfigurationAudit>> GetConfigurationHistoryAsync(string category, string key, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit history for a configuration ID
    /// </summary>
    /// <param name="configurationId">Configuration ID</param>
    /// <param name="pageSize">Number of records to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit entries</returns>
    Task<List<ConfigurationAudit>> GetConfigurationHistoryByIdAsync(int configurationId, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit entries by user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="days">Number of days to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit entries</returns>
    Task<List<ConfigurationAudit>> GetByUserAsync(int userId, int days = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent configuration changes
    /// </summary>
    /// <param name="hours">Hours to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recent audit entries</returns>
    Task<List<ConfigurationAudit>> GetRecentChangesAsync(int hours = 24, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configuration changes by category
    /// </summary>
    /// <param name="category">Configuration category</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit entries</returns>
    Task<List<ConfigurationAudit>> GetByCategoryAsync(string category, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configuration changes by action type
    /// </summary>
    /// <param name="action">Action type (Created, Updated, Deleted)</param>
    /// <param name="days">Number of days to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit entries</returns>
    Task<List<ConfigurationAudit>> GetByActionAsync(string action, int days = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configuration changes requiring approval
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit entries awaiting approval</returns>
    Task<List<ConfigurationAudit>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches audit entries
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching audit entries</returns>
    Task<List<ConfigurationAudit>> SearchAsync(string searchTerm, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit statistics
    /// </summary>
    /// <param name="days">Number of days to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audit statistics</returns>
    Task<ConfigurationAuditStats> GetAuditStatsAsync(int days = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives old audit entries
    /// </summary>
    /// <param name="olderThanDays">Archive entries older than this many days</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of archived entries</returns>
    Task<int> ArchiveOldEntriesAsync(int olderThanDays, CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration audit statistics
/// </summary>
public class ConfigurationAuditStats
{
    public int TotalChanges { get; set; }
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int DeletedCount { get; set; }
    public int UniqueUsers { get; set; }
    public int UniqueConfigurations { get; set; }
    public Dictionary<string, int> ChangesByCategory { get; set; } = new();
    public Dictionary<string, int> ChangesByUser { get; set; } = new();
    public Dictionary<string, int> ChangesByDay { get; set; } = new();
    public DateTime? MostRecentChange { get; set; }
    public string? MostActiveCategory { get; set; }
    public string? MostActiveUser { get; set; }
}
