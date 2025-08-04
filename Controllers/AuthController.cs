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

    public AuthController(IMediator mediator, IAuthService authService, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates user and sets secure cookies
    /// </summary>
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
                return ValidationError(GetModelStateErrors(), "Validation failed");

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

                return SuccessResponse(loginResponse, "Login successful");
            }

            return BadRequestResponse(result.Errors, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login attempt for {Email}", request.Email);
            return ServerErrorResponse("An error occurred during login");
        }
    }

    /// <summary>
    /// Refreshes access token using refresh token from cookies
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponseDto<LoginResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto? request = null)
    {
        try
        {
            var tokenInfo = _authService.ExtractTokensFromCookies(Request);
            if (tokenInfo?.RefreshToken == null)
                return BadRequestResponse("No refresh token found in cookies");

            var command = new RefreshTokenCommand
            {
                RefreshToken = tokenInfo.RefreshToken,
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent(),
                DeviceFingerprint = request?.DeviceFingerprint ?? GenerateDeviceFingerprint()
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _authService.SetAuthenticationCookies(Response, result, false);

                var loginResponse = new LoginResponseDto
                {
                    IsSuccess = true,
                    User = result.User,
                    RequiresPasswordChange = result.RequiresPasswordChange
                };

                return SuccessResponse(loginResponse, "Token refreshed successfully");
            }

            _authService.ClearAuthenticationCookies(Response, false);
            return BadRequestResponse(result.Errors, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return ServerErrorResponse("An error occurred during token refresh");
        }
    }

    /// <summary>
    /// Logs out user and clears authentication cookies
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    public async Task<IActionResult> Logout([FromQuery] bool logoutFromAllDevices = false)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return BadRequestResponse("User not authenticated");

            var tokenInfo = _authService.ExtractTokensFromCookies(Request);
            var command = new LogoutCommand
            {
                UserId = userId.Value,
                RefreshToken = tokenInfo?.RefreshToken,
                IpAddress = GetClientIpAddress(),
                LogoutFromAllDevices = logoutFromAllDevices
            };

            var result = await _mediator.Send(command);
            _authService.ClearAuthenticationCookies(Response, false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("User {UserId} logged out successfully. Tokens revoked: {TokensRevoked}",
                    userId.Value, result.TokensRevoked);

                return SuccessResponse($"Logout successful. {result.TokensRevoked} session(s) terminated.");
            }

            return SuccessResponse("Logout completed (cookies cleared)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            _authService.ClearAuthenticationCookies(Response, false);
            return ServerErrorResponse("Logout completed with errors");
        }
    }

    /// <summary>
    /// Changes user password
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationError(GetModelStateErrors(), "Validation failed");

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return BadRequestResponse("User not authenticated");

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
                if (result.RequiresReauthentication)
                    _authService.ClearAuthenticationCookies(Response, false);

                _logger.LogInformation("Password changed successfully for user: {UserId}", userId.Value);

                return SuccessResponse(result.RequiresReauthentication
                    ? "Password changed successfully. Please login again."
                    : "Password changed successfully");
            }

            return BadRequestResponse(result.Errors, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change");
            return ServerErrorResponse("An error occurred while changing password");
        }
    }

    /// <summary>
    /// Initiates password reset process
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationError(GetModelStateErrors(), "Validation failed");

            var command = new InitiatePasswordResetCommand { Email = request.Email };
            await _mediator.Send(command);

            return SuccessResponse("If an account with that email exists, password reset instructions have been sent.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset initiation");
            return SuccessResponse("If an account with that email exists, password reset instructions have been sent.");
        }
    }

    /// <summary>
    /// Resets password using reset token
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationError(GetModelStateErrors(), "Validation failed");

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
                return SuccessResponse("Password reset successful. You can now login with your new password.");
            }

            return BadRequestResponse(result.Errors, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return ServerErrorResponse("An error occurred while resetting password");
        }
    }

    /// <summary>
    /// Gets current user information
    /// </summary>
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
                return UnauthorizedResponse("User not authenticated");

            var query = new GetCurrentUserQuery { UserId = userId.Value };
            var currentUser = await _mediator.Send(query);

            if (currentUser == null)
                return UnauthorizedResponse("User not found or account invalid");

            return SuccessResponse(currentUser, "User found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return ServerErrorResponse("An error occurred while retrieving user information");
        }
    }

    /// <summary>
    /// Gets user permissions
    /// </summary>
    [HttpGet("permissions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDto<UserPermissionsDto>), 200)]
    public async Task<IActionResult> GetUserPermissions()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return BadRequestResponse("User not authenticated");

            var query = new GetUserPermissionsQuery { UserId = userId.Value };
            var permissions = await _mediator.Send(query);

            return SuccessResponse(permissions, "Permissions found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user permissions");
            return ServerErrorResponse("An error occurred while retrieving permissions");
        }
    }

    /// <summary>
    /// Validates current access token
    /// </summary>
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
                return SuccessResponse(new TokenValidationDto { IsValid = false, Reason = "No access token found" });
            }

            var query = new ValidateTokenQuery { AccessToken = tokenInfo.AccessToken };
            var result = await _mediator.Send(query);

            var validationDto = new TokenValidationDto
            {
                IsValid = result.IsValid,
                UserId = result.UserId,
                UserEmail = result.UserEmail,
                Expiry = result.Expiry,
                Reason = result.ErrorMessage,
                IsExpired = result.IsExpired ?? false,
                IsNearExpiry = result.IsNearExpiry ?? false
            };

            return SuccessResponse(validationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return SuccessResponse(new TokenValidationDto { IsValid = false, Reason = "Token validation failed" });
        }
    }

    /// <summary>
    /// Gets user's active sessions
    /// </summary>
    [HttpGet("sessions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDto<List<UserSessionDto>>), 200)]
    public async Task<IActionResult> GetUserSessions()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return BadRequestResponse("User not authenticated");

            var sessions = await _authService.GetUserSessionsAsync(userId.Value);
            return SuccessResponse(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user sessions");
            return ServerErrorResponse("An error occurred while retrieving sessions");
        }
    }

    /// <summary>
    /// Terminates a specific session
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 200)]
    public async Task<IActionResult> TerminateSession(int sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return BadRequestResponse("User not authenticated");

            var result = await _authService.TerminateSessionAsync(userId.Value, sessionId);

            if (result)
                return SuccessResponse("Session terminated successfully");

            return BadRequestResponse("Failed to terminate session");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating session {SessionId}", sessionId);
            return ServerErrorResponse("An error occurred while terminating the session");
        }
    }

    /// <summary>
    /// Debug endpoint to view user claims
    /// </summary>
    [HttpGet("debug-claims")]
    [Authorize]
    public IActionResult DebugClaims()
    {
        var claims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();
        var userId = GetCurrentUserId();

        var debugInfo = new
        {
            IsAuthenticated = User.Identity?.IsAuthenticated,
            UserId = userId,
            Claims = claims,
            Identity = User.Identity?.Name,
            AuthenticationType = User.Identity?.AuthenticationType
        };

        return SuccessResponse(debugInfo, "Debug info");
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