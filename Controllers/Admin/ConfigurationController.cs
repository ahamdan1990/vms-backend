using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using VisitorManagementSystem.Api.Application.Queries.Configuration;
using VisitorManagementSystem.Api.Application.Commands.Configuration;
using VisitorManagementSystem.Api.Application.DTOs.Configuration;
using VisitorManagementSystem.Api.Domain.Constants;

namespace VisitorManagementSystem.Api.Controllers.Admin;

/// <summary>
/// Admin controller for managing system configurations
/// </summary>
[ApiController]
[Route("api/admin/configuration")]
[Authorize(Roles = "Administrator")]
public class ConfigurationController : BaseController
{
    private readonly IMediator _mediator;

    public ConfigurationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all configurations grouped by category
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Permissions.Configuration.ReadAll)]
    public async Task<IActionResult> GetAllConfigurations()
    {
        var query = new GetAllConfigurationsQuery();
        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets all configurations for a specific category
    /// </summary>
    [HttpGet("{category}")]
    [Authorize(Policy = Permissions.Configuration.Read)]
    public async Task<IActionResult> GetCategoryConfiguration(string category)
    {
        var query = new GetCategoryConfigurationQuery { Category = category };
        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets a specific configuration value
    /// </summary>
    [HttpGet("{category}/{key}")]
    [Authorize(Policy = Permissions.Configuration.Read)]
    public async Task<IActionResult> GetConfiguration(string category, string key)
    {
        var query = new GetConfigurationQuery { Category = category, Key = key };
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFoundResponse($"Configuration {category}.{key}");
        }

        return SuccessResponse(result);
    }    /// <summary>
    /// Updates a configuration value
    /// </summary>
    [HttpPut("{category}/{key}")]
    [Authorize(Policy = Permissions.Configuration.Update)]
    public async Task<IActionResult> UpdateConfiguration(string category, string key, [FromBody] UpdateConfigurationDto request)
    {
        var command = new UpdateConfigurationCommand
        {
            Category = category,
            Key = key,
            Value = request.Value,
            Reason = request.Reason,
            ModifiedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result, "Configuration updated successfully");
    }

    /// <summary>
    /// Creates a new configuration
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.Configuration.Create)]
    public async Task<IActionResult> CreateConfiguration([FromBody] CreateConfigurationDto request)
    {
        var command = new CreateConfigurationCommand
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
            Environment = request.Environment,
            DisplayOrder = request.DisplayOrder,
            CreatedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return CreatedResponse(result, Url.Action(nameof(GetConfiguration), new { category = result.Category, key = result.Key }));
    }

    /// <summary>
    /// Deletes a configuration
    /// </summary>
    [HttpDelete("{category}/{key}")]
    [Authorize(Policy = Permissions.Configuration.Delete)]
    public async Task<IActionResult> DeleteConfiguration(string category, string key, [FromBody] DeleteConfigurationDto? request)
    {
        var command = new DeleteConfigurationCommand
        {
            Category = category,
            Key = key,
            Reason = request?.Reason ?? "Configuration deleted by administrator",
            DeletedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        
        if (!result)
        {
            return NotFoundResponse($"Configuration {category}.{key}");
        }

        return SuccessResponse("Configuration deleted successfully");
    }    /// <summary>
    /// Gets configuration history/audit trail
    /// </summary>
    [HttpGet("{category}/{key}/history")]
    [Authorize(Policy = Permissions.Configuration.ViewHistory)]
    public async Task<IActionResult> GetConfigurationHistory(string category, string key, [FromQuery] int pageSize = 50)
    {
        var query = new GetConfigurationHistoryQuery 
        { 
            Category = category, 
            Key = key, 
            PageSize = pageSize 
        };
        
        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Searches configurations
    /// </summary>
    [HttpGet("search")]
    [Authorize(Policy = Permissions.Configuration.Read)]
    public async Task<IActionResult> SearchConfigurations([FromQuery] string searchTerm, [FromQuery] string? category = null)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequestResponse("Search term is required");
        }

        var query = new SearchConfigurationsQuery 
        { 
            SearchTerm = searchTerm, 
            Category = category 
        };
        
        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Validates a configuration value without saving
    /// </summary>
    [HttpPost("{category}/{key}/validate")]
    [Authorize(Policy = Permissions.Configuration.Read)]
    public async Task<IActionResult> ValidateConfiguration(string category, string key, [FromBody] ValidateConfigurationDto request)
    {
        var query = new ValidateConfigurationQuery 
        { 
            Category = category, 
            Key = key, 
            Value = request.Value 
        };
        
        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Invalidates configuration cache
    /// </summary>
    [HttpPost("cache/invalidate")]
    [Authorize(Policy = Permissions.Configuration.InvalidateCache)]
    public async Task<IActionResult> InvalidateCache([FromQuery] string? category = null)
    {
        var command = new InvalidateConfigurationCacheCommand 
        { 
            Category = category,
            InvalidatedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };
        
        var result = await _mediator.Send(command);
        return SuccessResponse(result.Message);
    }
}