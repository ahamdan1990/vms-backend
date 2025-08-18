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

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets paginated list of users
    /// </summary>
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
                PageSize = Math.Min(pageSize, 100),
                SearchTerm = searchTerm?.Trim(),
                Role = role,
                Status = status,
                Department = department?.Trim(),
                SortBy = sortBy,
                SortDescending = SortDescending,
                IncludeInactive = status == null || status == UserStatus.Inactive || status == UserStatus.Suspended

            };

            var result = await _mediator.Send(query);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users list");
            return ServerErrorResponse("An error occurred while retrieving users");
        }
    }

    /// <summary>
    /// Gets user by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = Permissions.User.ReadAll)]
    [ProducesResponseType(typeof(ApiResponseDto<UserDetailDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> GetUser(int id)
    {
        try
        {
            var query = new GetUserByIdQuery { UserId = id };
            var user = await _mediator.Send(query);

            if (user == null)
                return NotFoundResponse("User", id);

            return SuccessResponse(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return ServerErrorResponse("An error occurred while retrieving the user");
        }
    }

    /// <summary>
    /// Creates a new user
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.User.Create)]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), 201)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationError(GetModelStateErrors(), "Validation failed");

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return BadRequestResponse("User not authenticated");

            if (!Enum.TryParse<UserRole>(request.Role, out var targetRole))
                return BadRequestResponse($"Invalid role: {request.Role}");

            var command = new CreateUserCommand
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                
                // Enhanced phone fields
                PhoneNumber = request.PhoneNumber,
                PhoneCountryCode = request.PhoneCountryCode,
                PhoneType = request.PhoneType,
                
                Role = targetRole,
                Department = request.Department,
                JobTitle = request.JobTitle,
                EmployeeId = request.EmployeeId,
                
                // User preferences
                TimeZone = request.TimeZone,
                Language = request.Language,
                Theme = request.Theme,
                
                // Enhanced address fields
                AddressType = request.AddressType,
                Street1 = request.Street1,
                Street2 = request.Street2,
                City = request.City,
                State = request.State,
                PostalCode = request.PostalCode,
                Country = request.Country,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                
                MustChangePassword = request.MustChangePassword,
                SendWelcomeEmail = request.SendWelcomeEmail,
                CreatedBy = currentUserId.Value
            };

            var result = await _mediator.Send(command);

            _logger.LogInformation("User created successfully: {UserId} ({Email}) by {CreatedBy}",
                result.Id, result.Email, currentUserId.Value);

            return CreatedAtAction(nameof(GetUser), new { id = result.Id },
                ApiResponseDto<UserDto>.SuccessResponse(result, "User created successfully", GetCorrelationId()));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User creation failed due to business rule violation");
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return ServerErrorResponse("An error occurred while creating the user");
        }
    }

    /// <summary>
    /// Updates an existing user
    /// </summary>
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
                return ValidationError(GetModelStateErrors(), "Validation failed");

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return BadRequestResponse("User not authenticated");

            if (!Enum.TryParse<UserRole>(request.Role, out var targetRole))
                return BadRequestResponse($"Invalid role: {request.Role}");

            if (!Enum.TryParse<UserStatus>(request.Status, out var targetStatus))
                return BadRequestResponse($"Invalid status: {request.Status}");

            var command = new UpdateUserCommand
            {
                Id = id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                
                // Enhanced phone fields
                PhoneNumber = request.PhoneNumber,
                PhoneCountryCode = request.PhoneCountryCode,
                PhoneType = request.PhoneType,
                
                Role = targetRole,
                Status = targetStatus,
                Department = request.Department,
                JobTitle = request.JobTitle,
                EmployeeId = request.EmployeeId,
                
                // User preferences
                TimeZone = request.TimeZone,
                Language = request.Language,
                Theme = request.Theme,
                
                // Enhanced address fields
                AddressType = request.AddressType,
                Street1 = request.Street1,
                Street2 = request.Street2,
                City = request.City,
                State = request.State,
                PostalCode = request.PostalCode,
                Country = request.Country,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                
                ModifiedBy = currentUserId.Value
            };

            var result = await _mediator.Send(command);

            if (result == null)
                return NotFoundResponse("User", id);

            _logger.LogInformation("User updated successfully: {UserId} by {ModifiedBy}",
                id, currentUserId.Value);

            return SuccessResponse(result, "User updated successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User update failed due to business rule violation");
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return ServerErrorResponse("An error occurred while updating the user");
        }
    }

    /// <summary>
    /// Activates a user account
    /// </summary>
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
                return BadRequestResponse("User not authenticated");

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

            return SuccessResponse(result, "User activated successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User activation failed");
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user {UserId}", id);
            return ServerErrorResponse("An error occurred while activating the user");
        }
    }

    /// <summary>
    /// Deactivates a user account
    /// </summary>
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
                return BadRequestResponse("User not authenticated");

            if (id == currentUserId.Value)
                return BadRequestResponse("You cannot deactivate your own account");

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

            return SuccessResponse(result, "User deactivated successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User deactivation failed");
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", id);
            return ServerErrorResponse("An error occurred while deactivating the user");
        }
    }

    /// <summary>
    /// Deletes a user (soft delete)
    /// </summary>
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
                return BadRequestResponse("User not authenticated");

            if (id == currentUserId.Value)
                return BadRequestResponse("You cannot delete your own account");

            var command = new DeleteUserCommand
            {
                Id = id,
                DeletedBy = currentUserId.Value
            };

            var result = await _mediator.Send(command);

            if (!result)
                return NotFoundResponse("User", id);

            _logger.LogInformation("User deleted: {UserId} by {DeletedBy}", id, currentUserId.Value);
            return SuccessResponse("User deleted successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User deletion failed");
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return ServerErrorResponse("An error occurred while deleting the user");
        }
    }

    /// <summary>
    /// Unlocks a user account
    /// </summary>
    [HttpPost("{id}/unlock")]
    [Authorize(Policy = Permissions.User.Activate)]
    [ProducesResponseType(typeof(ApiResponseDto<CommandResultDto<object>>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> UnlockUser(int id, [FromBody] UserUnlockDto request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return BadRequestResponse("User not authenticated");

            var command = new UnlockUserCommand
            {
                UserId = id,
                UnlockedBy = currentUserId.Value,
                Reason = request.Reason
            };

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return NotFoundResponse("User", id);

            _logger.LogInformation("User account unlocked: {UserId} by {UnlockedBy}. Reason: {Reason}",
                id, currentUserId.Value, request.Reason);

            return SuccessResponse(result, "User account unlocked successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking user {UserId}", id);
            return ServerErrorResponse("An error occurred while unlocking the user account");
        }
    }

    /// <summary>
    /// Gets user's activity log
    /// </summary>
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
                Days = Math.Min(days, 365)
            };

            var result = await _mediator.Send(query);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user activity for {UserId}", id);
            return ServerErrorResponse("An error occurred while retrieving user activity");
        }
    }

    /// <summary>
    /// Resets user password (admin function)
    /// </summary>
    [HttpPost("{id}/reset-password")]
    [Authorize(Policy = Permissions.User.ResetPassword)]
    [ProducesResponseType(typeof(ApiResponseDto<CommandResultDto<AdminPasswordResetResponseDto>>), 200)]
    public async Task<IActionResult> ResetUserPassword(int id, [FromBody] AdminPasswordResetDto request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return BadRequestResponse("User not authenticated");

            _logger.LogDebug("Reset request details: NewPassword={NewPassword}, MustChangePassword={MustChangePassword}, NotifyUser={NotifyUser}, Reason={Reason}",
                request.NewPassword, request.MustChangePassword, request.NotifyUser, request.Reason);
            var command = new AdminPasswordResetCommand
            {
                UserId = id,
                NewPassword = request.NewPassword,
                MustChangePassword = request.MustChangePassword,
                NotifyUser = request.NotifyUser,
                ResetBy = currentUserId.Value,
                Reason = request.Reason,
                GenerateTemporaryPassword = string.IsNullOrWhiteSpace(request.NewPassword)
            };

            var result = await _mediator.Send(command);

            _logger.LogInformation("Password reset by admin for user: {UserId} by {ResetBy}",
                id, currentUserId.Value);

            return SuccessResponse(result, "Password reset successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", id);
            return ServerErrorResponse("An error occurred while resetting the password");
        }
    }

    /// <summary>
    /// Gets current user's profile
    /// GET /api/Users/profile
    /// </summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ApiResponseDto<UserProfileDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> GetCurrentUserProfile()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return BadRequestResponse("User not authenticated");

            var query = new GetCurrentUserProfileQuery { UserId = currentUserId.Value };
            var profile = await _mediator.Send(query);

            return SuccessResponse(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user profile");
            return ServerErrorResponse("An error occurred while retrieving your profile");
        }
    }

    /// <summary>
    /// Updates current user's profile (self-service)
    /// PUT /api/Users/profile
    /// </summary>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(ApiResponseDto<UserProfileDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    public async Task<IActionResult> UpdateCurrentUserProfile([FromBody] UpdateUserProfileDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationError(GetModelStateErrors(), "Validation failed");

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return BadRequestResponse("User not authenticated");

            var command = new UpdateUserProfileCommand
            {
                UserId = currentUserId.Value,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                
                // Enhanced phone fields
                PhoneNumber = request.PhoneNumber,
                PhoneCountryCode = request.PhoneCountryCode,
                PhoneType = request.PhoneType,
                
                Department = request.Department,
                JobTitle = request.JobTitle,
                EmployeeId = request.EmployeeId,
                
                // Enhanced address fields
                AddressType = request.AddressType,
                Street1 = request.Street1,
                Street2 = request.Street2,
                City = request.City,
                State = request.State,
                PostalCode = request.PostalCode,
                Country = request.Country,
                Latitude = request.Latitude,
                Longitude = request.Longitude
            };

            var result = await _mediator.Send(command);

            _logger.LogInformation("User profile updated successfully: {UserId}", currentUserId.Value);

            return SuccessResponse(result, "Profile updated successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Profile update failed due to business rule violation");
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return ServerErrorResponse("An error occurred while updating your profile");
        }
    }

    /// <summary>
    /// Updates current user's preferences
    /// PUT /api/Users/profile/preferences
    /// </summary>
    [HttpPut("profile/preferences")]
    [ProducesResponseType(typeof(ApiResponseDto<UserProfileDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    public async Task<IActionResult> UpdateCurrentUserPreferences([FromBody] UpdateUserPreferencesDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationError(GetModelStateErrors(), "Validation failed");

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return BadRequestResponse("User not authenticated");

            var command = new UpdateUserPreferencesCommand
            {
                UserId = currentUserId.Value,
                TimeZone = request.TimeZone,
                Language = request.Language,
                Theme = request.Theme
            };

            var result = await _mediator.Send(command);

            _logger.LogInformation("User preferences updated successfully: {UserId}", currentUserId.Value);

            return SuccessResponse(result, "Preferences updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user preferences");
            return ServerErrorResponse("An error occurred while updating your preferences");
        }
    }

    /// <summary>
    /// Uploads current user's profile photo
    /// POST /api/Users/profile/photo
    /// </summary>
    [HttpPost("profile/photo")]
    [ProducesResponseType(typeof(ApiResponseDto<string>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    public async Task<IActionResult> UploadProfilePhoto(IFormFile file)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return BadRequestResponse("User not authenticated");

            if (file == null || file.Length == 0)
                return BadRequestResponse("No file provided");

            var command = new UploadProfilePhotoCommand
            {
                UserId = currentUserId.Value,
                File = file
            };

            var photoUrl = await _mediator.Send(command);

            _logger.LogInformation("Profile photo uploaded successfully: {UserId}", currentUserId.Value);

            return SuccessResponse(photoUrl, "Profile photo uploaded successfully");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid file upload attempt");
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile photo");
            return ServerErrorResponse("An error occurred while uploading your profile photo");
        }
    }

    /// <summary>
    /// Removes current user's profile photo
    /// DELETE /api/Users/profile/photo
    /// </summary>
    [HttpDelete("profile/photo")]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    public async Task<IActionResult> RemoveProfilePhoto()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return BadRequestResponse("User not authenticated");

            var command = new RemoveProfilePhotoCommand
            {
                UserId = currentUserId.Value
            };

            await _mediator.Send(command);

            _logger.LogInformation("Profile photo removed successfully: {UserId}", currentUserId.Value);

            return SuccessResponse("Profile photo removed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing profile photo");
            return ServerErrorResponse("An error occurred while removing your profile photo");
        }
    }

    /// <summary>
    /// Gets available roles for assignment
    /// </summary>
    [HttpGet("roles")]
    [Authorize(Policy = Permissions.User.ReadAll)]
    [ProducesResponseType(typeof(ApiResponseDto<List<RoleDto>>), 200)]
    public async Task<IActionResult> GetAvailableRoles()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return BadRequestResponse("User not authenticated");

            var query = new GetAvailableRolesQuery { UserId = currentUserId.Value };
            var result = await _mediator.Send(query);

            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available roles");
            return ServerErrorResponse("An error occurred while retrieving available roles");
        }
    }
}