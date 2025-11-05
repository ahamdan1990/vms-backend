using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VisitorManagementSystem.Api.Application.DTOs.Configuration;
using VisitorManagementSystem.Api.Application.Services.Configuration;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Controllers.Admin;

/// <summary>
/// Admin controller for managing system configurations
/// </summary>
[ApiController]
[Route("api/admin/configuration")]
[Authorize(Roles = "Administrator")]
public class ConfigurationController : ControllerBase
{
    private readonly IDynamicConfigurationService _configService;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(
        IDynamicConfigurationService configService, 
        ILogger<ConfigurationController> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all configurations grouped by category
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Permissions.SystemConfig.Read)]
    public async Task<IActionResult> GetAllConfigurations(CancellationToken cancellationToken)
    {
        try
        {
            var configurations = await _configService.GetAllConfigurationsAsync(cancellationToken);
            return Ok(new { success = true, data = configurations });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all configurations");
            return StatusCode(500, new { success = false, message = "Failed to retrieve configurations" });
        }
    }

    /// <summary>
    /// Gets all configurations for a specific category
    /// </summary>
    [HttpGet("{category}")]
    [Authorize(Policy = Permissions.SystemConfig.Read)]
    public async Task<IActionResult> GetCategoryConfiguration(string category, CancellationToken cancellationToken)
    {
        try
        {
            var configurations = await _configService.GetCategoryConfigurationAsync(category, cancellationToken);
            return Ok(new { success = true, data = configurations, category });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration for category: {Category}", category);
            return StatusCode(500, new { success = false, message = $"Failed to retrieve configurations for category: {category}" });
        }
    }

    /// <summary>
    /// Gets a specific configuration value
    /// </summary>
    [HttpGet("{category}/{key}")]
    [Authorize(Policy = Permissions.SystemConfig.Read)]
    public async Task<IActionResult> GetConfiguration(string category, string key, CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await _configService.GetConfigurationMetadataAsync(category, key, cancellationToken);
            if (metadata == null)
            {
                return NotFound(new { success = false, message = $"Configuration {category}.{key} not found" });
            }

            var value = await _configService.GetConfigurationValueAsync(category, key, cancellationToken: cancellationToken);
            
            return Ok(new 
            { 
                success = true, 
                data = new 
                {
                    category = metadata.Category,
                    key = metadata.Key,
                    value = metadata.IsSensitive ? "***" : value,
                    dataType = metadata.DataType,
                    description = metadata.Description,
                    isReadOnly = metadata.IsReadOnly,
                    isEncrypted = metadata.IsEncrypted,
                    isSensitive = metadata.IsSensitive,
                    requiresRestart = metadata.RequiresRestart,
                    defaultValue = metadata.DefaultValue
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration {Category}.{Key}", category, key);
            return StatusCode(500, new { success = false, message = "Failed to retrieve configuration" });
        }
    }
    /// <summary>
    /// Updates a configuration value
    /// </summary>
    [HttpPut("{category}/{key}")]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    public async Task<IActionResult> UpdateConfiguration(string category, string key, [FromBody] UpdateConfigurationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }

            // Validate the configuration first
            var validation = await _configService.ValidateConfigurationAsync(category, key, request.Value, cancellationToken);
            if (!validation.IsValid)
            {
                return BadRequest(new 
                { 
                    success = false, 
                    message = "Invalid configuration value", 
                    errors = validation.Errors,
                    warnings = validation.Warnings
                });
            }

            var success = await _configService.SetConfigurationValueAsync(category, key, request.Value, userId, request.Reason, cancellationToken);
            
            if (success)
            {
                _logger.LogInformation("Configuration {Category}.{Key} updated by user {UserId}", category, key, userId);
                
                // Check if this change requires restart
                var metadata = await _configService.GetConfigurationMetadataAsync(category, key, cancellationToken);
                var requiresRestart = metadata?.RequiresRestart ?? false;

                return Ok(new 
                { 
                    success = true, 
                    message = "Configuration updated successfully",
                    requiresRestart = requiresRestart,
                    restartWarning = requiresRestart ? "This change requires application restart to take effect" : null
                });
            }

            return BadRequest(new { success = false, message = "Failed to update configuration" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration {Category}.{Key}", category, key);
            return StatusCode(500, new { success = false, message = "Failed to update configuration" });
        }
    }

    /// <summary>
    /// Creates a new configuration
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    public async Task<IActionResult> CreateConfiguration([FromBody] CreateConfigurationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }

            var config = new SystemConfiguration
            {
                Category = request.Category,
                Key = request.Key,
                Value = request.Value,
                DataType = request.DataType,
                Description = request.Description,
                RequiresRestart = request.RequiresRestart,
                IsEncrypted = request.IsEncrypted,
                IsSensitive = request.IsSensitive,
                IsReadOnly = request.IsReadOnly,
                DefaultValue = request.DefaultValue,
                ValidationRules = request.ValidationRules,
                Group = request.Group,
                Environment = request.Environment ?? "All",
                DisplayOrder = request.DisplayOrder
            };

            var createdConfig = await _configService.CreateConfigurationAsync(config, userId, cancellationToken);
            
            if (createdConfig != null)
            {
                _logger.LogInformation("Configuration {Category}.{Key} created by user {UserId}", request.Category, request.Key, userId);
                return CreatedAtAction(nameof(GetConfiguration), 
                    new { category = createdConfig.Category, key = createdConfig.Key }, 
                    new { success = true, message = "Configuration created successfully", data = createdConfig });
            }

            return BadRequest(new { success = false, message = "Failed to create configuration" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating configuration {Category}.{Key}", request.Category, request.Key);
            return StatusCode(500, new { success = false, message = "Failed to create configuration" });
        }
    }
    /// <summary>
    /// Deletes a configuration
    /// </summary>
    [HttpDelete("{category}/{key}")]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    public async Task<IActionResult> DeleteConfiguration(string category, string key, [FromBody] DeleteConfigurationRequest? request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }

            var reason = request?.Reason ?? "Configuration deleted by administrator";
            var success = await _configService.DeleteConfigurationAsync(category, key, userId, reason, cancellationToken);
            
            if (success)
            {
                _logger.LogInformation("Configuration {Category}.{Key} deleted by user {UserId}", category, key, userId);
                return Ok(new { success = true, message = "Configuration deleted successfully" });
            }

            return NotFound(new { success = false, message = "Configuration not found or cannot be deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration {Category}.{Key}", category, key);
            return StatusCode(500, new { success = false, message = "Failed to delete configuration" });
        }
    }

    /// <summary>
    /// Gets configuration history/audit trail
    /// </summary>
    [HttpGet("{category}/{key}/history")]
    [Authorize(Policy = Permissions.SystemConfig.Read)]
    public async Task<IActionResult> GetConfigurationHistory(string category, string key, CancellationToken cancellationToken, [FromQuery] int pageSize = 50)
    {
        try
        {
            var history = await _configService.GetConfigurationHistoryAsync(category, key, pageSize, cancellationToken);
            return Ok(new { success = true, data = history });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration history for {Category}.{Key}", category, key);
            return StatusCode(500, new { success = false, message = "Failed to retrieve configuration history" });
        }
    }

    /// <summary>
    /// Searches configurations
    /// </summary>
    [HttpGet("search")]
    [Authorize(Policy = Permissions.SystemConfig.Read)]
    public async Task<IActionResult> SearchConfigurations([FromQuery] string searchTerm, [FromQuery] string? category = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest(new { success = false, message = "Search term is required" });
            }

            var results = await _configService.SearchConfigurationsAsync(searchTerm, category, cancellationToken);
            return Ok(new { success = true, data = results, searchTerm, category });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching configurations with term: {SearchTerm}", searchTerm);
            return StatusCode(500, new { success = false, message = "Failed to search configurations" });
        }
    }

    /// <summary>
    /// Validates a configuration value without saving
    /// </summary>
    [HttpPost("{category}/{key}/validate")]
    [Authorize(Policy = Permissions.SystemConfig.Read)]
    public async Task<IActionResult> ValidateConfiguration(string category, string key, [FromBody] ValidateConfigurationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var validation = await _configService.ValidateConfigurationAsync(category, key, request.Value, cancellationToken);
            return Ok(new { success = true, validation });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration {Category}.{Key}", category, key);
            return StatusCode(500, new { success = false, message = "Failed to validate configuration" });
        }
    }
    /// <summary>
    /// Invalidates configuration cache
    /// </summary>
    [HttpPost("cache/invalidate")]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    public async Task<IActionResult> InvalidateCache([FromQuery] string? category = null)
    {
        try
        {
            await _configService.InvalidateCacheAsync(category);
            var message = category == null ? "All configuration cache invalidated" : $"Cache invalidated for category: {category}";
            
            _logger.LogInformation("Configuration cache invalidated by user {UserId} for category: {Category}", GetCurrentUserId(), category ?? "All");
            return Ok(new { success = true, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating configuration cache for category: {Category}", category);
            return StatusCode(500, new { success = false, message = "Failed to invalidate cache" });
        }
    }

    /// <summary>
    /// Gets current user ID from claims
    /// </summary>
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim?.Value, out var userId) ? userId : 0;
    }
}

