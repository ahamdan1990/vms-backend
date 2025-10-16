using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using VisitorManagementSystem.Api.Application.Queries.Audit;
using VisitorManagementSystem.Api.Application.Commands.Audit;
using VisitorManagementSystem.Api.Application.DTOs.Audit;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Controllers.Admin;

/// <summary>
/// Admin controller for managing audit logs and system activity tracking
/// </summary>
[ApiController]
[Route("api/audit")]
[Authorize(Roles = "Administrator")]
public class AuditController : BaseController
{
    private readonly IMediator _mediator;

    public AuditController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets paginated audit logs with filtering and sorting
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Permissions.Audit.ReadAll)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? category = null,
        [FromQuery] int? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? severity = null,
        [FromQuery] string sortBy = "CreatedOn",
        [FromQuery] bool sortDescending = true)
    {
        var query = new GetAuditLogsQuery
        {
            PageIndex = pageIndex,
            PageSize = Math.Min(pageSize, 100),
            SearchTerm = searchTerm,
            Category = category,
            UserId = userId,
            Action = action,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Severity = severity,
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets a specific audit log entry
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.Audit.ReadAll)]
    public async Task<IActionResult> GetAuditLog(int id)
    {
        var query = new GetAuditLogByIdQuery { Id = id };
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFoundResponse("Audit log", id);
        }

        return SuccessResponse(result);
    }    /// <summary>
    /// Gets user activity audit logs
    /// </summary>
    [HttpGet("user-activity")]
    [Authorize(Policy = Permissions.Audit.ViewUserActivity)]
    public async Task<IActionResult> GetUserActivity(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? userId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? action = null)
    {
        var query = new GetUserActivityQuery
        {
            PageIndex = pageIndex,
            PageSize = Math.Min(pageSize, 100),
            UserId = userId,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Action = action
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets system events audit logs
    /// </summary>
    [HttpGet("system-events")]
    [Authorize(Policy = Permissions.Audit.ViewSystemEvents)]
    public async Task<IActionResult> GetSystemEvents(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? severity = null,
        [FromQuery] string? source = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        var query = new GetSystemEventsQuery
        {
            PageIndex = pageIndex,
            PageSize = Math.Min(pageSize, 100),
            Severity = severity,
            Source = source,
            DateFrom = dateFrom,
            DateTo = dateTo
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets security events audit logs
    /// </summary>
    [HttpGet("security-events")]
    [Authorize(Policy = Permissions.Audit.ViewSecurityEvents)]
    public async Task<IActionResult> GetSecurityEvents(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? eventType = null,
        [FromQuery] string? ipAddress = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? riskLevel = null)
    {
        var query = new GetSecurityEventsQuery
        {
            PageIndex = pageIndex,
            PageSize = Math.Min(pageSize, 100),
            EventType = eventType,
            IpAddress = ipAddress,
            DateFrom = dateFrom,
            DateTo = dateTo,
            RiskLevel = riskLevel
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }    /// <summary>
    /// Searches audit logs
    /// </summary>
    [HttpGet("search")]
    [Authorize(Policy = Permissions.Audit.Search)]
    public async Task<IActionResult> SearchAuditLogs(
        [FromQuery] string searchTerm,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? category = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequestResponse("Search term is required");
        }

        var query = new SearchAuditLogsQuery
        {
            SearchTerm = searchTerm,
            PageIndex = pageIndex,
            PageSize = Math.Min(pageSize, 100),
            Category = category,
            DateFrom = dateFrom,
            DateTo = dateTo
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Exports audit logs to CSV or Excel
    /// </summary>
    [HttpGet("export")]
    [Authorize(Policy = Permissions.Audit.Export)]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] string format = "csv",
        [FromQuery] string? category = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] bool includeDetails = false,
        [FromQuery] int maxRecords = 10000)
    {
        var command = new ExportAuditLogsCommand
        {
            Format = format,
            Category = category,
            DateFrom = dateFrom,
            DateTo = dateTo,
            IncludeDetails = includeDetails,
            MaxRecords = maxRecords,
            ExportedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        
        var contentType = format.ToLower() switch
        {
            "xlsx" or "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "text/csv"
        };

        var fileName = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{format.ToLower()}";
        
        return File(result.FileContent, contentType, fileName);
    }

    /// <summary>
    /// Marks audit logs as reviewed
    /// </summary>
    [HttpPost("mark-reviewed")]
    [Authorize(Policy = Permissions.Audit.Review)]
    public async Task<IActionResult> MarkAuditLogsAsReviewed([FromBody] MarkAuditLogsReviewedDto dto)
    {
        var command = new MarkAuditLogsReviewedCommand
        {
            AuditLogIds = dto.AuditLogIds,
            ReviewComments = dto.ReviewComments,
            ReviewedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result, $"Marked {result.UpdatedCount} audit logs as reviewed");
    }
}