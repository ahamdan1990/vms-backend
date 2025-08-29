namespace VisitorManagementSystem.Api.Application.DTOs.Configuration
{
    /// <summary>
    /// Request DTO for updating configuration
    /// </summary>
    public class UpdateConfigurationRequest
    {
        public string Value { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

}
