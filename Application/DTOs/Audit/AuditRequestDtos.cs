using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Audit;

/// <summary>
/// DTO for marking audit logs as reviewed
/// </summary>
public class MarkAuditLogsReviewedDto
{
    [Required]
    public List<int> AuditLogIds { get; set; } = new();
    
    [MaxLength(1000)]
    public string? ReviewComments { get; set; }
}

/// <summary>
/// DTO for export audit logs result
/// </summary>
public class ExportAuditLogsResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public int RecordCount { get; set; }
    public DateTime ExportedAt { get; set; }
    public int ExportedBy { get; set; }
}

/// <summary>
/// DTO for mark reviewed result
/// </summary>
public class MarkReviewedResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int UpdatedCount { get; set; }
    public int ReviewedCount { get; set; }
    public int ReviewedBy { get; set; }
    public DateTime ReviewedOn { get; set; }
    public List<int> FailedIds { get; set; } = new();
    public string? Message { get; set; }
}