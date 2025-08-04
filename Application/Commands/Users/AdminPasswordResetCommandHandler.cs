using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Controllers;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Users
{
    /// <summary>
    /// Handler for AdminPasswordResetCommand
    /// </summary>
    public class AdminPasswordResetCommandHandler : IRequestHandler<AdminPasswordResetCommand, CommandResultDto<AdminPasswordResetResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly IAuthService _authService;
        private readonly ILogger<AdminPasswordResetCommandHandler> _logger;

        public AdminPasswordResetCommandHandler(
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            IAuthService authService,
            ILogger<AdminPasswordResetCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _authService = authService;
            _logger = logger;
        }

        public async Task<CommandResultDto<AdminPasswordResetResponseDto>> Handle(AdminPasswordResetCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing AdminPasswordResetCommand for UserId: {UserId} by User: {ResetBy}",
                    request.UserId, request.ResetBy);

                var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
                if (user == null)
                {
                    return CommandResultDto<AdminPasswordResetResponseDto>.Failure("User not found");
                }

                var resetByUser = await _unitOfWork.Users.GetByIdAsync(request.ResetBy, cancellationToken);
                if (resetByUser == null)
                {
                    return CommandResultDto<AdminPasswordResetResponseDto>.Failure("Invalid administrator user");
                }

                string newPassword;

                if (request.GenerateTemporaryPassword)
                {
                    newPassword = _passwordService.GenerateTemporaryPassword();
                    _logger.LogInformation("Generated temporary password for user: {UserId}", request.UserId);
                }
                else if (!string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    newPassword = request.NewPassword;

                    // Validate the new password
                    var passwordValidation = _passwordService.ValidatePassword(newPassword, user);
                    if (!passwordValidation.IsValid)
                    {
                        return CommandResultDto<AdminPasswordResetResponseDto>.Failure("New password does not meet requirements", passwordValidation.Errors);
                    }
                }
                else
                {
                    return CommandResultDto<AdminPasswordResetResponseDto>.Failure("Either provide a new password or enable temporary password generation");
                }

                // Save current password to history
                await _passwordService.SavePasswordHistoryAsync(user.Id, user.PasswordHash, user.PasswordSalt, cancellationToken);

                // Hash the new password
                var hashResult = _passwordService.HashPassword(newPassword);

                // Update user password
                user.ChangePassword(hashResult.Hash, hashResult.Salt);

                if (request.MustChangePassword)
                {
                    user.MustChangePassword = true;
                }

                // Update the user
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Revoke all existing tokens to force re-authentication
                await _authService.LogoutFromAllDevicesAsync(user.Id, "Administrative password reset", cancellationToken: cancellationToken);

                var reason = request.Reason ?? "Administrative password reset";
                _logger.LogInformation("Password reset completed for user: {UserId} by administrator: {ResetBy}, Reason: {Reason}",
                    request.UserId, request.ResetBy, reason);


                var responseDto = new AdminPasswordResetResponseDto
                {
                    NewPassword = newPassword,
                    MustChangePassword = request.MustChangePassword,
                    NotifyUser = request.NotifyUser,
                    Reason = reason
                };
                var result = CommandResultDto<AdminPasswordResetResponseDto>.Success(responseDto,"Password reset successfully");



                // TODO: Send notification to user if requested
                if (request.NotifyUser)
                {
                    // This would be implemented with email service in later chunks
                    _logger.LogInformation("Password reset notification requested for user: {UserId}", request.UserId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AdminPasswordResetCommand for UserId: {UserId}", request.UserId);
                return CommandResultDto<AdminPasswordResetResponseDto>.Failure("An error occurred while resetting the password");
            }
        }
    }

}
