using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents an audit log entry for tracking system activities
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>
    /// Type of event that occurred
    /// </summary>
    [Required]
    public EventType EventType { get; set; }

    /// <summary>
    /// Name of the entity that was affected
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// ID of the entity that was affected
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// Action that was performed (Create, Update, Delete, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Description of the action performed
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Old values before the change (JSON format)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// New values after the change (JSON format)
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// Additional metadata about the event (JSON format)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// IP address of the client that initiated the action
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the client
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Correlation ID for tracking related operations
    /// </summary>
    [MaxLength(50)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Session ID for tracking user sessions
    /// </summary>
    [MaxLength(50)]
    public string? SessionId { get; set; }

    /// <summary>
    /// Request ID for tracking individual requests
    /// </summary>
    [MaxLength(50)]
    public string? RequestId { get; set; }

    /// <summary>
    /// HTTP method used for the request
    /// </summary>
    [MaxLength(10)]
    public string? HttpMethod { get; set; }

    /// <summary>
    /// URL path of the request
    /// </summary>
    [MaxLength(500)]
    public string? RequestPath { get; set; }

    /// <summary>
    /// Status code of the response
    /// </summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>
    /// Duration of the request in milliseconds
    /// </summary>
    public long? Duration { get; set; }

    /// <summary>
    /// Size of the request in bytes
    /// </summary>
    public long? RequestSize { get; set; }

    /// <summary>
    /// Size of the response in bytes
    /// </summary>
    public long? ResponseSize { get; set; }

    /// <summary>
    /// Indicates if the action was successful
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// Error message if the action failed
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Exception details if an error occurred
    /// </summary>
    public string? ExceptionDetails { get; set; }

    /// <summary>
    /// ID of the user who performed the action
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Navigation property to the user who performed the action
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// Risk level of the action (Low, Medium, High, Critical)
    /// </summary>
    [MaxLength(20)]
    public string RiskLevel { get; set; } = "Low";

    /// <summary>
    /// Indicates if this entry requires attention
    /// </summary>
    public bool RequiresAttention { get; set; } = false;

    /// <summary>
    /// Indicates if this entry has been reviewed
    /// </summary>
    public bool IsReviewed { get; set; } = false;

    /// <summary>
    /// ID of the user who reviewed this entry
    /// </summary>
    public int? ReviewedBy { get; set; }

    /// <summary>
    /// Date when this entry was reviewed
    /// </summary>
    public DateTime? ReviewedDate { get; set; }

    /// <summary>
    /// Review comments
    /// </summary>
    [MaxLength(500)]
    public string? ReviewComments { get; set; }

    /// <summary>
    /// Creates an audit log entry for a successful action
    /// </summary>
    /// <param name="eventType">Type of event</param>
    /// <param name="entityName">Name of the affected entity</param>
    /// <param name="entityId">ID of the affected entity</param>
    /// <param name="action">Action performed</param>
    /// <param name="description">Description of the action</param>
    /// <param name="userId">ID of the user performing the action</param>
    /// <param name="ipAddress">IP address of the client</param>
    /// <param name="userAgent">User agent of the client</param>
    /// <returns>New audit log entry</returns>
    public static AuditLog CreateSuccessEntry(
        EventType eventType,
        string entityName,
        int? entityId,
        string action,
        string? description = null,
        int? userId = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        return new AuditLog
        {
            EventType = eventType,
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            Description = description,
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsSuccess = true,
            CreatedOn = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an audit log entry for a failed action
    /// </summary>
    /// <param name="eventType">Type of event</param>
    /// <param name="entityName">Name of the affected entity</param>
    /// <param name="action">Action attempted</param>
    /// <param name="errorMessage">Error message</param>
    /// <param name="exceptionDetails">Exception details</param>
    /// <param name="userId">ID of the user attempting the action</param>
    /// <param name="ipAddress">IP address of the client</param>
    /// <param name="userAgent">User agent of the client</param>
    /// <returns>New audit log entry</returns>
    public static AuditLog CreateFailureEntry(
        EventType eventType,
        string entityName,
        string action,
        string errorMessage,
        string? exceptionDetails = null,
        int? userId = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        return new AuditLog
        {
            EventType = eventType,
            EntityName = entityName,
            Action = action,
            ErrorMessage = errorMessage,
            ExceptionDetails = exceptionDetails,
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsSuccess = false,
            RiskLevel = "Medium",
            RequiresAttention = true,
            CreatedOn = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Marks the audit log entry as reviewed
    /// </summary>
    /// <param name="reviewedBy">ID of the user reviewing the entry</param>
    /// <param name="comments">Review comments</param>
    public void MarkAsReviewed(int reviewedBy, string? comments = null)
    {
        IsReviewed = true;
        ReviewedBy = reviewedBy;
        ReviewedDate = DateTime.UtcNow;
        ReviewComments = comments;
        RequiresAttention = false;
        UpdateModifiedOn();
    }

    /// <summary>
    /// Sets the risk level of the audit entry
    /// </summary>
    /// <param name="riskLevel">Risk level (Low, Medium, High, Critical)</param>
    public void SetRiskLevel(string riskLevel)
    {
        RiskLevel = riskLevel;

        // High and Critical risk levels require attention
        if (riskLevel == "High" || riskLevel == "Critical")
        {
            RequiresAttention = true;
        }

        UpdateModifiedOn();
    }

    /// <summary>
    /// Adds performance metrics to the audit log
    /// </summary>
    /// <param name="duration">Duration in milliseconds</param>
    /// <param name="requestSize">Request size in bytes</param>
    /// <param name="responseSize">Response size in bytes</param>
    public void AddPerformanceMetrics(long? duration = null, long? requestSize = null, long? responseSize = null)
    {
        Duration = duration;
        RequestSize = requestSize;
        ResponseSize = responseSize;
        UpdateModifiedOn();
    }

    /// <summary>
    /// Sets request information
    /// </summary>
    /// <param name="httpMethod">HTTP method</param>
    /// <param name="requestPath">Request path</param>
    /// <param name="responseStatusCode">Response status code</param>
    /// <param name="correlationId">Correlation ID</param>
    /// <param name="requestId">Request ID</param>
    /// <param name="sessionId">Session ID</param>
    public void SetRequestInfo(
        string? httpMethod = null,
        string? requestPath = null,
        int? responseStatusCode = null,
        string? correlationId = null,
        string? requestId = null,
        string? sessionId = null)
    {
        HttpMethod = httpMethod;
        RequestPath = requestPath;
        ResponseStatusCode = responseStatusCode;
        CorrelationId = correlationId;
        RequestId = requestId;
        SessionId = sessionId;
        UpdateModifiedOn();
    }
}