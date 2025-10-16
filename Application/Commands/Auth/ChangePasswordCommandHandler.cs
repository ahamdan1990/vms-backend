using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Auth;
using VisitorManagementSystem.Api.Application.Services.Auth;

namespace VisitorManagementSystem.Api.Application.Commands.Auth
{
    /// <summary>
    /// Handler for change password command
    /// </summary>
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, PasswordChangeResult>
    {
        private readonly IAuthService _authService;
        private readonly IPasswordService _passwordService;
        private readonly ILogger<ChangePasswordCommandHandler> _logger;

        public ChangePasswordCommandHandler(
            IAuthService authService,
            IPasswordService passwordService,
            ILogger<ChangePasswordCommandHandler> logger)
        {
            _authService = authService;
            _passwordService = passwordService;
            _logger = logger;
        }

        public async Task<PasswordChangeResult> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing password change request for user: {UserId}", request.UserId);

                // Validate input
                if (request.NewPassword != request.ConfirmPassword)
                {
                    _logger.LogWarning("Password confirmation mismatch for user: {UserId}", request.UserId);
                    return new PasswordChangeResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "New password and confirmation do not match.",
                        Errors = new List<string> { "Password confirmation mismatch" }
                    };
                }

                // Prepare change password DTO
                var changePasswordRequest = new ChangePasswordDto
                {
                    CurrentPassword = request.CurrentPassword,
                    NewPassword = request.NewPassword,
                    ConfirmPassword = request.ConfirmPassword
                };

                // Attempt password change
                var result = await _authService.ChangePasswordAsync(
                    request.UserId,
                    changePasswordRequest,
                    cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Password changed successfully for user: {UserId}", request.UserId);

                    // Optionally invalidate all sessions
                    if (request.InvalidateAllSessions)
                    {
                        _logger.LogDebug("Invalidating all sessions for user: {UserId} after password change", request.UserId);
                        await _authService.LogoutFromAllDevicesAsync(
                            request.UserId,
                            "Password changed - security measure",
                            null,
                            cancellationToken);

                        result.RequiresReauthentication = true;
                    }
                }
                else
                {
                    _logger.LogWarning("Password change failed for user: {UserId}: {Error}",
                        request.UserId, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing password change command for user: {UserId}", request.UserId);
                return new PasswordChangeResult
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while changing the password. Please try again.",
                    Errors = new List<string> { "Internal server error" }
                };
            }
        }
    }
}
