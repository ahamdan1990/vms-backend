using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Services.Configuration;

/// <summary>
/// Dynamic configuration service implementation for managing database-driven settings
/// </summary>
public class DynamicConfigurationService : IDynamicConfigurationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DynamicConfigurationService> _logger;
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(30);
    private const string CacheKeyPrefix = "config_";

    public DynamicConfigurationService(
        IUnitOfWork unitOfWork,
        IMemoryCache cache,
        ILogger<DynamicConfigurationService> logger)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetConfigurationAsync<T>(string category, string key, T? defaultValue = default, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}{category}_{key}";
            
            if (_cache.TryGetValue(cacheKey, out T? cachedValue))
            {
                return cachedValue;
            }

            var config = await _unitOfWork.SystemConfigurations
                .GetByCategoryAndKeyAsync(category, key, cancellationToken);

            if (config == null)
            {
                _logger.LogWarning("Config not found: {Category}.{Key}, using default {DefaultValue}", category, key, defaultValue);
                _cache.Set(cacheKey, defaultValue, CacheExpiry);
                return defaultValue;
            }

            var value = ConvertValue<T>(config.Value, config.DataType);
            _logger.LogInformation("Config value retrieved from DB: {Category}.{Key} = {Value}", category, key, value);
            _cache.Set(cacheKey, value, CacheExpiry);
            
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration {Category}.{Key}", category, key);
            return defaultValue;
        }
    }

    public async Task<string?> GetConfigurationValueAsync(string category, string key, string? defaultValue = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await _unitOfWork.SystemConfigurations
                .GetByCategoryAndKeyAsync(category, key, cancellationToken);

            return config?.Value ?? defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration value {Category}.{Key}", category, key);
            return defaultValue;
        }
    }

    public async Task<bool> SetConfigurationAsync<T>(string category, string key, T value, int modifiedBy, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var jsonValue = JsonSerializer.Serialize(value);
            return await SetConfigurationValueAsync(category, key, jsonValue, modifiedBy, reason, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration {Category}.{Key}", category, key);
            return false;
        }
    }
    public async Task<bool> SetConfigurationValueAsync(string category, string key, string value, int modifiedBy, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await _unitOfWork.SystemConfigurations
                .GetByCategoryAndKeyAsync(category, key, cancellationToken);

            if (config == null)
            {
                return false; // Configuration doesn't exist
            }

            if (config.IsReadOnly)
            {
                _logger.LogWarning("Attempted to modify read-only configuration {Category}.{Key}", category, key);
                return false;
            }

            // Validate the new value
            var validationResult = await ValidateConfigurationAsync(category, key, value, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Invalid configuration value for {Category}.{Key}: {Errors}", 
                    category, key, string.Join(", ", validationResult.Errors));
                return false;
            }

            var oldValue = config.Value;
            config.UpdateValue(value, modifiedBy);

            // Log the change in audit table
            await LogConfigurationChangeAsync(config.Id, category, key, oldValue, value, "Updated", modifiedBy, reason, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Invalidate cache
            var cacheKey = $"{CacheKeyPrefix}{category}_{key}";
            _cache.Remove(cacheKey);
            
            _logger.LogInformation("Configuration updated: {Category}.{Key} by user {UserId}", category, key, modifiedBy);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration {Category}.{Key}", category, key);
            return false;
        }
    }
    public async Task<Dictionary<string, object>> GetCategoryConfigurationAsync(string category, CancellationToken cancellationToken = default)
    {
        try
        {
            var configurations = await _unitOfWork.SystemConfigurations
                .GetByCategoryAsync(category, cancellationToken);

            var result = new Dictionary<string, object>();

            foreach (var config in configurations)
            {
                var value = ConvertValue<object>(config.Value, config.DataType);
                if (value != null)
                {
                    result[config.Key] = value;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category configuration for {Category}", category);
            return new Dictionary<string, object>();
        }
    }

    public async Task<Dictionary<string, Dictionary<string, object>>> GetAllConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allConfigurations = await _unitOfWork.SystemConfigurations.GetAllAsync(cancellationToken);
            var result = new Dictionary<string, Dictionary<string, object>>();

            foreach (var config in allConfigurations)
            {
                if (!result.ContainsKey(config.Category))
                {
                    result[config.Category] = new Dictionary<string, object>();
                }

                var value = ConvertValue<object>(config.Value, config.DataType);

                    result[config.Category][config.Key] = value;
                
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all configurations");
            return new Dictionary<string, Dictionary<string, object>>();
        }
    }
    public async Task<bool> DeleteConfigurationAsync(string category, string key, int modifiedBy, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await _unitOfWork.SystemConfigurations
                .GetByCategoryAndKeyAsync(category, key, cancellationToken);

            if (config == null)
            {
                return false;
            }

            if (config.IsReadOnly)
            {
                _logger.LogWarning("Attempted to delete read-only configuration {Category}.{Key}", category, key);
                return false;
            }

            var oldValue = config.Value;
            
            // Log the deletion
            await LogConfigurationChangeAsync(config.Id, category, key, oldValue, "", "Deleted", modifiedBy, reason, cancellationToken);

            _unitOfWork.SystemConfigurations.Remove(config);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Invalidate cache
            var cacheKey = $"{CacheKeyPrefix}{category}_{key}";
            _cache.Remove(cacheKey);

            _logger.LogInformation("Configuration deleted: {Category}.{Key} by user {UserId}", category, key, modifiedBy);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration {Category}.{Key}", category, key);
            return false;
        }
    }

    public async Task InvalidateCacheAsync(string? category = null)
    {
        try
        {
            if (string.IsNullOrEmpty(category))
            {
                // Clear all configuration cache
                if (_cache is MemoryCache memoryCache)
                {
                    var field = typeof(MemoryCache).GetField("_coherentState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field?.GetValue(memoryCache) is object coherentState)
                    {
                        var entriesCollection = coherentState.GetType().GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (entriesCollection?.GetValue(coherentState) is System.Collections.IDictionary entries)
                        {
                            var keysToRemove = entries.Keys.Cast<object>()
                                .Where(k => k.ToString()?.StartsWith(CacheKeyPrefix) == true)
                                .ToList();

                            foreach (var key in keysToRemove)
                            {
                                _cache.Remove(key);
                            }
                        }
                    }
                }
            }
            else
            {
                // Clear cache for specific category
                var configurations = await _unitOfWork.SystemConfigurations.GetByCategoryAsync(category);
                foreach (var config in configurations)
                {
                    var cacheKey = $"{CacheKeyPrefix}{config.Category}_{config.Key}";
                    _cache.Remove(cacheKey);
                }
            }

            _logger.LogInformation("Configuration cache invalidated for category: {Category}", category ?? "All");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for category: {Category}", category);
        }
    }

    public async Task InvalidateAllCacheAsync()
    {
        await InvalidateCacheAsync(null);
    }
    public async Task<SystemConfiguration?> GetConfigurationMetadataAsync(string category, string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _unitOfWork.SystemConfigurations
                .GetByCategoryAndKeyAsync(category, key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration metadata {Category}.{Key}", category, key);
            return null;
        }
    }

    public async Task<List<ConfigurationAudit>> GetConfigurationHistoryAsync(string category, string key, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _unitOfWork.ConfigurationAudits
                .GetConfigurationHistoryAsync(category, key, pageSize, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration history {Category}.{Key}", category, key);
            return new List<ConfigurationAudit>();
        }
    }

    public async Task<ValidationResult> ValidateConfigurationAsync(string category, string key, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await _unitOfWork.SystemConfigurations
                .GetByCategoryAndKeyAsync(category, key, cancellationToken);

            if (config == null)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = { "Configuration not found" }
                };
            }

            var result = new ValidationResult { IsValid = true };

            if (!config.IsValidValue(value))
            {
                result.IsValid = false;
                result.Errors.Add($"Value '{value}' is not valid for configuration {category}.{key}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration {Category}.{Key}", category, key);
            return new ValidationResult
            {
                IsValid = false,
                Errors = { "Validation failed due to system error" }
            };
        }
    }
    public async Task<SystemConfiguration?> CreateConfigurationAsync(SystemConfiguration configuration, int modifiedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if configuration already exists
            var exists = await _unitOfWork.SystemConfigurations
                .ExistsAsync(configuration.Category, configuration.Key, cancellationToken);

            if (exists)
            {
                _logger.LogWarning("Configuration {Category}.{Key} already exists", configuration.Category, configuration.Key);
                return null;
            }

            configuration.SetCreatedBy(modifiedBy);
            await _unitOfWork.SystemConfigurations.AddAsync(configuration, cancellationToken);

            // Log the creation
            await LogConfigurationChangeAsync(configuration.Id, configuration.Category, configuration.Key, 
                null, configuration.Value, "Created", modifiedBy, "Configuration created", cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Configuration created: {Category}.{Key} by user {UserId}", 
                configuration.Category, configuration.Key, modifiedBy);

            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating configuration {Category}.{Key}", configuration.Category, configuration.Key);
            return null;
        }
    }

    public async Task<List<SystemConfiguration>> SearchConfigurationsAsync(string searchTerm, string? category = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _unitOfWork.SystemConfigurations.SearchAsync(searchTerm, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching configurations with term: {SearchTerm}", searchTerm);
            return new List<SystemConfiguration>();
        }
    }
    #region Helper Methods

    /// <summary>
    /// Converts configuration value to specified type
    /// </summary>
    private T? ConvertValue<T>(string value, string dataType)
    {
        try
        {
            if (string.IsNullOrEmpty(value))
                return default(T);

            return dataType.ToLower() switch
            {
                "string" => (T)(object)value,
                "int" or "integer" => (T)(object)int.Parse(value),
                "bool" or "boolean" => (T)(object)bool.Parse(value),
                "timespan" => (T)(object)TimeSpan.Parse(value),
                "datetime" => (T)(object)DateTime.Parse(value),
                "decimal" => (T)(object)decimal.Parse(value),
                "double" => (T)(object)double.Parse(value),
                "object" => JsonSerializer.Deserialize<T>(value),
                _ => JsonSerializer.Deserialize<T>(value)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting value '{Value}' to type '{DataType}'", value, dataType);
            return default(T);
        }
    }

    /// <summary>
    /// Logs configuration changes to audit table
    /// </summary>
    private async Task LogConfigurationChangeAsync(int configurationId, string category, string key, 
        string? oldValue, string newValue, string action, int modifiedBy, string? reason, CancellationToken cancellationToken)
    {
        try
        {
            var audit = new ConfigurationAudit
            {
                SystemConfigurationId = configurationId,
                Category = category,
                Key = key,
                OldValue = oldValue,
                NewValue = newValue,
                Action = action,
                Reason = reason,
                CreatedBy = modifiedBy
            };

            await _unitOfWork.ConfigurationAudits.AddAsync(audit, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging configuration change for {Category}.{Key}", category, key);
        }
    }

    #endregion
}
