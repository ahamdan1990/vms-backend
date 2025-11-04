using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using VisitorManagementSystem.Api.Application.Commands.Permissions;
using VisitorManagementSystem.Api.Application.Queries.Permissions;
using VisitorManagementSystem.Api.Application.DTOs.Permissions;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Domain.Constants;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Roles management controller for managing roles and their permissions
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class RolesController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IMediator mediator, ILogger<RolesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all roles in the system
    /// </summary>
    /// <param name="isActive">Filter by active status (optional)</param>
    /// <param name="includeCounts">Include user and permission counts (default: true)</param>
    /// <returns>List of roles</returns>
    [HttpGet]
    [Authorize(Policy = Permissions.Role.ReadAll)]
    [ProducesResponseType(typeof(ApiResponseDto<List<RoleDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 403)]
    public async Task<IActionResult> GetAllRoles(
        [FromQuery] bool? isActive = null,
        [FromQuery] bool includeCounts = true)
    {
        try
        {
            var query = new GetAllRolesQuery
            {
                IsActive = isActive,
                IncludeCounts = includeCounts
            };

            var result = await _mediator.Send(query);
            return SuccessResponse(result, $"Retrieved {result.Count} roles");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return ServerErrorResponse("An error occurred while retrieving roles");
        }
    }

    /// <summary>
    /// Gets a role by ID with its permissions
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <returns>Role with permissions</returns>
    [HttpGet("{id}")]
    [Authorize(Policy = Permissions.Role.ReadAll)]
    [ProducesResponseType(typeof(ApiResponseDto<RoleWithPermissionsDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 403)]
    public async Task<IActionResult> GetRoleById(int id)
    {
        try
        {
            var query = new GetRoleWithPermissionsQuery(id);
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFoundResponse($"Role with ID {id} not found");
            }

            return SuccessResponse(result, $"Retrieved role '{result.Name}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role {RoleId}", id);
            return ServerErrorResponse("An error occurred while retrieving the role");
        }
    }

    /// <summary>
    /// Creates a new role
    /// </summary>
    /// <param name="createRoleDto">Role creation data</param>
    /// <returns>Created role</returns>
    [HttpPost]
    [Authorize(Policy = Permissions.Role.Create)]
    [ProducesResponseType(typeof(ApiResponseDto<RoleDto>), 201)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 403)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto createRoleDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequestResponse("Invalid role data");
            }

            var command = new CreateRoleCommand(createRoleDto);
            var result = await _mediator.Send(command);

            _logger.LogInformation("Created new role: {RoleName} with ID {RoleId}", result.Name, result.Id);
            return CreatedResponse(result, $"Role '{result.Name}' created successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create role: {Message}", ex.Message);
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            return ServerErrorResponse("An error occurred while creating the role");
        }
    }

    /// <summary>
    /// Updates an existing role
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="updateRoleDto">Role update data</param>
    /// <returns>Updated role</returns>
    [HttpPut("{id}")]
    [Authorize(Policy = Permissions.Role.Update)]
    [ProducesResponseType(typeof(ApiResponseDto<RoleDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 403)]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleDto updateRoleDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequestResponse("Invalid role data");
            }

            var command = new UpdateRoleCommand(id, updateRoleDto);
            var result = await _mediator.Send(command);

            _logger.LogInformation("Updated role: {RoleName} with ID {RoleId}", result.Name, result.Id);
            return SuccessResponse(result, $"Role '{result.Name}' updated successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update role {RoleId}: {Message}", id, ex.Message);
            return NotFoundResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role {RoleId}", id);
            return ServerErrorResponse("An error occurred while updating the role");
        }
    }

    /// <summary>
    /// Grants permissions to a role
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="grantPermissionsDto">Permissions to grant</param>
    /// <returns>Number of permissions granted</returns>
    [HttpPost("{id}/permissions/grant")]
    [Authorize(Policy = Permissions.Role.ManagePermissions)]
    [ProducesResponseType(typeof(ApiResponseDto<int>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 403)]
    public async Task<IActionResult> GrantPermissions(int id, [FromBody] GrantPermissionsDto grantPermissionsDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequestResponse("Invalid permission data");
            }

            var command = new GrantPermissionsCommand(id, grantPermissionsDto);
            var grantedCount = await _mediator.Send(command);

            _logger.LogInformation("Granted {Count} permissions to role {RoleId}", grantedCount, id);
            return SuccessResponse(grantedCount, $"Granted {grantedCount} permission(s) to role");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to grant permissions to role {RoleId}: {Message}", id, ex.Message);
            return NotFoundResponse(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized attempt to grant permissions to role {RoleId}", id);
            return UnauthorizedResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting permissions to role {RoleId}", id);
            return ServerErrorResponse("An error occurred while granting permissions");
        }
    }

    /// <summary>
    /// Revokes permissions from a role
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="revokePermissionsDto">Permissions to revoke</param>
    /// <returns>Number of permissions revoked</returns>
    [HttpPost("{id}/permissions/revoke")]
    [Authorize(Policy = Permissions.Role.ManagePermissions)]
    [ProducesResponseType(typeof(ApiResponseDto<int>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 403)]
    public async Task<IActionResult> RevokePermissions(int id, [FromBody] RevokePermissionsDto revokePermissionsDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequestResponse("Invalid permission data");
            }

            var command = new RevokePermissionsCommand(id, revokePermissionsDto);
            var revokedCount = await _mediator.Send(command);

            _logger.LogInformation("Revoked {Count} permissions from role {RoleId}", revokedCount, id);
            return SuccessResponse(revokedCount, $"Revoked {revokedCount} permission(s) from role");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to revoke permissions from role {RoleId}: {Message}", id, ex.Message);
            return NotFoundResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking permissions from role {RoleId}", id);
            return ServerErrorResponse("An error occurred while revoking permissions");
        }
    }
}
