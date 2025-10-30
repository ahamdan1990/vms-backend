using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using VisitorManagementSystem.Api.Application.Queries.Permissions;
using VisitorManagementSystem.Api.Application.DTOs.Permissions;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Domain.Constants;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Permissions management controller for viewing system permissions
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class PermissionsController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(IMediator mediator, ILogger<PermissionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all permissions in the system
    /// </summary>
    /// <param name="category">Filter by category (optional)</param>
    /// <param name="isActive">Filter by active status (optional)</param>
    /// <param name="searchTerm">Search term for name/description (optional)</param>
    /// <returns>List of permissions</returns>
    [HttpGet]
    [Authorize(Policy = Permissions.Permission.ReadAll)]
    [ProducesResponseType(typeof(ApiResponseDto<List<PermissionDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 403)]
    public async Task<IActionResult> GetAllPermissions(
        [FromQuery] string? category = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? searchTerm = null)
    {
        try
        {
            var query = new GetAllPermissionsQuery
            {
                Category = category?.Trim(),
                IsActive = isActive,
                SearchTerm = searchTerm?.Trim()
            };

            var result = await _mediator.Send(query);
            return SuccessResponse(result, $"Retrieved {result.Count} permissions");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions");
            return ServerErrorResponse("An error occurred while retrieving permissions");
        }
    }

    /// <summary>
    /// Gets permissions grouped by category
    /// </summary>
    /// <param name="isActive">Filter by active status (optional)</param>
    /// <returns>List of permission categories with permissions</returns>
    [HttpGet("categories")]
    [Authorize(Policy = Permissions.Permission.ReadAll)]
    [ProducesResponseType(typeof(ApiResponseDto<List<PermissionCategoryDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 403)]
    public async Task<IActionResult> GetPermissionsByCategory([FromQuery] bool? isActive = null)
    {
        try
        {
            var query = new GetPermissionsByCategoryQuery
            {
                IsActive = isActive
            };

            var result = await _mediator.Send(query);
            return SuccessResponse(result, $"Retrieved {result.Count} permission categories");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions by category");
            return ServerErrorResponse("An error occurred while retrieving permission categories");
        }
    }
}
