using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Configuration;

namespace VisitorManagementSystem.Api.Application.Commands.Configuration;

/// <summary>
/// Command to create a new configuration
/// </summary>
public class CreateConfigurationCommand : IRequest<ConfigurationDto>
{
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool RequiresRestart { get; set; }
    public bool IsEncrypted { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsReadOnly { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
    public string? Group { get; set; }
    public string? Environment { get; set; }
    public int DisplayOrder { get; set; }
    public int CreatedBy { get; set; }
}

/// <summary>
/// Command to update a configuration
/// </summary>
public class UpdateConfigurationCommand : IRequest<ConfigurationUpdateResultDto>
{
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public int ModifiedBy { get; set; }
}

/// <summary>
/// Command to delete a configuration
/// </summary>
public class DeleteConfigurationCommand : IRequest<bool>
{
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public int DeletedBy { get; set; }
}

/// <summary>
/// Command to invalidate configuration cache
/// </summary>
public class InvalidateConfigurationCacheCommand : IRequest<CacheInvalidationResultDto>
{
    public string? Category { get; set; }
    public int InvalidatedBy { get; set; }
}