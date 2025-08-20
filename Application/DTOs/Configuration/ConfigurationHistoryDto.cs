namespace VisitorManagementSystem.Api.Application.DTOs.Configuration;

/// <summary>
/// DTO for configuration history entry
/// </summary>
public class ConfigurationHistoryDto
{
    public int Id { get; set; }
    public int ConfigurationId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public int UserId { get; set; }
    public int ChangedBy { get; set; }
    public string? UserName { get; set; }
    public string? ChangedByName { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime ChangedOn { get; set; }
}