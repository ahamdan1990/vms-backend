using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Audit;
using VisitorManagementSystem.Api.Application.DTOs.Common;

namespace VisitorManagementSystem.Api.Application.Queries.Audit;

/// <summary>
/// Query to get audit logs with filtering and pagination
/// </summary>
public class GetAuditLogsQuery : IRequest<PagedResultDto<AuditLogDto>>
{
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public int? UserId { get; set; }
    public string? Action { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Severity { get; set; }
    public string SortBy { get; set; } = "CreatedOn";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Query to get a specific audit log by ID
/// </summary>
public class GetAuditLogByIdQuery : IRequest<AuditLogDetailDto?>
{
    public int Id { get; set; }
}

/// <summary>
/// Query to get user activity logs
/// </summary>
public class GetUserActivityQuery : IRequest<PagedResultDto<AuditLogDto>>
{
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public int? UserId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Action { get; set; }
}/// <summary>
/// Query to get system events logs
/// </summary>
public class GetSystemEventsQuery : IRequest<PagedResultDto<AuditLogDto>>
{
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public string? Severity { get; set; }
    public string? Source { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

/// <summary>
/// Query to get security events logs
/// </summary>
public class GetSecurityEventsQuery : IRequest<PagedResultDto<AuditLogDto>>
{
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public string? EventType { get; set; }
    public string? IpAddress { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? RiskLevel { get; set; }
}

/// <summary>
/// Query to search audit logs
/// </summary>
public class SearchAuditLogsQuery : IRequest<PagedResultDto<AuditLogDto>>
{
    public string SearchTerm { get; set; } = string.Empty;
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public string? Category { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}