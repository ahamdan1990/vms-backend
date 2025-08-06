// Middleware/AuditLoggingMiddleware.cs
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using System.Security.Claims;

namespace VisitorManagementSystem.Api.Middleware;

/// <summary>
/// Audit logging middleware for tracking all API activities
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuditLoggingOptions _options;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    // ✅ PRODUCTION OPTIMIZED: Adjusted metadata size limits based on database schema
    private const int MAX_METADATA_LENGTH = 32000; // Optimized for nvarchar(max) with better performance
    private const int MAX_INDIVIDUAL_FIELD_LENGTH = 1500; // Reduced for better database performance

    public AuditLoggingMiddleware(
        RequestDelegate next,
        IOptions<AuditLoggingOptions> options,
        ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled || ShouldSkipAudit(context))
        {
            await _next(context);
            return;
        }

        var auditEntry = await CreateAuditEntryAsync(context);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Capture request body if needed
        var requestBody = await CaptureRequestBodyAsync(context);

        // Capture response
        var originalResponseBody = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            auditEntry.IsSuccess = false;
            auditEntry.ErrorMessage = TruncateString(ex.Message, 1000);
            auditEntry.ExceptionDetails = _options.IncludeExceptionDetails ? TruncateString(ex.ToString(), 2000) : null;
            auditEntry.RiskLevel = "High";
            auditEntry.RequiresAttention = true;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            // Copy response back
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;

            // Complete audit entry
            await CompleteAuditEntryAsync(context, auditEntry, stopwatch.ElapsedMilliseconds,
                requestBody, responseBodyStream);

            // Save audit entry
            await SaveAuditEntryAsync(context, auditEntry);
        }
    }

    private async Task CompleteAuditEntryAsync(HttpContext context, AuditLog auditEntry,
        long durationMs, string? requestBody, MemoryStream responseBody)
    {
        var response = context.Response;

        auditEntry.ResponseStatusCode = response.StatusCode;
        auditEntry.Duration = durationMs;
        auditEntry.RequestSize = CalculateRequestSize(context, requestBody);
        auditEntry.ResponseSize = responseBody.Length;

        // Determine success based on status code
        if (auditEntry.IsSuccess) // Only update if not already marked as failed by exception
        {
            auditEntry.IsSuccess = response.StatusCode < 400;
        }

        // Update risk level based on response
        if (response.StatusCode >= 500)
        {
            auditEntry.RiskLevel = "High";
            auditEntry.RequiresAttention = true;
        }
        else if (response.StatusCode >= 400)
        {
            auditEntry.RiskLevel = "Medium";
        }

        // FIXED: Create metadata with size constraints
        if (_options.IncludeRequestBody && ShouldCaptureRequestBody(context))
        {
            auditEntry.Metadata = CreateSafeMetadata(context, requestBody, responseBody);
        }
    }

    // FIXED: New method to create safe metadata that fits in database column
    private string? CreateSafeMetadata(HttpContext context, string? requestBody, MemoryStream responseBody)
    {
        try
        {
            // Start with essential data
            var metadata = new Dictionary<string, object>();

            // Add request body (truncated if needed)
            if (!string.IsNullOrEmpty(requestBody))
            {
                metadata["RequestBody"] = TruncateString(SanitizeData(requestBody), MAX_INDIVIDUAL_FIELD_LENGTH);
            }

            // Add headers (limited and sanitized)
            var headers = SanitizeHeaders(context.Request.Headers);
            var limitedHeaders = new Dictionary<string, string>();

            // Only include most important headers to save space
            var importantHeaders = new[] { "Accept", "Content-Type", "User-Agent", "Origin", "Host", "Content-Length" };
            foreach (var header in headers.Where(h => importantHeaders.Contains(h.Key)))
            {
                limitedHeaders[header.Key] = TruncateString(header.Value, 200);
            }
            metadata["RequestHeaders"] = limitedHeaders;

            // Add query parameters (sanitized)
            if (context.Request.Query.Any())
            {
                metadata["QueryParameters"] = SanitizeQueryParameters(context.Request.Query);
            }

            // Try to serialize and check size
            var initialJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
            {
                WriteIndented = false // Compact JSON to save space
            });

            // If still too large, create a summary
            if (initialJson.Length > MAX_METADATA_LENGTH)
            {
                return CreateMetadataSummary(context, requestBody, responseBody);
            }

            return initialJson;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create metadata, using summary");
            return CreateMetadataSummary(context, requestBody, responseBody);
        }
    }

    // FIXED: Create a summary when full metadata is too large
    private string CreateMetadataSummary(HttpContext context, string? requestBody, MemoryStream responseBody)
    {
        var summary = new
        {
            RequestBodySize = requestBody?.Length ?? 0,
            RequestBodyPreview = TruncateString(SanitizeData(requestBody), 100),
            HeaderCount = context.Request.Headers.Count,
            QueryParamCount = context.Request.Query.Count,
            ContentType = context.Request.ContentType,
            UserAgent = TruncateString(context.Request.Headers["User-Agent"].FirstOrDefault(), 100),
            Note = "Large request - summarized for storage"
        };

        var summaryJson = JsonSerializer.Serialize(summary, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        // Final safety check
        return TruncateString(summaryJson, MAX_METADATA_LENGTH);
    }

    // FIXED: Improved data sanitization with length limits
    private string? SanitizeData(string? data)
    {
        if (string.IsNullOrEmpty(data))
            return data;

        var sanitized = data;

        // Remove sensitive information
        foreach (var sensitiveField in _options.SensitiveFields)
        {
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized,
                $@"""{sensitiveField}"":\s*""[^""]*""",
                $@"""{sensitiveField}"":""***""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return sanitized;
    }

    // FIXED: Helper method to safely truncate strings
    private static string TruncateString(string? input, int maxLength)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        if (input.Length <= maxLength)
            return input;

        return input.Substring(0, Math.Max(0, maxLength - 15)) + "...[TRUNCATED]";
    }

    // CRITICAL: Special handling for database text/nvarchar(max) columns
    private string? SafeTruncateForDatabase(string? input, string fieldName)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // For nvarchar(max) columns, we should be able to store much more
        // But if there are still issues, we'll progressively truncate
        if (input.Length > MAX_METADATA_LENGTH)
        {
            _logger.LogWarning("{FieldName} is too large ({Length} chars), truncating to {MaxLength}", 
                fieldName, input.Length, MAX_METADATA_LENGTH);
            return TruncateString(input, MAX_METADATA_LENGTH);
        }

        return input;
    }

    // FIXED: Improved SaveAuditEntryAsync with better error handling
    private async Task SaveAuditEntryAsync(HttpContext context, AuditLog auditEntry)
    {
        try
        {
            // CRITICAL: Final validation and truncation before saving
            auditEntry.Metadata = SafeTruncateForDatabase(auditEntry.Metadata, "Metadata");
            auditEntry.OldValues = SafeTruncateForDatabase(auditEntry.OldValues, "OldValues");
            auditEntry.NewValues = SafeTruncateForDatabase(auditEntry.NewValues, "NewValues");
            auditEntry.ExceptionDetails = SafeTruncateForDatabase(auditEntry.ExceptionDetails, "ExceptionDetails");
            
            // Validate other text fields with their specific limits
            auditEntry.Description = TruncateString(auditEntry.Description, 500);
            auditEntry.UserAgent = TruncateString(auditEntry.UserAgent, 500);
            auditEntry.RequestPath = TruncateString(auditEntry.RequestPath, 500);
            auditEntry.ErrorMessage = TruncateString(auditEntry.ErrorMessage, 1000);
            auditEntry.ReviewComments = TruncateString(auditEntry.ReviewComments, 500);
            auditEntry.IpAddress = TruncateString(auditEntry.IpAddress, 45);
            auditEntry.CorrelationId = TruncateString(auditEntry.CorrelationId, 50);
            auditEntry.SessionId = TruncateString(auditEntry.SessionId, 50);
            auditEntry.RequestId = TruncateString(auditEntry.RequestId, 50);
            auditEntry.HttpMethod = TruncateString(auditEntry.HttpMethod, 10);
            auditEntry.RiskLevel = TruncateString(auditEntry.RiskLevel, 20);

            // Get the audit repository from DI
            using var scope = context.RequestServices.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork>();

            if (unitOfWork != null)
            {
                await unitOfWork.AuditLogs.AddAsync(auditEntry);
                await unitOfWork.SaveChangesAsync();
            }
            else
            {
                // Fallback to logging if repository not available
                _logger.LogInformation("Audit Entry: {@AuditEntry}", new
                {
                    auditEntry.EventType,
                    auditEntry.EntityName,
                    auditEntry.Action,
                    auditEntry.UserId,
                    auditEntry.IsSuccess,
                    auditEntry.ResponseStatusCode,
                    auditEntry.Duration
                });
            }
        }
        catch (Exception ex) when (ex.Message.Contains("String or binary data would be truncated"))
        {
            // CRITICAL: Handle string truncation by creating a minimal audit entry
            _logger.LogWarning(ex, "String truncation error, creating minimal audit entry for {Method} {Path}",
                auditEntry.HttpMethod, auditEntry.RequestPath);

            try
            {
                // Create a minimal audit entry with only essential fields
                var minimalEntry = new AuditLog
                {
                    EventType = auditEntry.EventType,
                    EntityName = TruncateString(auditEntry.EntityName, 100),
                    Action = TruncateString(auditEntry.Action, 50),
                    Description = "Large request - minimal audit due to size constraints",
                    UserId = auditEntry.UserId,
                    IpAddress = TruncateString(auditEntry.IpAddress, 45),
                    UserAgent = TruncateString(auditEntry.UserAgent, 200),
                    HttpMethod = TruncateString(auditEntry.HttpMethod, 10),
                    RequestPath = TruncateString(auditEntry.RequestPath, 200),
                    ResponseStatusCode = auditEntry.ResponseStatusCode,
                    IsSuccess = auditEntry.IsSuccess,
                    Duration = auditEntry.Duration,
                    RiskLevel = "Medium", // Flag for review
                    RequiresAttention = true,
                    Metadata = "{\"note\":\"Original data too large for storage\"}",
                    CreatedOn = DateTime.UtcNow,
                    IsActive = true
                };

                using var scope = context.RequestServices.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork>();
                if (unitOfWork != null)
                {
                    await unitOfWork.AuditLogs.AddAsync(minimalEntry);
                    await unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Failed to save even minimal audit entry");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save audit entry for {Method} {Path} - User: {UserId}",
                auditEntry.HttpMethod, auditEntry.RequestPath, auditEntry.UserId);

            // Don't rethrow - audit logging shouldn't break the main request
        }
    }

    // ... (rest of the existing methods remain the same)
    private async Task<AuditLog> CreateAuditEntryAsync(HttpContext context)
    {
        var request = context.Request;
        var user = context.User;

        var auditEntry = new AuditLog
        {
            EventType = DetermineEventType(request),
            EntityName = ExtractEntityName(request.Path),
            Action = DetermineAction(request.Method, request.Path),
            Description = $"{request.Method} {request.Path}",
            IpAddress = GetClientIpAddress(context),
            UserAgent = request.Headers["User-Agent"].FirstOrDefault(),
            CorrelationId = context.TraceIdentifier,
            SessionId = context.Session?.Id,
            RequestId = request.Headers["X-Request-ID"].FirstOrDefault() ?? context.TraceIdentifier,
            HttpMethod = request.Method,
            RequestPath = request.Path + request.QueryString,
            UserId = GetUserId(user),
            IsSuccess = true,
            RiskLevel = DetermineRiskLevel(request),
            CreatedOn = DateTime.UtcNow
        };

        // Set entity ID if available
        auditEntry.EntityId = ExtractEntityId(request.Path);

        return auditEntry;
    }

    private static EventType DetermineEventType(HttpRequest request)
    {
        var method = request.Method.ToUpper();
        var path = request.Path.Value?.ToLower() ?? "";

        if (path.Contains("/auth/"))
            return EventType.Authentication;

        if (path.Contains("/user"))
            return EventType.UserManagement;

        if (path.Contains("/invitation"))
            return EventType.Invitation;

        if (path.Contains("/visitor"))
            return EventType.Visitor;

        if (path.Contains("/checkin") || path.Contains("/checkout"))
            return EventType.CheckInOut;

        if (path.Contains("/system") || path.Contains("/config"))
            return EventType.SystemConfiguration;

        return EventType.General;
    }

    private static string ExtractEntityName(PathString path)
    {
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

        if (segments.Length >= 2)
        {
            var entityName = segments[1]; // Usually api/[entity]
            return char.ToUpper(entityName[0]) + entityName[1..];
        }

        return "Unknown";
    }

    private static string DetermineAction(string method, PathString path)
    {
        var pathStr = path.Value?.ToLower() ?? "";

        return method.ToUpper() switch
        {
            "GET" => pathStr.Contains("/search") ? "Search" : "Read",
            "POST" => pathStr.Contains("/login") ? "Login" :
                     pathStr.Contains("/logout") ? "Logout" :
                     pathStr.Contains("/activate") ? "Activate" :
                     pathStr.Contains("/deactivate") ? "Deactivate" :
                     pathStr.Contains("/approve") ? "Approve" :
                     pathStr.Contains("/deny") ? "Deny" :
                     "Create",
            "PUT" => "Update",
            "PATCH" => "PartialUpdate",
            "DELETE" => "Delete",
            _ => method
        };
    }

    private static string DetermineRiskLevel(HttpRequest request)
    {
        var method = request.Method.ToUpper();
        var path = request.Path.Value?.ToLower() ?? "";

        // High risk operations
        if (method == "DELETE" ||
            path.Contains("/delete") ||
            path.Contains("/reset") ||
            path.Contains("/admin") ||
            path.Contains("/system"))
            return "High";

        // Medium risk operations
        if (method is "POST" or "PUT" or "PATCH" ||
            path.Contains("/create") ||
            path.Contains("/update") ||
            path.Contains("/approve") ||
            path.Contains("/activate"))
            return "Medium";

        // Low risk operations (GET, etc.)
        return "Low";
    }

    private static int? ExtractEntityId(PathString path)
    {
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

        // Look for numeric segments that could be IDs
        foreach (var segment in segments)
        {
            if (int.TryParse(segment, out var id))
                return id;
        }

        return null;
    }

    private static int? GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                         user?.FindFirst("sub")?.Value;

        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private async Task<string?> CaptureRequestBodyAsync(HttpContext context)
    {
        if (!_options.IncludeRequestBody || !ShouldCaptureRequestBody(context))
            return null;

        try
        {
            context.Request.EnableBuffering();
            var buffer = new byte[Math.Min(context.Request.ContentLength ?? 0, _options.MaxRequestBodySize)];
            await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            context.Request.Body.Position = 0;

            return Encoding.UTF8.GetString(buffer);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture request body");
            return null;
        }
    }

    private bool ShouldSkipAudit(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Skip health checks and static files
        if (path.StartsWith("/health") ||
            path.StartsWith("/swagger") ||
            path.StartsWith("/favicon") ||
            path.StartsWith("/css") ||
            path.StartsWith("/js") ||
            path.StartsWith("/images"))
            return true;

        // Skip if in excluded paths
        return _options.ExcludePaths.Any(excludePath =>
            path.StartsWith(excludePath.ToLower()));
    }

    private bool ShouldCaptureRequestBody(HttpContext context)
    {
        var contentType = context.Request.ContentType?.ToLower() ?? "";

        // Only capture JSON and form data
        if (!contentType.Contains("application/json") &&
            !contentType.Contains("application/x-www-form-urlencoded"))
            return false;

        // Skip large requests
        var contentLength = context.Request.ContentLength ?? 0;
        if (contentLength > _options.MaxRequestBodySize)
            return false;

        // Skip sensitive endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        return !_options.SensitivePaths.Any(sensitivePath =>
            path.Contains(sensitivePath.ToLower()));
    }

    private long CalculateRequestSize(HttpContext context, string? requestBody)
    {
        var size = 0L;

        // Headers size (approximate)
        foreach (var header in context.Request.Headers)
        {
            size += header.Key.Length + (header.Value.ToString()?.Length ?? 0);
        }

        // Body size
        size += requestBody?.Length ?? context.Request.ContentLength ?? 0;

        return size;
    }

    private Dictionary<string, string> SanitizeHeaders(IHeaderDictionary headers)
    {
        var sanitized = new Dictionary<string, string>();

        foreach (var header in headers)
        {
            var key = header.Key;
            var value = header.Value.ToString();

            // Sanitize sensitive headers
            if (_options.SensitiveHeaders.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                value = "***";
            }

            sanitized[key] = value;
        }

        return sanitized;
    }

    private Dictionary<string, string> SanitizeQueryParameters(IQueryCollection query)
    {
        var sanitized = new Dictionary<string, string>();

        foreach (var param in query)
        {
            var key = param.Key;
            var value = param.Value.ToString();

            // Sanitize sensitive parameters
            if (_options.SensitiveFields.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                value = "***";
            }

            sanitized[key] = value;
        }

        return sanitized;
    }
}

/// <summary>
/// Audit logging configuration options
/// </summary>
public class AuditLoggingOptions
{
    public bool Enabled { get; set; } = true;
    public bool IncludeRequestBody { get; set; } = true;
    public bool IncludeResponseBodyOnError { get; set; } = false; // CHANGED: Disabled by default to save space
    public bool IncludeExceptionDetails { get; set; } = true;
    public long MaxRequestBodySize { get; set; } = 1024 * 512; // CHANGED: Reduced to 512KB
    public List<string> ExcludePaths { get; set; } = new()
    {
        "/health",
        "/swagger",
        "/favicon",
        "/metrics"
    };
    public List<string> SensitivePaths { get; set; } = new()
    {
        "/auth/login",
        "/auth/change-password",
        "/auth/reset-password"
    };
    public List<string> SensitiveFields { get; set; } = new()
    {
        "password",
        "currentPassword",
        "newPassword",
        "confirmPassword",
        "token",
        "refreshToken",
        "accessToken",
        "apiKey",
        "secret",
        "ssn",
        "socialSecurityNumber",
        "creditCard",
        "bankAccount"
    };
    public List<string> SensitiveHeaders { get; set; } = new()
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-API-Key",
        "X-Auth-Token"
    };
}