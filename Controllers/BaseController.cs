// Controllers/BaseController.cs
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Net;
using VisitorManagementSystem.Api.Application.DTOs.Common;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Base controller providing common functionality for all API controllers
/// </summary>
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Gets the current user ID from claims
    /// </summary>
    protected int? GetCurrentUserId()
    {
        var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                         User?.FindFirst("sub")?.Value ??
                         User?.FindFirst("user_id")?.Value;

        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Gets the current user email from claims
    /// </summary>
    protected string? GetCurrentUserEmail()
    {
        return User?.FindFirst(ClaimTypes.Email)?.Value ??
               User?.FindFirst("email")?.Value;
    }

    /// <summary>
    /// Gets the current user's full name from claims
    /// </summary>
    protected string? GetCurrentUserName()
    {
        return User?.FindFirst(ClaimTypes.Name)?.Value ??
               User?.FindFirst("name")?.Value;
    }

    /// <summary>
    /// Gets the current user's role from claims
    /// </summary>
    protected string? GetCurrentUserRole()
    {
        return User?.FindFirst(ClaimTypes.Role)?.Value ??
               User?.FindFirst("role")?.Value;
    }

    /// <summary>
    /// Gets all user permissions from claims
    /// </summary>
    protected List<string> GetCurrentUserPermissions()
    {
        return User?.FindAll("permission")?.Select(c => c.Value).ToList() ?? new List<string>();
    }

    /// <summary>
    /// Checks if current user has a specific permission
    /// </summary>
    protected bool HasPermission(string permission)
    {
        if (string.IsNullOrEmpty(permission))
            return false;

        return GetCurrentUserPermissions().Contains(permission);
    }

    /// <summary>
    /// Checks if current user has any of the specified permissions
    /// </summary>
    protected bool HasAnyPermission(params string[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
            return false;

        var userPermissions = GetCurrentUserPermissions();
        return permissions.Any(p => userPermissions.Contains(p));
    }

    /// <summary>
    /// Checks if current user has all of the specified permissions
    /// </summary>
    protected bool HasAllPermissions(params string[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
            return true;

        var userPermissions = GetCurrentUserPermissions();
        return permissions.All(p => userPermissions.Contains(p));
    }

    /// <summary>
    /// Checks if current user has a specific role
    /// </summary>
    protected bool HasRole(string role)
    {
        if (string.IsNullOrEmpty(role))
            return false;

        return User?.IsInRole(role) ?? false;
    }

    /// <summary>
    /// Checks if current user has any of the specified roles
    /// </summary>
    protected bool HasAnyRole(params string[] roles)
    {
        if (roles == null || roles.Length == 0)
            return false;

        return roles.Any(role => User?.IsInRole(role) ?? false);
    }

    /// <summary>
    /// Gets the client IP address
    /// </summary>
    protected string? GetClientIpAddress()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var firstIp = forwardedFor.Split(',')[0].Trim();
            if (IPAddress.TryParse(firstIp, out _))
                return firstIp;
        }

        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp) && IPAddress.TryParse(realIp, out _))
            return realIp;

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Gets the user agent string
    /// </summary>
    protected string? GetUserAgent()
    {
        return Request.Headers["User-Agent"].FirstOrDefault();
    }

    /// <summary>
    /// Gets the correlation ID for request tracking
    /// </summary>
    protected string GetCorrelationId()
    {
        return HttpContext.Items["CorrelationId"]?.ToString() ?? HttpContext.TraceIdentifier;
    }

    /// <summary>
    /// Gets the request ID from headers or generates a new one
    /// </summary>
    protected string GetRequestId()
    {
        return Request.Headers["X-Request-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
    }

    /// <summary>
    /// Gets the session ID if available
    /// </summary>
    protected string? GetSessionId()
    {
        return HttpContext.Session?.Id;
    }

    /// <summary>
    /// Gets model state errors as a list
    /// </summary>
    protected List<string> GetModelStateErrors()
    {
        return ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrEmpty(m))
            .ToList();
    }

    /// <summary>
    /// Gets model state errors grouped by field
    /// </summary>
    protected Dictionary<string, List<string>> GetModelStateErrorsByField()
    {
        return ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToList()
            );
    }

    #region Response Helper Methods

    /// <summary>
    /// Creates a successful response
    /// </summary>
    protected IActionResult SuccessResponse<T>(T data, string? message = null)
    {
        return Ok(ApiResponseDto<T>.SuccessResponse(data, message, GetCorrelationId()));
    }

    /// <summary>
    /// Creates a successful response without data
    /// </summary>
    protected IActionResult SuccessResponse(string? message = null)
    {
        return Ok(ApiResponseDto<object>.SuccessResponse(null, message, GetCorrelationId()));
    }

    /// <summary>
    /// Creates a bad request response with single error
    /// </summary>
    protected IActionResult BadRequestResponse(string error, string? message = null)
    {
        return BadRequest(ApiResponseDto<object>.ErrorResponse(error, message, GetCorrelationId()));
    }

    /// <summary>
    /// Creates a bad request response with multiple errors
    /// </summary>
    protected IActionResult BadRequestResponse(List<string> errors, string? message = null)
    {
        return BadRequest(ApiResponseDto<object>.ErrorResponse(errors, message, GetCorrelationId()));
    }

    /// <summary>
    /// Creates a validation error response
    /// </summary>
    protected IActionResult ValidationError(List<string> errors, string? message = null)
    {
        return BadRequest(ApiResponseDto<object>.ErrorResponse(errors, message, GetCorrelationId()));
    }

    /// <summary>
    /// Creates a validation error response with field-specific errors
    /// </summary>
    protected IActionResult ValidationError(Dictionary<string, List<string>> fieldErrors, string? message = null)
    {
        return BadRequest(ApiResponseDto<object>.ErrorResponse(
            fieldErrors.SelectMany(kvp => kvp.Value).ToList(), message, GetCorrelationId()));
    }

    /// <summary>
    /// Creates an unauthorized response
    /// </summary>
    protected IActionResult UnauthorizedResponse(string? message = null)
    {
        return Unauthorized(ApiResponseDto<object>.ErrorResponse(
            message ?? "Unauthorized access", null, GetCorrelationId()));
    }

    /// <summary>
    /// Creates a forbidden response
    /// </summary>
    protected IActionResult ForbiddenResponse(string? message = null)
    {
        return StatusCode(403, ApiResponseDto<object>.ErrorResponse(
            message ?? "Insufficient permissions", null, GetCorrelationId()));
    }

    /// <summary>
    /// Creates a not found response
    /// </summary>
    protected IActionResult NotFoundResponse(string resource, object? id = null)
    {
        var message = id != null
            ? $"{resource} with ID '{id}' not found"
            : $"{resource} not found";

        return NotFound(ApiResponseDto<object>.ErrorResponse(message, null, GetCorrelationId()));
    }

    /// <summary>
    /// Creates a server error response
    /// </summary>
    protected IActionResult ServerErrorResponse(string? message = null)
    {
        return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
            message ?? "An internal server error occurred", null, GetCorrelationId()));
    }

    /// <summary>
    /// Creates a created response
    /// </summary>
    protected IActionResult CreatedResponse<T>(T data, string? location = null, string? message = null)
    {
        var response = ApiResponseDto<T>.SuccessResponse(data, message ?? "Resource created successfully", GetCorrelationId());

        return location != null
            ? Created(location, response)
            : StatusCode(201, response);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Validates that the current user can access a resource owned by a specific user
    /// </summary>
    protected bool CanAccessUserResource(int resourceOwnerId, string? requiredPermission = null)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId == resourceOwnerId)
            return true;

        if (!string.IsNullOrEmpty(requiredPermission))
            return HasPermission(requiredPermission);

        return false;
    }

    /// <summary>
    /// Gets pagination parameters from query string
    /// </summary>
    protected (int pageIndex, int pageSize) GetPaginationParameters(int defaultPageSize = 20, int maxPageSize = 100)
    {
        var pageIndex = Math.Max(0, int.TryParse(Request.Query["pageIndex"], out var pi) ? pi : 0);
        var pageSize = int.TryParse(Request.Query["pageSize"], out var ps) ? ps : defaultPageSize;

        pageSize = Math.Min(Math.Max(1, pageSize), maxPageSize);

        return (pageIndex, pageSize);
    }

    /// <summary>
    /// Gets sort parameters from query string
    /// </summary>
    protected (string sortBy, string sortDirection) GetSortParameters(
        string defaultSortBy = "Id",
        params string[] allowedSortFields)
    {
        var sortBy = Request.Query["sortBy"].ToString() ?? defaultSortBy;
        var sortDirection = Request.Query["sortDirection"].ToString()?.ToLower() ?? "asc";

        if (allowedSortFields.Length > 0 && !allowedSortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            sortBy = defaultSortBy;

        if (sortDirection != "desc")
            sortDirection = "asc";

        return (sortBy, sortDirection);
    }

    /// <summary>
    /// Gets search term from query string
    /// </summary>
    protected string? GetSearchTerm(int minLength = 2)
    {
        var searchTerm = Request.Query["search"].ToString()?.Trim();

        return !string.IsNullOrEmpty(searchTerm) && searchTerm.Length >= minLength
            ? searchTerm
            : null;
    }

    /// <summary>
    /// Adds cache control headers
    /// </summary>
    protected void SetCacheHeaders(TimeSpan cacheTime, bool isPublic = false)
    {
        var cacheControl = isPublic ? "public" : "private";
        Response.Headers["Cache-Control"] = $"{cacheControl}, max-age={cacheTime.TotalSeconds}";
        Response.Headers["Expires"] = DateTime.UtcNow.Add(cacheTime).ToString("R");
    }

    /// <summary>
    /// Adds no-cache headers
    /// </summary>
    protected void SetNoCacheHeaders()
    {
        Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";
    }

    /// <summary>
    /// Adds security headers to the response.
    /// </summary>
    protected void SetSecurityHeaders()
    {
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["X-Frame-Options"] = "DENY";

        // Deprecated in modern browsers but kept for legacy support
        Response.Headers["X-XSS-Protection"] = "1; mode=block";

        Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    }


    /// <summary>
    /// Checks if request is from mobile device
    /// </summary>
    protected bool IsMobileRequest()
    {
        var userAgent = GetUserAgent()?.ToLower() ?? "";
        return userAgent.Contains("mobile") ||
               userAgent.Contains("android") ||
               userAgent.Contains("iphone") ||
               userAgent.Contains("ipad");
    }

    /// <summary>
    /// Gets request protocol
    /// </summary>
    protected string GetRequestProtocol()
    {
        return Request.IsHttps ? "https" : "http";
    }

    /// <summary>
    /// Gets full request URL
    /// </summary>
    protected string GetRequestUrl()
    {
        return $"{GetRequestProtocol()}://{Request.Host}{Request.Path}{Request.QueryString}";
    }

    /// <summary>
    /// Gets base URL
    /// </summary>
    protected string GetBaseUrl()
    {
        return $"{GetRequestProtocol()}://{Request.Host}";
    }

    #endregion
}