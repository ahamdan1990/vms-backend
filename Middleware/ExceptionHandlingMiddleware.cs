// Middleware/ExceptionHandlingMiddleware.cs
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using VisitorManagementSystem.Api.Application.DTOs.Common;

namespace VisitorManagementSystem.Api.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault();
        var ipAddress = GetClientIpAddress(context);

        // Log the exception with context
        _logger.LogError(exception,
            "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}, IP: {IpAddress}, UserAgent: {UserAgent}",
            correlationId, requestPath, requestMethod, ipAddress, userAgent);

        // Determine response based on exception type
        var response = CreateErrorResponse(exception, correlationId);

        // Set response headers
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = response.StatusCode;

        // Add security headers
        AddSecurityHeaders(context.Response);

        // Serialize and write response
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var jsonResponse = JsonSerializer.Serialize(response.Body, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }

    private ErrorResponse CreateErrorResponse(Exception exception, string correlationId)
    {
        return exception switch
        {
            ArgumentException argEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Body = ApiResponseDto<object>.ErrorResponse(
                    argEx.Message,
                    "Invalid argument provided")
            },

            UnauthorizedAccessException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Body = ApiResponseDto<object>.ErrorResponse(
                    "Access denied. Please authenticate and try again.",
                    "Unauthorized access")
            },

            InvalidOperationException invalidOpEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Body = ApiResponseDto<object>.ErrorResponse(
                    invalidOpEx.Message,
                    "Invalid operation")
            },

            NotSupportedException notSupportedEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Body = ApiResponseDto<object>.ErrorResponse(
                    notSupportedEx.Message,
                    "Operation not supported")
            },

            KeyNotFoundException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Body = ApiResponseDto<object>.ErrorResponse(
                    "The requested resource was not found",
                    "Resource not found")
            },

            TimeoutException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.RequestTimeout,
                Body = ApiResponseDto<object>.ErrorResponse(
                    "The request timed out. Please try again.",
                    "Request timeout")
            },

            TaskCanceledException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.RequestTimeout,
                Body = ApiResponseDto<object>.ErrorResponse(
                    "The request was cancelled or timed out",
                    "Request cancelled")
            },

            HttpRequestException httpEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadGateway,
                Body = ApiResponseDto<object>.ErrorResponse(
                    "An error occurred while communicating with external services",
                    "External service error")
            },

            ValidationException validationEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Body = ApiResponseDto<object>.ErrorResponse(
                    validationEx.Errors?.ToList() ?? new List<string> { validationEx.Message },
                    "Validation failed")
            },

            SecurityException secEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Forbidden,
                Body = ApiResponseDto<object>.ErrorResponse(
                    "Access denied due to security policy",
                    "Security violation")
            },

            DatabaseException dbEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = ApiResponseDto<object>.ErrorResponse(
                    _environment.IsDevelopment()
                        ? dbEx.Message
                        : "A database error occurred. Please try again later.",
                    "Database error")
            },

            ExternalServiceException extEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadGateway,
                Body = ApiResponseDto<object>.ErrorResponse(
                    $"External service '{extEx.ServiceName}' is currently unavailable",
                    "External service unavailable")
            },

            BusinessRuleException businessEx => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Body = ApiResponseDto<object>.ErrorResponse(
                    businessEx.Message,
                    "Business rule violation")
            },

            ConcurrencyException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Conflict,
                Body = ApiResponseDto<object>.ErrorResponse(
                    "The resource has been modified by another user. Please refresh and try again.",
                    "Concurrency conflict")
            },

            _ => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = ApiResponseDto<object>.ErrorResponse(
                    _environment.IsDevelopment()
                        ? exception.Message
                        : "An unexpected error occurred. Please try again later.",
                    "Internal server error")
            }
        };
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (in case of proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check for real IP header
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString();
    }

    private static void AddSecurityHeaders(HttpResponse response)
    {
        if (!response.Headers.ContainsKey("X-Content-Type-Options"))
            response.Headers.Add("X-Content-Type-Options", "nosniff");

        if (!response.Headers.ContainsKey("X-Frame-Options"))
            response.Headers.Add("X-Frame-Options", "DENY");

        if (!response.Headers.ContainsKey("X-XSS-Protection"))
            response.Headers.Add("X-XSS-Protection", "1; mode=block");

        if (!response.Headers.ContainsKey("Referrer-Policy"))
            response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    }

    private record ErrorResponse
    {
        public int StatusCode { get; init; }
        public ApiResponseDto<object> Body { get; init; } = null!;
    }
}

/// <summary>
/// Custom validation exception
/// </summary>
public class ValidationException : Exception
{
    public IEnumerable<string>? Errors { get; }

    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string message, IEnumerable<string> errors) : base(message)
    {
        Errors = errors;
    }

    public ValidationException(IEnumerable<string> errors) : base("Validation failed")
    {
        Errors = errors;
    }
}

/// <summary>
/// Custom security exception
/// </summary>
public class SecurityException : Exception
{
    public SecurityException(string message) : base(message)
    {
    }

    public SecurityException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Custom database exception
/// </summary>
public class DatabaseException : Exception
{
    public string? Operation { get; }

    public DatabaseException(string message) : base(message)
    {
    }

    public DatabaseException(string message, string operation) : base(message)
    {
        Operation = operation;
    }

    public DatabaseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Custom external service exception
/// </summary>
public class ExternalServiceException : Exception
{
    public string ServiceName { get; }

    public ExternalServiceException(string serviceName, string message) : base(message)
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, string message, Exception innerException)
        : base(message, innerException)
    {
        ServiceName = serviceName;
    }
}

/// <summary>
/// Custom business rule exception
/// </summary>
public class BusinessRuleException : Exception
{
    public string? RuleName { get; }

    public BusinessRuleException(string message) : base(message)
    {
    }

    public BusinessRuleException(string ruleName, string message) : base(message)
    {
        RuleName = ruleName;
    }
}

/// <summary>
/// Custom concurrency exception
/// </summary>
public class ConcurrencyException : Exception
{
    public string? EntityType { get; }
    public object? EntityId { get; }

    public ConcurrencyException(string message) : base(message)
    {
    }

    public ConcurrencyException(string entityType, object entityId, string message) : base(message)
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}