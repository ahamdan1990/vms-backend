using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Auth;
using VisitorManagementSystem.Api.Application.Services.Auth;

namespace VisitorManagementSystem.Api.Application.Commands.Auth
{
    /// <summary>
    /// Handler for logout command
    /// </summary>
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, LogoutResult>
    {
        private readonly IAuthService _authService;
        private readonly ILogger<LogoutCommandHandler> _logger;

        public LogoutCommandHandler(
            IAuthService authService,
            ILogger<LogoutCommandHandler> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public async Task<LogoutResult> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing logout request for user: {UserId}", request.UserId);

                LogoutResult result;

                if (request.LogoutFromAllDevices)
                {
                    _logger.LogInformation("Logging out user {UserId} from all devices", request.UserId);
                    result = await _authService.LogoutFromAllDevicesAsync(
                        request.UserId,
                        "User logout from all devices",
                        request.IpAddress,
                        cancellationToken);
                }
                else
                {
                    result = await _authService.LogoutAsync(
                        request.UserId,
                        request.RefreshToken,
                        request.IpAddress,
                        cancellationToken);
                }

                if (result.IsSuccess)
                {
                    _logger.LogInformation("User {UserId} logged out successfully. Tokens revoked: {TokensRevoked}",
                        request.UserId, result.TokensRevoked);
                }
                else
                {
                    _logger.LogWarning("Logout failed for user {UserId}: {Message}", request.UserId, result.Message);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing logout command for user: {UserId}", request.UserId);
                return new LogoutResult
                {
                    IsSuccess = false,
                    Message = "An error occurred during logout.",
                    TokensRevoked = 0
                };
            }
        }
    }
}
