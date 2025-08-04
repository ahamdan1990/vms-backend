using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// System configuration entity for storing dynamic settings
/// </summary>
public class SystemConfiguration : AuditableEntity
{
    /// <summary>
    /// Configuration category (Security, JWT, Lockout, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Configuration key
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Configuration value (JSON for complex objects)
    /// </summary>
    [Required]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Data type of the value
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this setting does
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this setting requires application restart
    /// </summary>
    public bool RequiresRestart { get; set; } = false;

    /// <summary>
    /// Whether this setting is encrypted
    /// </summary>
    public bool IsEncrypted { get; set; } = false;

    /// <summary>
    /// Whether this setting is read-only
    /// </summary>
    public bool IsReadOnly { get; set; } = false;

    /// <summary>
    /// Whether this setting is sensitive (password, secret key, etc.)
    /// </summary>
    public bool IsSensitive { get; set; } = false;

    /// <summary>
    /// Default value for this setting
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Validation rules (JSON)
    /// </summary>
    public string? ValidationRules { get; set; }

    /// <summary>
    /// Minimum allowed value (for numeric types)
    /// </summary>
    public string? MinValue { get; set; }

    /// <summary>
    /// Maximum allowed value (for numeric types)
    /// </summary>
    public string? MaxValue { get; set; }

    /// <summary>
    /// Allowed values (JSON array for enum-like values)
    /// </summary>
    public string? AllowedValues { get; set; }

    /// <summary>
    /// Configuration group for UI organization
    /// </summary>
    [MaxLength(100)]
    public string? Group { get; set; }

    /// <summary>
    /// Display order within the group
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Environment where this configuration applies (All, Development, Production, etc.)
    /// </summary>
    [MaxLength(50)]
    public string Environment { get; set; } = "All";

    /// <summary>
    /// Navigation property for configuration audit entries
    /// </summary>
    public virtual ICollection<ConfigurationAudit> AuditEntries { get; set; } = new List<ConfigurationAudit>();

    /// <summary>
    /// Composite key for category and key
    /// </summary>
    public string CompositeKey => $"{Category}.{Key}";

    /// <summary>
    /// Checks if the configuration value is valid based on validation rules
    /// </summary>
    /// <param name="value">Value to validate</param>
    /// <returns>True if valid</returns>
    public bool IsValidValue(string value)
    {
        try
        {
            // Basic type validation
            switch (DataType.ToLower())
            {
                case "int":
                case "integer":
                    if (!int.TryParse(value, out var intVal))
                        return false;
                    if (!string.IsNullOrEmpty(MinValue) && int.TryParse(MinValue, out var minInt) && intVal < minInt)
                        return false;
                    if (!string.IsNullOrEmpty(MaxValue) && int.TryParse(MaxValue, out var maxInt) && intVal > maxInt)
                        return false;
                    break;

                case "bool":
                case "boolean":
                    if (!bool.TryParse(value, out _))
                        return false;
                    break;

                case "timespan":
                    if (!TimeSpan.TryParse(value, out _))
                        return false;
                    break;

                case "datetime":
                    if (!DateTime.TryParse(value, out _))
                        return false;
                    break;

                case "string":
                    if (!string.IsNullOrEmpty(MinValue) && int.TryParse(MinValue, out var minLen) && value.Length < minLen)
                        return false;
                    if (!string.IsNullOrEmpty(MaxValue) && int.TryParse(MaxValue, out var maxLen) && value.Length > maxLen)
                        return false;
                    break;
            }

            // Check allowed values if specified
            if (!string.IsNullOrEmpty(AllowedValues))
            {
                var allowedList = System.Text.Json.JsonSerializer.Deserialize<string[]>(AllowedValues);
                if (allowedList != null && !allowedList.Contains(value, StringComparer.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Updates the configuration value with validation
    /// </summary>
    /// <param name="newValue">New value</param>
    /// <param name="modifiedBy">User making the change</param>
    /// <returns>True if updated successfully</returns>
    public bool UpdateValue(string newValue, int modifiedBy)
    {
        if (IsReadOnly)
            throw new InvalidOperationException("Cannot update read-only configuration");

        if (!IsValidValue(newValue))
            throw new ArgumentException("Invalid configuration value");

        Value = newValue;
        UpdateModifiedBy(modifiedBy);
        return true;
    }

    /// <summary>
    /// Gets the display name for this configuration
    /// </summary>
    public string DisplayName => $"{Category} - {Key}";

    /// <summary>
    /// Gets help text for this configuration
    /// </summary>
    public string HelpText => Description ?? $"Configuration setting for {Key}";
}
