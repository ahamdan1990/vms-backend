using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Controllers.Admin;

/// <summary>
/// Admin controller for managing audit logs and system activity tracking
/// </summary>
[ApiController]
[Route("api/Audit")]
[Authorize(Roles = "Administrator")]
public class AuditController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IUnitOfWork unitOfWork, ILogger<AuditController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
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
        [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build a combined predicate based on all filters
            var predicate = BuildAuditLogPredicate(searchTerm, category, userId, action, dateFrom, dateTo, severity);

            // Get paginated results
            var (auditLogs, totalCount) = await _unitOfWork.AuditLogs.GetPagedAsync<DateTime>(
                pageIndex, pageSize, predicate, 
                sortDescending ? (a => a.CreatedOn) : (a => a.CreatedOn), 
                cancellationToken);

            // Project to response format
            var projectedLogs = auditLogs.Select(a => new
            {
                id = a.Id,
                timestamp = a.CreatedOn,
                eventType = a.EventType.ToString(),
                category = a.EventType.GetCategory(),
                entityName = a.EntityName,
                entityId = a.EntityId,
                action = a.Action,
                description = a.Description,
                ipAddress = a.IpAddress,
                userId = a.UserId,
                userName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}".Trim() : null,
                isSuccess = a.IsSuccess,
                errorMessage = a.ErrorMessage,
                severity = a.RiskLevel,
                requiresAttention = a.RequiresAttention,
                isReviewed = a.IsReviewed,
                httpMethod = a.HttpMethod,
                requestPath = a.RequestPath,
                responseStatusCode = a.ResponseStatusCode,
                duration = a.Duration
            }).ToList();

            var response = new
            {
                success = true,
                data = new
                {
                    items = projectedLogs,
                    totalCount = totalCount,
                    pageIndex = pageIndex,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(500, new { success = false, message = "Failed to retrieve audit logs" });
        }
    }

    /// <summary>
    /// Gets a specific audit log entry
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = Permissions.Audit.ReadAll)]
    public async Task<IActionResult> GetAuditLog(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = await _unitOfWork.AuditLogs.GetByIdAsync(id, a => a.User!);

            if (auditLog == null)
            {
                return NotFound(new { success = false, message = "Audit log not found" });
            }

            var projectedLog = new
            {
                id = auditLog.Id,
                timestamp = auditLog.CreatedOn,
                eventType = auditLog.EventType.ToString(),
                category = auditLog.EventType.GetCategory(),
                entityName = auditLog.EntityName,
                entityId = auditLog.EntityId,
                action = auditLog.Action,
                description = auditLog.Description,
                oldValues = auditLog.OldValues,
                newValues = auditLog.NewValues,
                metadata = auditLog.Metadata,
                ipAddress = auditLog.IpAddress,
                userAgent = auditLog.UserAgent,
                correlationId = auditLog.CorrelationId,
                sessionId = auditLog.SessionId,
                requestId = auditLog.RequestId,
                httpMethod = auditLog.HttpMethod,
                requestPath = auditLog.RequestPath,
                responseStatusCode = auditLog.ResponseStatusCode,
                duration = auditLog.Duration,
                requestSize = auditLog.RequestSize,
                responseSize = auditLog.ResponseSize,
                userId = auditLog.UserId,
                userName = auditLog.User != null ? $"{auditLog.User.FirstName} {auditLog.User.LastName}".Trim() : null,
                userEmail = auditLog.User?.Email,
                isSuccess = auditLog.IsSuccess,
                errorMessage = auditLog.ErrorMessage,
                exceptionDetails = auditLog.ExceptionDetails,
                severity = auditLog.RiskLevel,
                requiresAttention = auditLog.RequiresAttention,
                isReviewed = auditLog.IsReviewed,
                reviewedBy = auditLog.ReviewedBy,
                reviewedDate = auditLog.ReviewedDate,
                reviewComments = auditLog.ReviewComments
            };

            return Ok(new { success = true, data = projectedLog });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit log {Id}", id);
            return StatusCode(500, new { success = false, message = "Failed to retrieve audit log" });
        }
    }
    /// <summary>
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
        [FromQuery] string? action = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // User activity includes Authentication, Authorization, and UserManagement events
            var userActivityEventTypes = new[]
            {
                EventType.UserManagement
            };

            var predicate = BuildUserActivityPredicate(userActivityEventTypes, userId, dateFrom, dateTo, action);

            var (auditLogs, totalCount) = await _unitOfWork.AuditLogs.GetPagedAsync<DateTime>(
                pageIndex, pageSize, predicate, 
                (a => a.CreatedOn), cancellationToken);

            var projectedLogs = auditLogs.Select(a => new
            {
                id = a.Id,
                timestamp = a.CreatedOn,
                eventType = a.EventType.ToString(),
                category = a.EventType.GetCategory(),
                action = a.Action,
                description = a.Description,
                userId = a.UserId,
                userName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}".Trim() : null,
                ipAddress = a.IpAddress,
                userAgent = a.UserAgent,
                isSuccess = a.IsSuccess,
                errorMessage = a.ErrorMessage,
                severity = a.RiskLevel,
                requiresAttention = a.RequiresAttention
            }).ToList();

            var response = new
            {
                success = true,
                data = new
                {
                    items = projectedLogs,
                    totalCount = totalCount,
                    pageIndex = pageIndex,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user activity logs");
            return StatusCode(500, new { success = false, message = "Failed to retrieve user activity logs" });
        }
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
        [FromQuery] DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // System events include non-security related system activities
            var systemEventTypes = new[]
            {
                EventType.SystemConfiguration,
                EventType.SystemMaintenance,
                EventType.Performance,
                EventType.Error,
                EventType.Integration,
                EventType.ApiAccess,
                EventType.General
            };

            var predicate = BuildSystemEventsPredicate(systemEventTypes, severity, source, dateFrom, dateTo);

            var (auditLogs, totalCount) = await _unitOfWork.AuditLogs.GetPagedAsync<DateTime>(
                pageIndex, pageSize, predicate,
                (a => a.CreatedOn), cancellationToken);

            var projectedLogs = auditLogs.Select(a => new
            {
                id = a.Id,
                timestamp = a.CreatedOn,
                eventType = a.EventType.ToString(),
                category = a.EventType.GetCategory(),
                entityName = a.EntityName,
                action = a.Action,
                description = a.Description,
                userId = a.UserId,
                userName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}".Trim() : null,
                isSuccess = a.IsSuccess,
                errorMessage = a.ErrorMessage,
                exceptionDetails = a.ExceptionDetails,
                severity = a.RiskLevel,
                requiresAttention = a.RequiresAttention,
                duration = a.Duration,
                responseStatusCode = a.ResponseStatusCode
            }).ToList();

            var response = new
            {
                success = true,
                data = new
                {
                    items = projectedLogs,
                    totalCount = totalCount,
                    pageIndex = pageIndex,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system events");
            return StatusCode(500, new { success = false, message = "Failed to retrieve system events" });
        }
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
        [FromQuery] string? riskLevel = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all security-related event types
            var securityEventTypes = EventTypeExtensions.GetSecurityEventTypes();

            var predicate = BuildSecurityEventsPredicate(securityEventTypes, eventType, ipAddress, dateFrom, dateTo, riskLevel);

            var (auditLogs, totalCount) = await _unitOfWork.AuditLogs.GetPagedAsync<DateTime>(
                pageIndex, pageSize, predicate,
                (a => a.CreatedOn), cancellationToken);

            var projectedLogs = auditLogs.Select(a => new
            {
                id = a.Id,
                timestamp = a.CreatedOn,
                eventType = a.EventType.ToString(),
                category = a.EventType.GetCategory(),
                action = a.Action,
                description = a.Description,
                userId = a.UserId,
                userName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}".Trim() : null,
                ipAddress = a.IpAddress,
                userAgent = a.UserAgent,
                isSuccess = a.IsSuccess,
                errorMessage = a.ErrorMessage,
                severity = a.RiskLevel,
                requiresAttention = a.RequiresAttention,
                isReviewed = a.IsReviewed,
                correlationId = a.CorrelationId,
                sessionId = a.SessionId
            }).ToList();

            var response = new
            {
                success = true,
                data = new
                {
                    items = projectedLogs,
                    totalCount = totalCount,
                    pageIndex = pageIndex,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security events");
            return StatusCode(500, new { success = false, message = "Failed to retrieve security events" });
        }
    }

    /// <summary>
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
        [FromQuery] DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest(new { success = false, message = "Search term is required" });
            }

            var predicate = BuildSearchPredicate(searchTerm, category, dateFrom, dateTo);

            var (auditLogs, totalCount) = await _unitOfWork.AuditLogs.GetPagedAsync<DateTime>(
                pageIndex, pageSize, predicate,
                (a => a.CreatedOn), cancellationToken);

            var projectedLogs = auditLogs.Select(a => new
            {
                id = a.Id,
                timestamp = a.CreatedOn,
                eventType = a.EventType.ToString(),
                category = a.EventType.GetCategory(),
                entityName = a.EntityName,
                action = a.Action,
                description = a.Description,
                userId = a.UserId,
                userName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}".Trim() : null,
                ipAddress = a.IpAddress,
                isSuccess = a.IsSuccess,
                errorMessage = a.ErrorMessage,
                severity = a.RiskLevel,
                requiresAttention = a.RequiresAttention
            }).ToList();

            var response = new
            {
                success = true,
                data = new
                {
                    items = projectedLogs,
                    totalCount = totalCount,
                    pageIndex = pageIndex,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    searchTerm = searchTerm,
                    category = category
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching audit logs with term: {SearchTerm}", searchTerm);
            return StatusCode(500, new { success = false, message = "Failed to search audit logs" });
        }
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
        [FromQuery] int maxRecords = 10000,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var predicate = BuildExportPredicate(category, dateFrom, dateTo);

            // Get all matching records (up to maxRecords)
            var auditLogs = await _unitOfWork.AuditLogs.GetAsync(predicate, cancellationToken);
            
            // Take only the requested number of records and order by date descending
            var limitedLogs = auditLogs
                .OrderByDescending(a => a.CreatedOn)
                .Take(maxRecords)
                .ToList();

            // Generate appropriate file based on format
            var fileName = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            var contentType = "";
            byte[] fileContent;

            switch (format.ToLower())
            {
                case "csv":
                    contentType = "text/csv";
                    fileName += ".csv";
                    fileContent = GenerateCsvContent(limitedLogs, includeDetails);
                    break;
                case "xlsx":
                case "excel":
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    fileName += ".xlsx";
                    // For now, return CSV content - in a real implementation you'd generate Excel
                    fileContent = GenerateCsvContent(limitedLogs, includeDetails);
                    break;
                default:
                    return BadRequest(new { success = false, message = "Unsupported export format" });
            }

            return File(fileContent, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs");
            return StatusCode(500, new { success = false, message = "Failed to export audit logs" });
        }
    }

    #region Helper Methods

    /// <summary>
    /// Gets current user ID from claims
    /// </summary>
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim?.Value, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Builds predicate for general audit log filtering
    /// </summary>
    private System.Linq.Expressions.Expression<Func<AuditLog, bool>> BuildAuditLogPredicate(
        string? searchTerm, string? category, int? userId, string? action, 
        DateTime? dateFrom, DateTime? dateTo, string? severity)
    {
        return auditLog =>
            (string.IsNullOrEmpty(searchTerm) || 
             auditLog.EntityName.Contains(searchTerm) ||
             auditLog.Action.Contains(searchTerm) ||
             (auditLog.Description != null && auditLog.Description.Contains(searchTerm))) &&

            (string.IsNullOrEmpty(category) || 
             GetEventTypesForCategory(category).Contains(auditLog.EventType)) &&

            (!userId.HasValue || auditLog.UserId == userId) &&

            (string.IsNullOrEmpty(action) || auditLog.Action.Contains(action)) &&

            (!dateFrom.HasValue || auditLog.CreatedOn >= dateFrom.Value) &&

            (!dateTo.HasValue || auditLog.CreatedOn <= dateTo.Value) &&

            (string.IsNullOrEmpty(severity) || auditLog.RiskLevel == severity);
    }

    /// <summary>
    /// Builds predicate for user activity filtering
    /// </summary>
    private System.Linq.Expressions.Expression<Func<AuditLog, bool>> BuildUserActivityPredicate(
        EventType[] userActivityEventTypes, int? userId, DateTime? dateFrom, DateTime? dateTo, string? action)
    {
        return auditLog =>
            userActivityEventTypes.Contains(auditLog.EventType) &&
            (!userId.HasValue || auditLog.UserId == userId) &&
            (string.IsNullOrEmpty(action) || auditLog.Action.Contains(action)) &&
            (!dateFrom.HasValue || auditLog.CreatedOn >= dateFrom.Value) &&
            (!dateTo.HasValue || auditLog.CreatedOn <= dateTo.Value);
    }

    /// <summary>
    /// Builds predicate for system events filtering
    /// </summary>
    private System.Linq.Expressions.Expression<Func<AuditLog, bool>> BuildSystemEventsPredicate(
        EventType[] systemEventTypes, string? severity, string? source, DateTime? dateFrom, DateTime? dateTo)
    {
        return auditLog =>
            systemEventTypes.Contains(auditLog.EventType) &&
            (string.IsNullOrEmpty(severity) || auditLog.RiskLevel == severity) &&
            (string.IsNullOrEmpty(source) || auditLog.EntityName.Contains(source)) &&
            (!dateFrom.HasValue || auditLog.CreatedOn >= dateFrom.Value) &&
            (!dateTo.HasValue || auditLog.CreatedOn <= dateTo.Value);
    }
    /// <summary>
    /// Builds predicate for security events filtering
    /// </summary>
    private System.Linq.Expressions.Expression<Func<AuditLog, bool>> BuildSecurityEventsPredicate(
        List<EventType> securityEventTypes, string? eventType, string? ipAddress, 
        DateTime? dateFrom, DateTime? dateTo, string? riskLevel)
    {
        EventType? specificEventType = null;
        if (!string.IsNullOrEmpty(eventType) && Enum.TryParse<EventType>(eventType, true, out var et))
        {
            specificEventType = et;
        }

        return auditLog =>
            securityEventTypes.Contains(auditLog.EventType) &&
            (!specificEventType.HasValue || auditLog.EventType == specificEventType.Value) &&
            (string.IsNullOrEmpty(ipAddress) || (auditLog.IpAddress != null && auditLog.IpAddress.Contains(ipAddress))) &&
            (!dateFrom.HasValue || auditLog.CreatedOn >= dateFrom.Value) &&
            (!dateTo.HasValue || auditLog.CreatedOn <= dateTo.Value) &&
            (string.IsNullOrEmpty(riskLevel) || auditLog.RiskLevel == riskLevel);
    }

    /// <summary>
    /// Builds predicate for search filtering
    /// </summary>
    private System.Linq.Expressions.Expression<Func<AuditLog, bool>> BuildSearchPredicate(
        string searchTerm, string? category, DateTime? dateFrom, DateTime? dateTo)
    {
        return auditLog =>
            (auditLog.EntityName.Contains(searchTerm) ||
             auditLog.Action.Contains(searchTerm) ||
             (auditLog.Description != null && auditLog.Description.Contains(searchTerm)) ||
             (auditLog.User != null && 
              (auditLog.User.FirstName.Contains(searchTerm) || 
               auditLog.User.LastName.Contains(searchTerm) || 
               (auditLog.User.Email != null && auditLog.User.Email.Value.Contains(searchTerm)))) ||
             (auditLog.IpAddress != null && auditLog.IpAddress.Contains(searchTerm)) ||
             (auditLog.RequestPath != null && auditLog.RequestPath.Contains(searchTerm))) &&

            (string.IsNullOrEmpty(category) || 
             GetEventTypesForCategory(category).Contains(auditLog.EventType)) &&

            (!dateFrom.HasValue || auditLog.CreatedOn >= dateFrom.Value) &&

            (!dateTo.HasValue || auditLog.CreatedOn <= dateTo.Value);
    }

    /// <summary>
    /// Builds predicate for export filtering
    /// </summary>
    private System.Linq.Expressions.Expression<Func<AuditLog, bool>> BuildExportPredicate(
        string? category, DateTime? dateFrom, DateTime? dateTo)
    {
        return auditLog =>
            (string.IsNullOrEmpty(category) || 
             GetEventTypesForCategory(category).Contains(auditLog.EventType)) &&
            (!dateFrom.HasValue || auditLog.CreatedOn >= dateFrom.Value) &&
            (!dateTo.HasValue || auditLog.CreatedOn <= dateTo.Value);
    }

    /// <summary>
    /// Gets event types for a specific category
    /// </summary>
    private List<EventType> GetEventTypesForCategory(string category)
    {
        if (Enum.TryParse<EventType>(category, true, out var eventType))
        {
            return new List<EventType> { eventType };
        }

        return EventTypeExtensions.GetEventTypesByCategory(category);
    }

    /// <summary>
    /// Generates CSV content from audit log data
    /// </summary>
    private byte[] GenerateCsvContent(List<AuditLog> auditLogs, bool includeDetails)
    {
        var csv = new StringBuilder();
        
        // CSV Header
        if (includeDetails)
        {
            csv.AppendLine("Timestamp,EventType,Category,EntityName,EntityId,Action,Description,UserName,UserEmail,IpAddress,UserAgent,IsSuccess,ErrorMessage,Severity,RequiresAttention,IsReviewed,HttpMethod,RequestPath,ResponseStatusCode,Duration,OldValues,NewValues,Metadata,ExceptionDetails");
        }
        else
        {
            csv.AppendLine("Timestamp,EventType,Category,EntityName,EntityId,Action,Description,UserName,UserEmail,IpAddress,UserAgent,IsSuccess,ErrorMessage,Severity,RequiresAttention,IsReviewed,HttpMethod,RequestPath,ResponseStatusCode,Duration");
        }
        
        // CSV Data
        foreach (var log in auditLogs)
        {
            var userName = log.User != null ? $"{log.User.FirstName} {log.User.LastName}".Trim() : "";
            var userEmail = log.User?.Email ?? "";
            
            var basicRow = $"{log.CreatedOn:yyyy-MM-dd HH:mm:ss},{EscapeCsvValue(log.EventType.ToString())},{EscapeCsvValue(log.EventType.GetCategory())},{EscapeCsvValue(log.EntityName)},{log.EntityId},{EscapeCsvValue(log.Action)},{EscapeCsvValue(log.Description)},{EscapeCsvValue(userName)},{EscapeCsvValue(userEmail)},{EscapeCsvValue(log.IpAddress)},{EscapeCsvValue(log.UserAgent)},{log.IsSuccess},{EscapeCsvValue(log.ErrorMessage)},{EscapeCsvValue(log.RiskLevel)},{log.RequiresAttention},{log.IsReviewed},{EscapeCsvValue(log.HttpMethod)},{EscapeCsvValue(log.RequestPath)},{log.ResponseStatusCode},{log.Duration}";
            
            if (includeDetails)
            {
                basicRow += $",{EscapeCsvValue(log.OldValues)},{EscapeCsvValue(log.NewValues)},{EscapeCsvValue(log.Metadata)},{EscapeCsvValue(log.ExceptionDetails)}";
            }
            
            csv.AppendLine(basicRow);
        }
        
        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    /// <summary>
    /// Escapes CSV values to handle commas and quotes
    /// </summary>
    private string EscapeCsvValue(object? value)
    {
        if (value == null) return "";
        
        var stringValue = value.ToString() ?? "";
        
        if (stringValue.Contains(',') || stringValue.Contains('"') || stringValue.Contains('\n'))
        {
            return $"\"{stringValue.Replace("\"", "\"\"")}\"";
        }
        
        return stringValue;
    }

    #endregion
}