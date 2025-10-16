namespace VisitorManagementSystem.Api.Application.DTOs.Audit;

/// <summary>
/// DTO for audit log data
/// </summary>
public class AuditLogDto
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Severity { get; set; }
    public bool RequiresAttention { get; set; }
    public bool IsReviewed { get; set; }
    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedOn { get; set; }
    public string? ReviewComments { get; set; }
    public string? HttpMethod { get; set; }
    public string? RequestPath { get; set; }
    public int? ResponseStatusCode { get; set; }
    public long? Duration { get; set; }
    public DateTime CreatedOn { get; set; }
}

/// <summary>
/// DTO for detailed audit log data
/// </summary>
public class AuditLogDetailDto : AuditLogDto
{
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Metadata { get; set; }
    public string? CorrelationId { get; set; }
    public string? SessionId { get; set; }
    public string? RequestId { get; set; }
    public long? RequestSize { get; set; }
    public long? ResponseSize { get; set; }
    public string? ExceptionDetails { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public int? ModifiedBy { get; set; }
}