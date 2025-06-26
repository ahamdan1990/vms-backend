// Controllers/UsersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using VisitorManagementSystem.Api.Application.Commands.Users;
using VisitorManagementSystem.Api.Application.Queries.Users;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Auth;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// User management controller for CRUD operations and user administration
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UsersController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IMediator mediator,
        ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets paginated list of users
    /// </summary>
    /// <param name="pageIndex">Page index (0-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="searchTerm">Search term for filtering</param>
    /// <param name="role">Filter by role</param>
    /// <param name="status">Filter by status</param>
    /// <param name="department">Filter by department</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortDirection">Sort direction (asc/desc)</param>
    /// <returns>Paginated list of users</returns>
    [HttpGet]
    [Authorize(Policy = Permissions.User.ReadAll)]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResultDto<UserListDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 403)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] UserStatus? status = null,
        [FromQuery] string? department = null,
        [FromQuery] string sortBy = "LastName",
        [FromQuery] bool SortDescending = false)
    {
        try
        {
            var query = new GetUsersQuery
            {
                PageIndex = pageIndex,
                PageSize = Math.Min(pageSize, 100), // Max 100 items per page
                SearchTerm = searchTerm?.Trim(),
                Role = role,
                Status = status,
                Department = department?.Trim(),
                SortBy = sortBy,
                SortDescending = SortDescending
            };

            var result = await _mediator.Send(query);

            return Ok(ApiResponseDto<PagedResultDto<UserListDto>>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users list");
            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "An error occurred while retrieving users"));
        }
    }

    /// <summary>
    /// Gets user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    [HttpGet("{id}")]
    [Authorize(Policy = Permissions.User.ReadAll)]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> GetUser(int id)
    {
        try
        {
            var query = new GetUserByIdQuery();
            var user = await _mediator.Send(query);

            if (user == null)
            {
                return NotFound(ApiResponseDto<object>.ErrorResponse(
                    $"User with ID {id} not found"));
            }

            return Ok(ApiResponseDto<UserDto>.SuccessResponse(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "An error occurred while retrieving the user"));
        }
    }

    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="request">User creation request</param>
    /// <returns>Created user</returns>
    [HttpPost]
    [Authorize(Policy = Permissions.User.Create)]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), 201)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse(
                    GetModelStateErrors(), "Validation failed"));
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse("User not authenticated"));
            }

            // Validate role assignment permissions
            if (!Enum.TryParse<UserRole>(request.Role, out var targetRole))
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse(
                    $"Invalid role: {request.Role}"));
            }

            var command = new CreateUserCommand
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Role = targetRole,
                Department = request.Department,
                JobTitle = request.JobTitle,
                EmployeeId = request.EmployeeId,
                TimeZone = request.TimeZone,
                Language = request.Language,
                MustChangePassword = request.MustChangePassword,
                CreatedBy = currentUserId.Value
            };

            var result = await _mediator.Send(command);

            _logger.LogInformation("User created successfully: {UserId} ({Email}) by {CreatedBy}",
                result.Id, result.Email, currentUserId.Value);

            return CreatedAtAction(nameof(GetUser), new { id = result.Id },
                ApiResponseDto<UserDto>.SuccessResponse(result, "User created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User creation failed due to business rule violation");
            return BadRequest(ApiResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "An error occurred while creating the user"));
        }
    }

    /// <summary>
    /// Updates an existing user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">User update request</param>
    /// <returns>Updated user</returns>
    [HttpPut("{id}")]
    [Authorize(Policy = Permissions.User.UpdateAll)]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse(
                    GetModelStateErrors(), "Validation failed"));
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse("User not authenticated"));
            }

            // Validate role and status
            if (!Enum.TryParse<UserRole>(request.Role, out var targetRole))
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse(
                    $"Invalid role: {request.Role}"));
            }

            if (!Enum.TryParse<UserStatus>(request.Status, out var targetStatus))
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse(
                    $"Invalid status: {request.Status}"));
            }

            var command = new UpdateUserCommand
            {
                Id = id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Role = targetRole,
                Status = targetStatus,
                Department = request.Department,
                JobTitle = request.JobTitle,
                EmployeeId = request.EmployeeId,
                TimeZone = request.TimeZone,
                Language = request.Language,
                ModifiedBy = currentUserId.Value
            };

            var result = await _mediator.Send(command);

            if (result == null)
            {
                return NotFound(ApiResponseDto<object>.ErrorResponse(
                    $"User with ID {id} not found"));
            }

            _logger.LogInformation("User updated successfully: {UserId} by {ModifiedBy}",
                id, currentUserId.Value);

            return Ok(ApiResponseDto<UserDto>.SuccessResponse(result, "User updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User update failed due to business rule violation");
            return BadRequest(ApiResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "An error occurred while updating the user"));
        }
    }

    /// <summary>
    /// Activates a user account
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Activation request</param>
    /// <returns>Updated user</returns>
    [HttpPost("{id}/activate")]
    [Authorize(Policy = Permissions.User.Activate)]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> ActivateUser(int id, [FromBody] UserActivationDto request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse("User not authenticated"));
            }

            var command = new ActivateUserCommand
            {
                Id = id,
                ActivatedBy = currentUserId.Value,
                Reason = request.Reason,
                ResetFailedAttempts = request.ResetFailedAttempts
            };

            var result = await _mediator.Send(command);

            _logger.LogInformation("User activated: {UserId} by {ActivatedBy}. Reason: {Reason}",
                id, currentUserId.Value, request.Reason);

            return Ok(ApiResponseDto<UserDto>.SuccessResponse(result, "User activated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User activation failed");
            return BadRequest(ApiResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user {UserId}", id);
            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "An error occurred while activating the user"));
        }
    }

    /// <summary>
    /// Deactivates a user account
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Deactivation request</param>
    /// <returns>Updated user</returns>
    [HttpPost("{id}/deactivate")]
    [Authorize(Policy = Permissions.User.Deactivate)]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> DeactivateUser(int id, [FromBody] UserDeactivationDto request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse("User not authenticated"));
            }

            // Prevent self-deactivation
            if (id == currentUserId.Value)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse(
                    "You cannot deactivate your own account"));
            }

            var command = new DeactivateUserCommand
            {
                Id = id,
                DeactivatedBy = currentUserId.Value,
                Reason = request.Reason,
                RevokeAllSessions = request.RevokeAllSessions
            };

            var result = await _mediator.Send(command);

            _logger.LogInformation("User deactivated: {UserId} by {DeactivatedBy}. Reason: {Reason}",
                id, currentUserId.Value, request.Reason);

            return Ok(ApiResponseDto<UserDto>.SuccessResponse(result, "User deactivated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User deactivation failed");
            return BadRequest(ApiResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", id);
            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "An error occurred while deactivating the user"));
        }
    }

    /// <summary>
    /// Deletes a user (soft delete)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = Permissions.User.DeleteAll)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse("User not authenticated"));
            }

            // Prevent self-deletion
            if (id == currentUserId.Value)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse(
                    "You cannot delete your own account"));
            }

            var command = new DeleteUserCommand
            {
                Id = id,
                DeletedBy = currentUserId.Value
            };

            var result = await _mediator.Send(command);

            if (!result)
            {
                return NotFound(ApiResponseDto<object>.ErrorResponse(
                    $"User with ID {id} not found"));
            }

            _logger.LogInformation("User deleted: {UserId} by {DeletedBy}", id, currentUserId.Value);

            return Ok(ApiResponseDto<object>.SuccessResponse(null, "User deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User deletion failed");
            return BadRequest(ApiResponseDto<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "An error occurred while deleting the user"));
        }
    }

    /// <summary>
    /// Unlocks a user account
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Unlock request</param>
    /// <returns>Unlock result</returns>
    [HttpPost("{id}/unlock")]
    [Authorize(Policy = Permissions.User.Activate)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> UnlockUser(int id, [FromBody] UserUnlockDto request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse("User not authenticated"));
            }

            var command = new UnlockUserCommand
            {
                UserId = id,
                UnlockedBy = currentUserId.Value,
                Reason = request.Reason
            };

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return NotFound(ApiResponseDto<object>.ErrorResponse(
                    $"User with ID {id} not found or not locked"));
            }

            _logger.LogInformation("User account unlocked: {UserId} by {UnlockedBy}. Reason: {Reason}",
                id, currentUserId.Value, request.Reason);

            return Ok(ApiResponseDto<object>.SuccessResponse(null, "User account unlocked successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking user {UserId}", id);
            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "An error occurred while unlocking the user account"));
        }
    }

    /// <summary>
    /// Gets user's activity log
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="days">Number of days to look back</param>
    /// <returns>User activity log</returns>
    [HttpGet("{id}/activity")]
    [Authorize(Policy = Permissions.User.ViewActivity)]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResultDto<UserActivityDto>>), 200)]
    public async Task<IActionResult> GetUserActivity(
        int id,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] int days = 30)
    {
        try
        {
            var query = new GetUserActivityQuery
            {
                UserId = id,
                PageIndex = pageIndex,
                PageSize = Math.Min(pageSize, 100),
                Days = Math.Min(days, 365) // Max 1 year
            };

            var result = await _mediator.Send(query);

            return Ok(ApiResponseDto<PagedResultDto<UserActivityDto>>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user activity for {UserId}", id);
            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "An error occurred while retrieving user activity"));
        }
    }

    /// <summary>
    /// Resets user password (admin function)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Password reset request</param>
    /// <returns>Reset result</returns>
    [HttpPost("{id}/reset-password")]
    [Authorize(Policy = Permissions.User.ResetPassword)]
    [ProducesResponseType(typeof(ApiResponseDto<CommandResultDto>), 200)]
    public async Task<IActionResult> ResetUserPassword(int id, [FromBody] AdminPasswordResetDto request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse("User not authenticated"));
            }

            var command = new AdminPasswordResetCommand
            {
                UserId = id,
                NewPassword = request.NewPassword,
                MustChangePassword = request.MustChangePassword,
                NotifyUser = request.NotifyUser,
                ResetBy = currentUserId.Value
            };

            var result = await _mediator.Send(command);

            _logger.LogInformation("Password reset by admin for user: {UserId} by {ResetBy}",
                id, currentUserId.Value);

            return Ok(ApiResponseDto<CommandResultDto>.SuccessResponse(result,
                "Password reset successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", id);
            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "An error occurred while resetting the password"));
        }
    }

    /// <summary>
    /// Gets available roles for assignment
    /// </summary>
    /// <returns>List of available roles</returns>
    [HttpGet("roles")]
    [Authorize(Policy = Permissions.User.ReadAll)]
    [ProducesResponseType(typeof(ApiResponseDto<List<RoleDto>>), 200)]
    public async Task<IActionResult> GetAvailableRoles()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse("User not authenticated"));
            }

            var query = new GetAvailableRolesQuery
            {
                UserId = currentUserId.Value
            };

            var result = await _mediator.Send(query);

            return Ok(ApiResponseDto<List<RoleDto>>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available roles");
            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "An error occurred while retrieving available roles"));
        }
    }
}

#region DTOs

/// <summary>
/// User activation request DTO
/// </summary>
public class UserActivationDto
{
    public string? Reason { get; set; }
    public bool ResetFailedAttempts { get; set; } = true;
}

/// <summary>
/// User deactivation request DTO
/// </summary>
public class UserDeactivationDto
{
    [Required(ErrorMessage = "Reason is required for deactivation")]
    public string Reason { get; set; } = string.Empty;
    public bool RevokeAllSessions { get; set; } = true;
}

/// <summary>
/// User unlock request DTO
/// </summary>
public class UserUnlockDto
{
    public string? Reason { get; set; }
}

/// <summary>
/// Admin password reset request DTO
/// </summary>
public class AdminPasswordResetDto
{
    public string? NewPassword { get; set; } // If null, system generates one
    public bool MustChangePassword { get; set; } = true;
    public bool NotifyUser { get; set; } = true;
}


/// <summary>
/// User activity DTO
/// </summary>
public class UserActivityDto
{
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSuccess { get; set; }
}

/// <summary>
/// Role DTO
/// </summary>
public class RoleDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int HierarchyLevel { get; set; }
    public bool CanAssign { get; set; }
}

#endregion