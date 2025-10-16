using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Configuration;

/// <summary>
/// DTO for updating a configuration
/// </summary>
public class UpdateConfigurationDto
{
    [Required]
    public string Value { get; set; } = string.Empty;
    
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for deleting a configuration
/// </summary>
public class DeleteConfigurationDto
{
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for validating a configuration
/// </summary>
public class ValidateConfigurationDto
{
    [Required]
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// DTO for configuration validation result
/// </summary>
public class ConfigurationValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// DTO for configuration update result
/// </summary>
public class ConfigurationUpdateResultDto
{
    public bool Success { get; set; }
    public bool RequiresRestart { get; set; }
    public string? RestartWarning { get; set; }
    public string? Message { get; set; }
    public DateTime UpdatedOn { get; set; }
    public int UpdatedBy { get; set; }
}

/// <summary>
/// DTO for cache invalidation result
/// </summary>
public class CacheInvalidationResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime InvalidatedAt { get; set; }
    public int InvalidatedBy { get; set; }
    public string Scope { get; set; } = string.Empty;
}