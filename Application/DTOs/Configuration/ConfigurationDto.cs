using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Configuration;

/// <summary>
/// DTO for configuration data
/// </summary>
public class ConfigurationDto
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string DataType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsEncrypted { get; set; }
    public bool IsSensitive { get; set; }
    public bool RequiresRestart { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
    public string? Group { get; set; }
    public string? Environment { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime CreatedOn { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public int? ModifiedBy { get; set; }
}

/// <summary>
/// DTO for creating a configuration
/// </summary>
public class CreateConfigurationDto
{
    [Required]
    public string Category { get; set; } = string.Empty;
    
    [Required]
    public string Key { get; set; } = string.Empty;
    
    [Required]
    public string Value { get; set; } = string.Empty;
    
    [Required]
    public string DataType { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    public bool RequiresRestart { get; set; } = false;
    public bool IsEncrypted { get; set; } = false;
    public bool IsSensitive { get; set; } = false;
    public bool IsReadOnly { get; set; } = false;
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
    public string? Group { get; set; }
    public string? Environment { get; set; }
    public int DisplayOrder { get; set; } = 0;
}