using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Configuration;

namespace VisitorManagementSystem.Api.Application.Queries.Configuration;

/// <summary>
/// Query to get all configurations grouped by category
/// </summary>
public class GetAllConfigurationsQuery : IRequest<Dictionary<string, List<ConfigurationDto>>>
{
}

/// <summary>
/// Query to get configurations for a specific category
/// </summary>
public class GetCategoryConfigurationQuery : IRequest<List<ConfigurationDto>>
{
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Query to get a specific configuration
/// </summary>
public class GetConfigurationQuery : IRequest<ConfigurationDto?>
{
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}

/// <summary>
/// Query to get configuration history
/// </summary>
public class GetConfigurationHistoryQuery : IRequest<List<ConfigurationHistoryDto>>
{
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Query to search configurations
/// </summary>
public class SearchConfigurationsQuery : IRequest<List<ConfigurationDto>>
{
    public string SearchTerm { get; set; } = string.Empty;
    public string? Category { get; set; }
}

/// <summary>
/// Query to validate a configuration value
/// </summary>
public class ValidateConfigurationQuery : IRequest<ConfigurationValidationResultDto>
{
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}