using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for SystemConfiguration entity
/// </summary>
public interface ISystemConfigurationRepository : IGenericRepository<SystemConfiguration>
{
    /// <summary>
    /// Gets configuration by category and key
    /// </summary>
    /// <param name="category">Configuration category</param>
    /// <param name="key">Configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>System configuration or null</returns>
    Task<SystemConfiguration?> GetByCategoryAndKeyAsync(string category, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configurations for a category
    /// </summary>
    /// <param name="category">Configuration category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of configurations</returns>
    Task<List<SystemConfiguration>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configuration categories
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of categories</returns>
    Task<List<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configurations that require restart
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of configurations requiring restart</returns>
    Task<List<SystemConfiguration>> GetConfigurationsRequiringRestartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets encrypted configurations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of encrypted configurations</returns>
    Task<List<SystemConfiguration>> GetEncryptedConfigurationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configurations by environment
    /// </summary>
    /// <param name="environment">Environment name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of configurations</returns>
    Task<List<SystemConfiguration>> GetByEnvironmentAsync(string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches configurations by key or description
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching configurations</returns>
    Task<List<SystemConfiguration>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configurations modified after a specific date
    /// </summary>
    /// <param name="since">Date to check from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recently modified configurations</returns>
    Task<List<SystemConfiguration>> GetModifiedSinceAsync(DateTime since, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk updates configurations
    /// </summary>
    /// <param name="configurations">Configurations to update</param>
    /// <param name="modifiedBy">User making the changes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of updated configurations</returns>
    Task<int> BulkUpdateAsync(List<SystemConfiguration> configurations, int modifiedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a configuration exists
    /// </summary>
    /// <param name="category">Configuration category</param>
    /// <param name="key">Configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists</returns>
    Task<bool> ExistsAsync(string category, string key, CancellationToken cancellationToken = default);
}
