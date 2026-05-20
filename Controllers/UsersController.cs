// Controllers/UsersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using VisitorManagementSystem.Api.Application.Commands.Users;
using VisitorManagementSystem.Api.Application.Queries.Users;
using VisitorManagementSystem.Api.Application.DTOs.FaceDetection;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.Services.Common;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Application.DTOs.Auth;
using VisitorManagementSystem.Api.Application.Services.Users;
using VisitorManagementSystem.Api.Application.Services.FaceDetection;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

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
    private readonly IWebHostEnvironment _environment;
    private readonly IUserImportService _userImportService;
    private readonly IFaceTemplateEnrollmentService _enrollmentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUrlResolverService _urlResolver;

    public UsersController(
        IMediator mediator,
        ILogger<UsersController> logger,
        IWebHostEnvironment environment,
        IUserImportService userImportService,
        IFaceTemplateEnrollmentService enrollmentService,
        IUnitOfWork unitOfWork,
        IUrlResolverService urlResolver)
    {
        _mediator = mediator;
        _logger = logger;
        _environment = environment;
        _userImportService = userImportService;
        _enrollmentService = enrollmentService;
        _unitOfWork = unitOfWork;
        _urlResolver = urlResolver;
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
    /// Searches hosts across local staff and the corporate directory.
    /// </summary>
    [HttpGet("host-search")]
    [Authorize(Policy = Permissions.WalkIn.Register)]
    [ProducesResponseType(typeof(ApiResponseDto<List<HostSearchResultDto>>), 200)]
    public async Task<IActionResult> SearchHosts(
        [FromQuery] string searchTerm,
        [FromQuery] int maxResults = 10,
        [FromQuery] bool includeDirectory = true)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequestResponse("Search term is required");
        }

        try
        {
            var query = new SearchHostsQuery
            {
                SearchTerm = searchTerm,
                MaxResults = maxResults,
                IncludeDirectory = includeDirectory
            };

            var result = await _mediator.Send(query);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching hosts with term {SearchTerm}", searchTerm);
            return ServerErrorResponse("An error occurred while searching hosts");
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
    /// Ensures the specified host exists by provisioning them from LDAP if needed.
    /// </summary>
    [HttpPost("ensure-host")]
    [Authorize(Policy = Permissions.WalkIn.Register)]
    [ProducesResponseType(typeof(ApiResponseDto<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    public async Task<IActionResult> EnsureHostFromDirectory([FromBody] EnsureHostFromDirectoryDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationError(GetModelStateErrors(), "Validation failed");

            var command = new EnsureDirectoryHostCommand
            {
                Identifier = request.Identifier?.Trim() ?? string.Empty,
                Email = request.Email?.Trim(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = request.Role
            };

            var result = await _mediator.Send(command);
            return SuccessResponse(result, "Host ready to receive visitors");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Directory provisioning failed for identifier {Identifier}", request.Identifier);
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring host {Identifier}", request.Identifier);
            return ServerErrorResponse("An error occurred while preparing the host");
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

            var command = new CreateUserCommand
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,

                // Enhanced phone fields
                PhoneNumber = request.PhoneNumber,
                PhoneCountryCode = request.PhoneCountryCode,
                PhoneType = request.PhoneType,

                RoleName = request.Role,
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
                RequiresApprovalOverride = request.RequiresApprovalOverride,
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

                RoleName = request.Role,
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
                RequiresApprovalOverride = request.RequiresApprovalOverride,

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
    [ProducesResponseType(typeof(ApiResponseDto<List<UserRoleDto>>), 200)]
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

    // ══════════════════════════════════════════════════════════════════════════
    // IMPORT ENDPOINTS
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Downloads the blank Excel import template (.xlsx).
    /// GET /api/Users/import/template
    /// </summary>
    [HttpGet("import/template")]
    [Authorize(Policy = Permissions.User.Import)]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> DownloadImportTemplate(
        [FromQuery] string format = "xlsx",
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
            {
                var csv = await _userImportService.GenerateCsvImportTemplateAsync(cancellationToken);
                return File(csv, "text/csv", "import-users-template.csv");
            }

            var xlsx = await _userImportService.GenerateImportTemplateAsync(cancellationToken);
            return File(xlsx,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "import-users-template.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating import template");
            return ServerErrorResponse("An error occurred while generating the import template");
        }
    }

    /// <summary>
    /// Validates an uploaded file and returns a row-level report without creating any users.
    /// POST /api/Users/import/validate
    /// </summary>
    [HttpPost("import/validate")]
    [Authorize(Policy = Permissions.User.Import)]
    [RequestSizeLimit(5_242_880)] // 5 MB
    [ProducesResponseType(typeof(ApiResponseDto<ImportValidationResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    public async Task<IActionResult> ValidateImportFile(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return BadRequestResponse("User not authenticated");

            // Run as dry-run through the full import pipeline
            var command = new ImportUsersCommand
            {
                File = file,
                SkipInvalidRows = true,
                DryRun = true,
                ImportedBy = currentUserId.Value
            };

            var importResult = await _mediator.Send(command, cancellationToken);

            var validationResult = new ImportValidationResultDto
            {
                TotalRows   = importResult.TotalRows,
                ValidRows   = importResult.Results.Count(r => r.Status != ImportRowStatus.Skipped && r.FieldErrors.Count == 0),
                InvalidRows = importResult.Results.Count(r => r.Status == ImportRowStatus.Skipped || r.FieldErrors.Count > 0),
                FileErrors  = importResult.Results
                    .Where(r => r.RowNumber == 0)
                    .SelectMany(r => r.FieldErrors.Select(fe => fe.Message))
                    .ToList(),
                RowResults  = importResult.Results
            };

            return SuccessResponse(validationResult, "File validated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating import file");
            return ServerErrorResponse("An error occurred while validating the import file");
        }
    }

    /// <summary>
    /// Batch-checks which emails and employee IDs already exist in the database.
    /// POST /api/Users/check-duplicates
    /// </summary>
    [HttpPost("check-duplicates")]
    [Authorize(Policy = Permissions.User.Import)]
    [ProducesResponseType(typeof(ApiResponseDto<CheckDuplicatesResultDto>), 200)]
    public async Task<IActionResult> CheckDuplicates(
        [FromBody] CheckDuplicatesRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _userImportService.CheckDuplicatesAsync(
                request.Emails, request.EmployeeIds, cancellationToken);

            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking duplicates");
            return ServerErrorResponse("An error occurred while checking for duplicates");
        }
    }

    /// <summary>
    /// Imports users from an uploaded .xlsx or .csv file.
    /// POST /api/Users/import
    /// </summary>
    [HttpPost("import")]
    [Authorize(Policy = Permissions.User.Import)]
    [RequestSizeLimit(5_242_880)] // 5 MB
    [ProducesResponseType(typeof(ApiResponseDto<ImportUsersResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    public async Task<IActionResult> ImportUsers(
        IFormFile file,
        [FromQuery] bool skipInvalidRows = true,
        [FromQuery] bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return BadRequestResponse("User not authenticated");

            var command = new ImportUsersCommand
            {
                File           = file,
                SkipInvalidRows = skipInvalidRows,
                DryRun         = dryRun,
                ImportedBy     = currentUserId.Value
            };

            var result = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation(
                "Import completed by {UserId}. Total={Total}, Created={Created}, Skipped={Skipped}, Failed={Failed}",
                currentUserId.Value, result.TotalRows, result.CreatedCount,
                result.SkippedCount, result.FailedCount);

            var message = dryRun
                ? $"Dry run complete. {result.TotalRows - result.SkippedCount} rows are valid."
                : $"Import complete. {result.CreatedCount} users created, {result.SkippedCount} skipped, {result.FailedCount} failed.";

            return SuccessResponse(result, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user import");
            return ServerErrorResponse("An error occurred during the import");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // FACE ENROLLMENT ENDPOINTS
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Uploads a profile photo for a user and enrolls the face as their primary template.
    /// POST /api/users/{id}/photo
    /// </summary>
    [HttpPost("{id:int}/photo")]
    [Authorize(Policy = Permissions.User.Update)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> UploadUserPhoto(
        int id,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequestResponse("No file provided.");

            const long maxBytes = 10 * 1024 * 1024;
            if (file.Length > maxBytes)
                return BadRequestResponse("File exceeds the 10 MB limit.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                return BadRequestResponse("Only JPG, PNG, and WebP images are accepted.");

            byte[] imageBytes;
            await using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms, cancellationToken);
                imageBytes = ms.ToArray();
            }

            var result = await _enrollmentService.EnrollUserPhotoAsync(
                id, imageBytes, file.FileName, cancellationToken);

            if (result.Message?.Contains("not found") == true)
                return NotFoundResponse($"User {id} not found.");

            if (!result.Success && !result.Skipped)
                return BadRequestResponse(result.Message ?? "Photo upload failed.");

            _logger.LogInformation(
                "Profile photo uploaded for user {UserId}. FaceDetected={FaceDetected}",
                id, result.Success);

            return SuccessResponse(new
            {
                faceDetected = result.Success,
                faceRecognitionEnabled = result.Success,
                message = result.Message
            }, "Profile photo uploaded successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo for user {UserId}", id);
            return ServerErrorResponse("An error occurred while uploading the photo.");
        }
    }

    /// <summary>
    /// Removes the profile photo for a user and deactivates their face templates.
    /// DELETE /api/users/{id}/photo
    /// </summary>
    [HttpDelete("{id:int}/photo")]
    [Authorize(Policy = Permissions.User.Update)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> RemoveUserPhoto(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new RemoveProfilePhotoCommand { UserId = id };
            await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Profile photo removed for user {UserId}", id);
            return SuccessResponse("Profile photo removed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing photo for user {UserId}", id);
            return ServerErrorResponse("An error occurred while removing the photo.");
        }
    }

    // ── Face Template Management ──────────────────────────────────────────────

    /// <summary>GET /api/users/{id}/face-templates</summary>
    [HttpGet("{id:int}/face-templates")]
    [Authorize(Policy = Permissions.User.Read)]
    public async Task<ActionResult<List<FaceTemplateDto>>> GetUserFaceTemplates(
        int id, CancellationToken cancellationToken = default)
    {
        var templates = await _unitOfWork.Repository<FaceTemplate>().GetAsync(
            t => t.UserId == id && !t.IsDeleted,
            cancellationToken);

        return Ok(templates.Select(MapTemplateDto).ToList());
    }

    /// <summary>POST /api/users/{id}/face-templates — enroll without changing profile photo</summary>
    [HttpPost("{id:int}/face-templates")]
    [Authorize(Policy = Permissions.User.Update)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<FaceTemplateEnrollmentItemResultDto>> AddUserFaceTemplate(
        int id,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var uploadDir = Path.Combine(webRoot, "uploads", "users", id.ToString());
        Directory.CreateDirectory(uploadDir);
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp")) ext = ".jpg";
        var fileName = $"template_{id}_{Guid.NewGuid():N}{ext}";
        await System.IO.File.WriteAllBytesAsync(Path.Combine(uploadDir, fileName), bytes, cancellationToken);
        var sourcePath = $"uploads/users/{id}/{fileName}";

        var result = await _enrollmentService.EnrollAdditionalFaceAsync(
            "Staff", id, bytes, sourcePath: sourcePath, source: "Upload", cancellationToken: cancellationToken);

        if (!result.Success && !result.Skipped)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }

    /// <summary>DELETE /api/users/{id}/face-templates/{templateId}</summary>
    [HttpDelete("{id:int}/face-templates/{templateId:int}")]
    [Authorize(Policy = Permissions.User.Update)]
    public async Task<IActionResult> DeleteUserFaceTemplate(
        int id, int templateId, CancellationToken cancellationToken = default)
    {
        var template = await _unitOfWork.Repository<FaceTemplate>()
            .GetByIdAsync(templateId, cancellationToken);

        if (template == null || template.UserId != id || template.IsDeleted)
            return NotFound(new { message = $"Face template {templateId} not found for user {id}." });

        template.SoftDelete(GetCurrentUserId() ?? 0);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>PUT /api/users/{id}/face-templates/{templateId}/set-primary</summary>
    [HttpPut("{id:int}/face-templates/{templateId:int}/set-primary")]
    [Authorize(Policy = Permissions.User.Update)]
    public async Task<IActionResult> SetPrimaryUserFaceTemplate(
        int id, int templateId, CancellationToken cancellationToken = default)
    {
        var target = await _unitOfWork.Repository<FaceTemplate>()
            .GetByIdAsync(templateId, cancellationToken);

        if (target == null || target.UserId != id || target.IsDeleted)
            return NotFound(new { message = $"Face template {templateId} not found for user {id}." });

        var allTemplates = await _unitOfWork.Repository<FaceTemplate>().GetAsync(
            t => t.UserId == id && t.Engine == target.Engine && !t.IsDeleted,
            cancellationToken);

        foreach (var t in allTemplates)
            t.IsPrimary = t.Id == templateId;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>GET /api/users/{id}/candidate-snapshots — high-quality camera snapshots suitable for face template enrollment</summary>
    [HttpGet("{id:int}/candidate-snapshots")]
    [Authorize(Policy = Permissions.User.Read)]
    public async Task<ActionResult<List<CandidateSnapshotDto>>> GetUserCandidateSnapshots(
        int id, CancellationToken cancellationToken = default)
    {
        var candidates = await _unitOfWork.FaceEvents
            .GetCandidateSnapshotsAsync("Staff", id, 5, cancellationToken);

        return Ok(candidates.Select(e => new CandidateSnapshotDto
        {
            FaceEventId = e.Id,
            SnapshotUrl = !string.IsNullOrEmpty(e.SnapshotPath)
                ? _urlResolver.GetAbsoluteUrl(e.SnapshotPath) : null,
            Similarity = e.Similarity,
            Confidence = e.Confidence,
            CameraName = e.CameraName,
            CapturedAt = e.CapturedAt
        }));
    }

    private FaceTemplateDto MapTemplateDto(FaceTemplate t) =>
        new()
        {
            Id = t.Id,
            PersonType = t.PersonType,
            PersonId = t.PersonId,
            Engine = t.Engine,
            IsPrimary = t.IsPrimary,
            QualityScore = t.QualityScore,
            SourceImagePath = t.SourceImagePath,
            SourceImageUrl = !string.IsNullOrEmpty(t.SourceImagePath)
                ? _urlResolver.GetAbsoluteUrl(t.SourceImagePath)
                : null,
            Source = t.Source,
            LastMatchedAt = t.LastMatchedAt,
            MatchCount = t.MatchCount,
            CreatedOn = t.CreatedOn,
            IsActive = t.IsActive
        };
}
