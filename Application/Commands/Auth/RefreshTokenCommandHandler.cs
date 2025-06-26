using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Auth;
using VisitorManagementSystem.Api.Application.Services.Auth;

namespace VisitorManagementSystem.Api.Application.Commands.Auth
{

    /// <summary>
    /// Handler for refresh token command
    /// </summary>
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthenticationResult>
    {
        private readonly IAuthService _authService;
        private readonly ILogger<RefreshTokenCommandHandler> _logger;

        public RefreshTokenCommandHandler(
            IAuthService authService,
            ILogger<RefreshTokenCommandHandler> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public async Task<AuthenticationResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing refresh token request from IP: {IpAddress}", request.IpAddress);

                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    _logger.LogWarning("Refresh token command received with empty token");
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Refresh token is required.",
                        Errors = new List<string> { "Invalid refresh token" }
                    };
                }

                // Prepare refresh token request DTO
                var refreshRequest = new RefreshTokenRequestDto
                {
                    RefreshToken = request.RefreshToken
                };

                // Attempt token refresh
                var result = await _authService.RefreshTokenAsync(
                    refreshRequest,
                    request.IpAddress,
                    request.UserAgent,
                    request.DeviceFingerprint,
                    cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogDebug("Token refresh successful for user: {UserId}", result.User?.Id);
                }
                else
                {
                    _logger.LogWarning("Token refresh failed: {Error}", result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refresh token command");
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred during token refresh. Please login again.",
                    Errors = new List<string> { "Token refresh failed" }
                };
            }
        }
    }
}
