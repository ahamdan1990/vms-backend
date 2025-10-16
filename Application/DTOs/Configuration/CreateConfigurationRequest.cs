namespace VisitorManagementSystem.Api.Application.DTOs.Configuration
{
    /// <summary>
    /// Request DTO for creating configuration
    /// </summary>
    public class CreateConfigurationRequest
    {
        public string Category { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
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

}
