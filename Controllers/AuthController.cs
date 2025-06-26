// Controllers/AuthController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using VisitorManagementSystem.Api.Application.Commands.Auth;
using VisitorManagementSystem.Api.Application.Queries.Auth;
using VisitorManagementSystem.Api.Application.DTOs.Auth;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Infrastructure.Utilities;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Authentication controller for login, logout, and token management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IMediator mediator,
        IAuthService authService,
        ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates user and sets secure cookies
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication result</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponseDto<LoginResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 429)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse(
                    GetModelStateErrors(), "Validation failed"));
            }

            var command = new LoginCommand
            {
                Email = request.Email,
                Password = request.Password,
                RememberMe = request.RememberMe,
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent(),
                DeviceFingerprint = request.DeviceFingerprint ?? GenerateDeviceFingerprint()
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                // Set secure authentication cookies
                _authService.SetAuthenticationCookies(Response, result, false);

                var loginResponse = new LoginResponseDto
                {
                    IsSuccess = true,
                    User = result.User,
                    RequiresPasswordChange = result.RequiresPasswordChange,
                    RequiresTwoFactor = result.RequiresTwoFactor
                };

                _logger.LogInformation("User logged in successfully: {Email} from {IpAddress}",
                    request.Email, GetClientIpAddress());

                return Ok(ApiResponseDto<LoginResponseDto>.SuccessResponse(
                    loginResponse, "Login successful"));
            }

            var errorResponse = new LoginResponseDto
            {
                IsSuccess = false,
                ErrorMessage = result.ErrorMessage,
                LockoutTimeRemaining = result.LockoutTimeRemaining,
                Errors = result.Errors
            };

            return BadRequest(ApiResponseDto<LoginResponseDto>.ErrorResponse(
                result.Errors, result.ErrorMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login attempt for {Email}", request.Email);
            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "An error occurred during login"));
        }
    }

    /// <summary>
    /// Refreshes access token using refresh token from cookies
    /// </summary>
    /// <returns>New authentication tokens</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponseDto<LoginResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    public async Task<IActionResult> RefreshToken()
    {
        try
        {
            var tokenInfo = _authService.ExtractTokensFromCookies(Request);
            if (tokenInfo?.RefreshToken == null)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse(
                    "No refresh token found in cookies"));
            }

            var command = new RefreshTokenCommand
            {
                RefreshToken = tokenInfo.RefreshToken,
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent(),
                DeviceFingerprint = GenerateDeviceFingerprint()
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                // Update authentication cookies with new tokens
                _authService.SetAuthenticationCookies(Response, result, false);

                var loginResponse = new LoginResponseDto
                {
                    IsSuccess = true,
                    User = result.User,
                    RequiresPasswordChange = result.RequiresPasswordChange
                };

                return Ok(ApiResponseDto<LoginResponseDto>.SuccessResponse(
                    loginResponse, "Token refreshed successfully"));
            }

            // Clear invalid cookies
            _authService.ClearAuthenticationCookies(Response, false);

            return BadRequest(ApiResponseDto<object>.ErrorResponse(
                result.Errors, result.ErrorMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "An error occurred during token refresh"));
        }
    }

    /// <summary>
    /// Logs out user and clears authentication cookies
    /// </summary>
    /// <param name="logoutFromAllDevices">Whether to logout from all devices</param>
    /// <returns>Logout result</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    public async Task<IActionResult> Logout([FromQuery] bool logoutFromAllDevices = false)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse("User not authenticated"));
            }

            var tokenInfo = _authService.ExtractTokensFromCookies(Request);

            var command = new LogoutCommand
            {
                UserId = userId.Value,
                RefreshToken = tokenInfo?.RefreshToken,
                IpAddress = GetClientIpAddress(),
                LogoutFromAllDevices = logoutFromAllDevices
            };

            var result = await _mediator.Send(command);

            // Always clear cookies regardless of result
            _authService.ClearAuthenticationCookies(Response, false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("User {UserId} logged out successfully. Tokens revoked: {TokensRevoked}",
                    userId.Value, result.TokensRevoked);

                return Ok(ApiResponseDto<object>.SuccessResponse(null,
                    $"Logout successful. {result.TokensRevoked} session(s) terminated."));
            }

            return Ok(ApiResponseDto<object>.SuccessResponse(null,
                "Logout completed (cookies cleared)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");

            // Still clear cookies on error
            _authService.ClearAuthenticationCookies(Response, false);

            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "Logout completed with errors"));
        }
    }

    /// <summary>
    /// Changes user password
    /// </summary>
    /// <param name="request">Password change request</param>
    /// <returns>Password change result</returns>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse(
                    GetModelStateErrors(), "Validation failed"));
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse("User not authenticated"));
            }

            var command = new ChangePasswordCommand
            {
                UserId = userId.Value,
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword,
                ConfirmPassword = request.ConfirmPassword,
                InvalidateAllSessions = true
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                // Clear cookies if password change requires re-authentication
                if (result.RequiresReauthentication)
                {
                    _authService.ClearAuthenticationCookies(Response, false);
                }

                _logger.LogInformation("Password changed successfully for user: {UserId}", userId.Value);

                return Ok(ApiResponseDto<object>.SuccessResponse(null,
                    result.RequiresReauthentication
                        ? "Password changed successfully. Please login again."
                        : "Password changed successfully"));
            }

            return BadRequest(ApiResponseDto<object>.ErrorResponse(
                result.Errors, result.ErrorMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change");
            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "An error occurred while changing password"));
        }
    }

    /// <summary>
    /// Initiates password reset process
    /// </summary>
    /// <param name="request">Email for password reset</param>
    /// <returns>Password reset initiation result</returns>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse(
                    GetModelStateErrors(), "Validation failed"));
            }

            var command = new InitiatePasswordResetCommand
            {
                Email = request.Email
            };

            var result = await _mediator.Send(command);

            // Always return success for security (don't reveal if email exists)
            return Ok(ApiResponseDto<object>.SuccessResponse(null,
                "If an account with that email exists, password reset instructions have been sent."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset initiation");
            return Ok(ApiResponseDto<object>.SuccessResponse(null,
                "If an account with that email exists, password reset instructions have been sent."));
        }
    }

    /// <summary>
    /// Resets password using reset token
    /// </summary>
    /// <param name="request">Password reset request</param>
    /// <returns>Password reset result</returns>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse(
                    GetModelStateErrors(), "Validation failed"));
            }

            var command = new ResetPasswordCommand
            {
                Email = request.Email,
                ResetToken = request.Token,
                NewPassword = request.NewPassword,
                ConfirmPassword = request.ConfirmPassword
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Password reset completed successfully for email: {Email}", request.Email);

                return Ok(ApiResponseDto<object>.SuccessResponse(null,
                    "Password reset successful. You can now login with your new password."));
            }

            return BadRequest(ApiResponseDto<object>.ErrorResponse(
                result.Errors, result.ErrorMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "An error occurred while resetting password"));
        }
    }

    /// <summary>
    /// Gets current user information
    /// </summary>
    /// <returns>Current user details</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDto<CurrentUserDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 401)]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponseDto<object>.ErrorResponse("User not authenticated"));
            }

            var query = new GetCurrentUserQuery
            {
                UserId = userId.Value
            };

            var currentUser = await _mediator.Send(query);

            if (currentUser == null)
            {
                return Unauthorized(ApiResponseDto<object>.ErrorResponse("User not found or account invalid"));
            }

            return Ok(ApiResponseDto<CurrentUserDto>.SuccessResponse(currentUser));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "An error occurred while retrieving user information"));
        }
    }

    /// <summary>
    /// Gets user permissions
    /// </summary>
    /// <returns>User permissions</returns>
    [HttpGet("permissions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDto<UserPermissionsDto>), 200)]
    public async Task<IActionResult> GetUserPermissions()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse("User not authenticated"));
            }

            var query = new GetUserPermissionsQuery
            {
                UserId = userId.Value
            };

            var permissions = await _mediator.Send(query);

            return Ok(ApiResponseDto<UserPermissionsDto>.SuccessResponse(permissions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user permissions");
            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "An error occurred while retrieving permissions"));
        }
    }

    /// <summary>
    /// Validates current access token
    /// </summary>
    /// <returns>Token validation result</returns>
    [HttpPost("validate-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponseDto<TokenValidationDto>), 200)]
    public async Task<IActionResult> ValidateToken()
    {
        try
        {
            var tokenInfo = _authService.ExtractTokensFromCookies(Request);
            if (tokenInfo?.AccessToken == null)
            {
                return Ok(ApiResponseDto<TokenValidationDto>.SuccessResponse(
                    new TokenValidationDto { IsValid = false, Reason = "No access token found" }));
            }

            var query = new ValidateTokenQuery
            {
                AccessToken = tokenInfo.AccessToken
            };

            var result = await _mediator.Send(query);

            var validationDto = new TokenValidationDto
            {
                IsValid = result.IsValid,
                UserId = result.UserId,
                UserEmail = result.UserEmail,
                Expiry = result.Expiry,
                Reason = result.ErrorMessage,
                IsExpired = (bool)result.IsExpired,
                IsNearExpiry = (bool)result.IsNearExpiry
            };

            return Ok(ApiResponseDto<TokenValidationDto>.SuccessResponse(validationDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return Ok(ApiResponseDto<TokenValidationDto>.SuccessResponse(
                new TokenValidationDto { IsValid = false, Reason = "Token validation failed" }));
        }
    }

    /// <summary>
    /// Gets user's active sessions
    /// </summary>
    /// <returns>List of active sessions</returns>
    [HttpGet("sessions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDto<List<UserSessionDto>>), 200)]
    public async Task<IActionResult> GetUserSessions()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse("User not authenticated"));
            }

            var sessions = await _authService.GetUserSessionsAsync(userId.Value);

            return Ok(ApiResponseDto<List<UserSessionDto>>.SuccessResponse(sessions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user sessions");
            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "An error occurred while retrieving sessions"));
        }
    }

    /// <summary>
    /// Terminates a specific session
    /// </summary>
    /// <param name="sessionId">Session ID to terminate</param>
    /// <returns>Session termination result</returns>
    [HttpDelete("sessions/{sessionId}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    public async Task<IActionResult> TerminateSession(string sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return BadRequest(ApiResponseDto<object>.ErrorResponse("User not authenticated"));
            }

            var result = await _authService.TerminateSessionAsync(userId.Value, sessionId);

            if (result)
            {
                return Ok(ApiResponseDto<object>.SuccessResponse(null, "Session terminated successfully"));
            }

            return BadRequest(ApiResponseDto<object>.ErrorResponse("Failed to terminate session"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating session {SessionId}", sessionId);
            return StatusCode(500, ApiResponseDto<object>.ErrorResponse(
                "An error occurred while terminating the session"));
        }
    }


    [HttpGet("debug-claims")]
    [Authorize]
    public IActionResult DebugClaims()
    {
        var claims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();
        var userId = GetCurrentUserId();

        return Ok(new
        {
            IsAuthenticated = User.Identity?.IsAuthenticated,
            UserId = userId,
            Claims = claims,
            Identity = User.Identity?.Name,
            AuthenticationType = User.Identity?.AuthenticationType
        });
    }
    #region Private Methods

    private string GenerateDeviceFingerprint()
    {
        var userAgent = GetUserAgent() ?? "";
        var ipAddress = GetClientIpAddress() ?? "";
        return CryptoHelper.GenerateDeviceFingerprint(userAgent, ipAddress);
    }

    #endregion
}

/// <summary>
/// Forgot password request DTO
/// </summary>
public class ForgotPasswordRequestDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Token validation response DTO
/// </summary>
public class TokenValidationDto
{
    public bool IsValid { get; set; }
    public int? UserId { get; set; }
    public string? UserEmail { get; set; }
    public DateTime? Expiry { get; set; }
    public string? Reason { get; set; }
    public bool IsExpired { get; set; }
    public bool IsNearExpiry { get; set; }
}