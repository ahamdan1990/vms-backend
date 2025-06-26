// Controllers/BaseController.cs
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Net;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Base controller providing common functionality for all API controllers
/// </summary>
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Gets the current user ID from claims
    /// </summary>
    /// <returns>Current user ID or null if not authenticated</returns>
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
    /// <returns>Current user email or null if not authenticated</returns>
    protected string? GetCurrentUserEmail()
    {
        return User?.FindFirst(ClaimTypes.Email)?.Value ??
               User?.FindFirst("email")?.Value;
    }

    /// <summary>
    /// Gets the current user's full name from claims
    /// </summary>
    /// <returns>Current user's full name or null if not authenticated</returns>
    protected string? GetCurrentUserName()
    {
        return User?.FindFirst(ClaimTypes.Name)?.Value ??
               User?.FindFirst("name")?.Value;
    }

    /// <summary>
    /// Gets the current user's role from claims
    /// </summary>
    /// <returns>Current user's role or null if not authenticated</returns>
    protected string? GetCurrentUserRole()
    {
        return User?.FindFirst(ClaimTypes.Role)?.Value ??
               User?.FindFirst("role")?.Value;
    }

    /// <summary>
    /// Gets all user permissions from claims
    /// </summary>
    /// <returns>List of user permissions</returns>
    protected List<string> GetCurrentUserPermissions()
    {
        return User?.FindAll("permission")?.Select(c => c.Value).ToList() ?? new List<string>();
    }

    /// <summary>
    /// Checks if current user has a specific permission
    /// </summary>
    /// <param name="permission">Permission to check</param>
    /// <returns>True if user has the permission</returns>
    protected bool HasPermission(string permission)
    {
        if (string.IsNullOrEmpty(permission))
            return false;

        return GetCurrentUserPermissions().Contains(permission);
    }

    /// <summary>
    /// Checks if current user has any of the specified permissions
    /// </summary>
    /// <param name="permissions">Permissions to check</param>
    /// <returns>True if user has any of the permissions</returns>
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
    /// <param name="permissions">Permissions to check</param>
    /// <returns>True if user has all permissions</returns>
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
    /// <param name="role">Role to check</param>
    /// <returns>True if user has the role</returns>
    protected bool HasRole(string role)
    {
        if (string.IsNullOrEmpty(role))
            return false;

        return User?.IsInRole(role) ?? false;
    }

    /// <summary>
    /// Checks if current user has any of the specified roles
    /// </summary>
    /// <param name="roles">Roles to check</param>
    /// <returns>True if user has any of the roles</returns>
    protected bool HasAnyRole(params string[] roles)
    {
        if (roles == null || roles.Length == 0)
            return false;

        return roles.Any(role => User?.IsInRole(role) ?? false);
    }

    /// <summary>
    /// Gets the client IP address
    /// </summary>
    /// <returns>Client IP address</returns>
    protected string? GetClientIpAddress()
    {
        // Check for forwarded IP first (in case of proxy/load balancer)
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs, take the first one
            var firstIp = forwardedFor.Split(',')[0].Trim();
            if (IPAddress.TryParse(firstIp, out _))
            {
                return firstIp;
            }
        }

        // Check for real IP header
        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp) && IPAddress.TryParse(realIp, out _))
        {
            return realIp;
        }

        // Fall back to connection remote IP
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Gets the user agent string
    /// </summary>
    /// <returns>User agent string</returns>
    protected string? GetUserAgent()
    {
        return Request.Headers["User-Agent"].FirstOrDefault();
    }

    /// <summary>
    /// Gets the correlation ID for request tracking
    /// </summary>
    /// <returns>Correlation ID</returns>
    protected string GetCorrelationId()
    {
        return HttpContext.TraceIdentifier;
    }

    /// <summary>
    /// Gets the request ID from headers or generates a new one
    /// </summary>
    /// <returns>Request ID</returns>
    protected string GetRequestId()
    {
        return Request.Headers["X-Request-ID"].FirstOrDefault() ??
               HttpContext.TraceIdentifier;
    }

    /// <summary>
    /// Gets the session ID if available
    /// </summary>
    /// <returns>Session ID</returns>
    protected string? GetSessionId()
    {
        return HttpContext.Session?.Id;
    }

    /// <summary>
    /// Gets model state errors as a list
    /// </summary>
    /// <returns>List of validation errors</returns>
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
    /// <returns>Dictionary of field errors</returns>
    protected Dictionary<string, List<string>> GetModelStateErrorsByField()
    {
        return ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToList()
            );
    }

    /// <summary>
    /// Creates a standardized validation error response
    /// </summary>
    /// <param name="errors">Validation errors</param>
    /// <param name="message">General error message</param>
    /// <returns>BadRequest result with validation errors</returns>
    protected IActionResult ValidationError(List<string> errors, string? message = null)
    {
        return BadRequest(new
        {
            success = false,
            message = message ?? "Validation failed",
            errors = errors,
            timestamp = DateTime.UtcNow,
            correlationId = GetCorrelationId()
        });
    }

    /// <summary>
    /// Creates a standardized validation error response with field-specific errors
    /// </summary>
    /// <param name="fieldErrors">Field-specific validation errors</param>
    /// <param name="message">General error message</param>
    /// <returns>BadRequest result with field validation errors</returns>
    protected IActionResult ValidationError(Dictionary<string, List<string>> fieldErrors, string? message = null)
    {
        return BadRequest(new
        {
            success = false,
            message = message ?? "Validation failed",
            errors = fieldErrors.SelectMany(kvp => kvp.Value).ToList(),
            fieldErrors = fieldErrors,
            timestamp = DateTime.UtcNow,
            correlationId = GetCorrelationId()
        });
    }

    /// <summary>
    /// Creates a standardized unauthorized response
    /// </summary>
    /// <param name="message">Unauthorized message</param>
    /// <returns>Unauthorized result</returns>
    protected IActionResult UnauthorizedResponse(string? message = null)
    {
        return Unauthorized(new
        {
            success = false,
            message = message ?? "Unauthorized access",
            timestamp = DateTime.UtcNow,
            correlationId = GetCorrelationId()
        });
    }

    /// <summary>
    /// Creates a standardized forbidden response
    /// </summary>
    /// <param name="message">Forbidden message</param>
    /// <returns>Forbidden result</returns>
    protected IActionResult ForbiddenResponse(string? message = null)
    {
        return StatusCode(403, new
        {
            success = false,
            message = message ?? "Insufficient permissions",
            timestamp = DateTime.UtcNow,
            correlationId = GetCorrelationId()
        });
    }

    /// <summary>
    /// Creates a standardized not found response
    /// </summary>
    /// <param name="resource">Resource that was not found</param>
    /// <param name="id">Resource ID</param>
    /// <returns>NotFound result</returns>
    protected IActionResult NotFoundResponse(string resource, object? id = null)
    {
        var message = id != null
            ? $"{resource} with ID '{id}' not found"
            : $"{resource} not found";

        return NotFound(new
        {
            success = false,
            message = message,
            timestamp = DateTime.UtcNow,
            correlationId = GetCorrelationId()
        });
    }

    /// <summary>
    /// Creates a standardized server error response
    /// </summary>
    /// <param name="message">Error message</param>
    /// <returns>Internal server error result</returns>
    protected IActionResult ServerErrorResponse(string? message = null)
    {
        return StatusCode(500, new
        {
            success = false,
            message = message ?? "An internal server error occurred",
            timestamp = DateTime.UtcNow,
            correlationId = GetCorrelationId()
        });
    }

    /// <summary>
    /// Creates a standardized success response
    /// </summary>
    /// <param name="data">Response data</param>
    /// <param name="message">Success message</param>
    /// <returns>OK result with data</returns>
    protected IActionResult SuccessResponse(object? data = null, string? message = null)
    {
        return Ok(new
        {
            success = true,
            message = message,
            data = data,
            timestamp = DateTime.UtcNow,
            correlationId = GetCorrelationId()
        });
    }

    /// <summary>
    /// Creates a standardized created response
    /// </summary>
    /// <param name="data">Created resource data</param>
    /// <param name="location">Location of created resource</param>
    /// <param name="message">Success message</param>
    /// <returns>Created result with data</returns>
    protected IActionResult CreatedResponse(object data, string? location = null, string? message = null)
    {
        var response = new
        {
            success = true,
            message = message ?? "Resource created successfully",
            data = data,
            timestamp = DateTime.UtcNow,
            correlationId = GetCorrelationId()
        };

        return location != null
            ? Created(location, response)
            : StatusCode(201, response);
    }

    /// <summary>
    /// Validates that the current user can access a resource owned by a specific user
    /// </summary>
    /// <param name="resourceOwnerId">ID of the resource owner</param>
    /// <param name="requiredPermission">Permission required for access if not owner</param>
    /// <returns>True if access is allowed</returns>
    protected bool CanAccessUserResource(int resourceOwnerId, string? requiredPermission = null)
    {
        var currentUserId = GetCurrentUserId();

        // User can access their own resources
        if (currentUserId == resourceOwnerId)
            return true;

        // Check if user has required permission for other users' resources
        if (!string.IsNullOrEmpty(requiredPermission))
            return HasPermission(requiredPermission);

        return false;
    }

    /// <summary>
    /// Gets pagination parameters from query string
    /// </summary>
    /// <param name="defaultPageSize">Default page size</param>
    /// <param name="maxPageSize">Maximum allowed page size</param>
    /// <returns>Pagination parameters</returns>
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
    /// <param name="defaultSortBy">Default sort field</param>
    /// <param name="allowedSortFields">Allowed sort fields</param>
    /// <returns>Sort parameters</returns>
    protected (string sortBy, string sortDirection) GetSortParameters(
        string defaultSortBy = "Id",
        params string[] allowedSortFields)
    {
        var sortBy = Request.Query["sortBy"].ToString() ?? defaultSortBy;
        var sortDirection = Request.Query["sortDirection"].ToString()?.ToLower() ?? "asc";

        // Validate sort field
        if (allowedSortFields.Length > 0 && !allowedSortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
        {
            sortBy = defaultSortBy;
        }

        // Validate sort direction
        if (sortDirection != "desc")
        {
            sortDirection = "asc";
        }

        return (sortBy, sortDirection);
    }

    /// <summary>
    /// Gets search term from query string
    /// </summary>
    /// <param name="minLength">Minimum search term length</param>
    /// <returns>Search term or null if invalid</returns>
    protected string? GetSearchTerm(int minLength = 2)
    {
        var searchTerm = Request.Query["search"].ToString()?.Trim();

        return !string.IsNullOrEmpty(searchTerm) && searchTerm.Length >= minLength
            ? searchTerm
            : null;
    }

    /// <summary>
    /// Adds cache control headers to the response
    /// </summary>
    /// <param name="cacheTime">Cache duration</param>
    /// <param name="isPublic">Whether cache is public</param>
    protected void SetCacheHeaders(TimeSpan cacheTime, bool isPublic = false)
    {
        var cacheControl = isPublic ? "public" : "private";
        Response.Headers.Add("Cache-Control", $"{cacheControl}, max-age={cacheTime.TotalSeconds}");
        Response.Headers.Add("Expires", DateTime.UtcNow.Add(cacheTime).ToString("R"));
    }

    /// <summary>
    /// Adds no-cache headers to the response
    /// </summary>
    protected void SetNoCacheHeaders()
    {
        Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
        Response.Headers.Add("Pragma", "no-cache");
        Response.Headers.Add("Expires", "0");
    }

    /// <summary>
    /// Adds security headers to the response
    /// </summary>
    protected void SetSecurityHeaders()
    {
        Response.Headers.Add("X-Content-Type-Options", "nosniff");
        Response.Headers.Add("X-Frame-Options", "DENY");
        Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    }

    /// <summary>
    /// Checks if the request is from a mobile device
    /// </summary>
    /// <returns>True if request is from mobile device</returns>
    protected bool IsMobileRequest()
    {
        var userAgent = GetUserAgent()?.ToLower() ?? "";
        return userAgent.Contains("mobile") ||
               userAgent.Contains("android") ||
               userAgent.Contains("iphone") ||
               userAgent.Contains("ipad");
    }

    /// <summary>
    /// Gets the request protocol (HTTP/HTTPS)
    /// </summary>
    /// <returns>Request protocol</returns>
    protected string GetRequestProtocol()
    {
        return Request.IsHttps ? "https" : "http";
    }

    /// <summary>
    /// Gets the full request URL
    /// </summary>
    /// <returns>Full request URL</returns>
    protected string GetRequestUrl()
    {
        return $"{GetRequestProtocol()}://{Request.Host}{Request.Path}{Request.QueryString}";
    }

    /// <summary>
    /// Gets the base URL of the request
    /// </summary>
    /// <returns>Base URL</returns>
    protected string GetBaseUrl()
    {
        return $"{GetRequestProtocol()}://{Request.Host}";
    }
}