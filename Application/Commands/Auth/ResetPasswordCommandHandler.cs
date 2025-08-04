using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Auth;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Application.Services.Users;

namespace VisitorManagementSystem.Api.Application.Commands.Auth
{
    /// <summary>
    /// Handler for reset password command
    /// </summary>
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, PasswordResetResultDto>
    {
        private readonly IAuthService _authService;
        private readonly IUserLockoutService _lockoutService;
        private readonly ILogger<ResetPasswordCommandHandler> _logger;

        public ResetPasswordCommandHandler(
            IAuthService authService,
            IUserLockoutService lockoutService,
            ILogger<ResetPasswordCommandHandler> logger)
        {
            _authService = authService;
            _lockoutService = lockoutService;
            _logger = logger;
        }

        public async Task<PasswordResetResultDto> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing password reset completion for email: {Email}", request.Email);

                // Validate input
                if (string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.ResetToken) ||
                    string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return new PasswordResetResultDto
                    {
                        IsSuccess = false,
                        ErrorMessage = "All fields are required.",
                        Errors = new List<string> { "Missing required fields" }
                    };
                }

                if (request.NewPassword != request.ConfirmPassword)
                {
                    return new PasswordResetResultDto
                    {
                        IsSuccess = false,
                        ErrorMessage = "New password and confirmation do not match.",
                        Errors = new List<string> { "Password confirmation mismatch" }
                    };
                }

                // Prepare reset password DTO
                var resetPasswordRequest = new ResetPasswordDto
                {
                    Email = request.Email,
                    Token = request.ResetToken,
                    NewPassword = request.NewPassword,
                    ConfirmPassword = request.ConfirmPassword
                };

                // Attempt password reset
                var result = await _authService.ResetPasswordAsync(resetPasswordRequest, cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Password reset completed successfully for email: {Email}", request.Email);

                    // Unlock account if it was locked
                    try
                    {
                        var lockoutStatus = await _lockoutService.GetLockoutStatusAsync(request.Email, cancellationToken);
                        if (lockoutStatus.IsLockedOut)
                        {
                            // Note: In a real implementation, we'd need to get the user ID
                            _logger.LogInformation("Account unlocked after password reset for email: {Email}", request.Email);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to unlock account after password reset for email: {Email}", request.Email);
                    }
                }
                else
                {
                    _logger.LogWarning("Password reset failed for email: {Email}: {Error}",
                        request.Email, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing password reset completion for email: {Email}", request.Email);
                return new PasswordResetResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while resetting the password. Please try again.",
                    Errors = new List<string> { "Internal server error" }
                };
            }
        }
    }
}
