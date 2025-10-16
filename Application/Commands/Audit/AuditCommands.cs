using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Audit;

namespace VisitorManagementSystem.Api.Application.Commands.Audit;

/// <summary>
/// Command to export audit logs
/// </summary>
public class ExportAuditLogsCommand : IRequest<ExportAuditLogsResultDto>
{
    public string Format { get; set; } = "csv";
    public string? Category { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool IncludeDetails { get; set; }
    public int MaxRecords { get; set; } = 10000;
    public int ExportedBy { get; set; }
}

/// <summary>
/// Command to mark audit logs as reviewed
/// </summary>
public class MarkAuditLogsReviewedCommand : IRequest<MarkReviewedResultDto>
{
    public List<int> AuditLogIds { get; set; } = new();
    public string? ReviewComments { get; set; }
    public int ReviewedBy { get; set; }
}