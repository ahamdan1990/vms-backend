using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Application.Services.Configuration;

/// <summary>
/// Interface for dynamic configuration management service
/// </summary>
public interface IDynamicConfigurationService
{
    /// <summary>
    /// Gets a configuration value with strong typing
    /// </summary>
    /// <typeparam name="T">Type to convert value to</typeparam>
    /// <param name="category">Configuration category</param>
    /// <param name="key">Configuration key</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Configuration value or default</returns>
    Task<T?> GetConfigurationAsync<T>(string category, string key, T? defaultValue = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a configuration value as string
    /// </summary>
    /// <param name="category">Configuration category</param>
    /// <param name="key">Configuration key</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Configuration value or default</returns>
    Task<string?> GetConfigurationValueAsync(string category, string key, string? defaultValue = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a configuration value with strong typing
    /// </summary>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <param name="category">Configuration category</param>
    /// <param name="key">Configuration key</param>
    /// <param name="value">Configuration value</param>
    /// <param name="modifiedBy">User making the change</param>
    /// <param name="reason">Reason for change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> SetConfigurationAsync<T>(string category, string key, T value, int modifiedBy, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a configuration value as string
    /// </summary>
    /// <param name="category">Configuration category</param>
    /// <param name="key">Configuration key</param>
    /// <param name="value">Configuration value</param>
    /// <param name="modifiedBy">User making the change</param>
    /// <param name="reason">Reason for change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> SetConfigurationValueAsync(string category, string key, string value, int modifiedBy, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configurations for a category as a dictionary
    /// </summary>
    /// <param name="category">Configuration category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of key-value pairs</returns>
    Task<Dictionary<string, object>> GetCategoryConfigurationAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configurations as a hierarchical structure
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary grouped by category</returns>
    Task<Dictionary<string, Dictionary<string, object>>> GetAllConfigurationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a configuration
    /// </summary>
    /// <param name="category">Configuration category</param>
    /// <param name="key">Configuration key</param>
    /// <param name="modifiedBy">User making the change</param>
    /// <param name="reason">Reason for deletion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteConfigurationAsync(string category, string key, int modifiedBy, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cache for specific category or all configurations
    /// </summary>
    /// <param name="category">Category to invalidate, null for all</param>
    /// <returns>Task</returns>
    Task InvalidateCacheAsync(string? category = null);

    /// <summary>
    /// Invalidates all configuration caches
    /// </summary>
    /// <returns>Task</returns>
    Task InvalidateAllCacheAsync();

    /// <summary>
    /// Gets configuration metadata
    /// </summary>
    /// <param name="category">Configuration category</param>
    /// <param name="key">Configuration key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Configuration metadata</returns>
    Task<SystemConfiguration?> GetConfigurationMetadataAsync(string category, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configuration history
    /// </summary>
    /// <param name="category">Configuration category</param>
    /// <param name="key">Configuration key</param>
    /// <param name="pageSize">Number of records to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit entries</returns>
    Task<List<ConfigurationAudit>> GetConfigurationHistoryAsync(string category, string key, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a configuration value
    /// </summary>
    /// <param name="category">Configuration category</param>
    /// <param name="key">Configuration key</param>
    /// <param name="value">Value to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateConfigurationAsync(string category, string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new configuration
    /// </summary>
    /// <param name="configuration">Configuration to create</param>
    /// <param name="modifiedBy">User creating the configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created configuration</returns>
    Task<SystemConfiguration?> CreateConfigurationAsync(SystemConfiguration configuration, int modifiedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches configurations
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="category">Optional category filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching configurations</returns>
    Task<List<SystemConfiguration>> SearchConfigurationsAsync(string searchTerm, string? category = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration validation result
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string? SuggestedValue { get; set; }
}
