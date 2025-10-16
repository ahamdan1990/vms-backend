using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Auth;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Application.Services.Users;

namespace VisitorManagementSystem.Api.Application.Commands.Auth
{
    /// <summary>
    /// Handler for login command
    /// </summary>
    public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthenticationResult>
    {
        private readonly IAuthService _authService;
        private readonly IUserLockoutService _lockoutService;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            IAuthService authService,
            IUserLockoutService lockoutService,
            ILogger<LoginCommandHandler> logger)
        {
            _authService = authService;
            _lockoutService = lockoutService;
            _logger = logger;
        }

        public async Task<AuthenticationResult> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing login request for email: {Email}", request.Email);

                // Check if account is locked out
                var lockoutStatus = await _lockoutService.GetLockoutStatusAsync(request.Email, cancellationToken);
                if (lockoutStatus.IsLockedOut)
                {
                    _logger.LogWarning("Login attempt for locked account: {Email}", request.Email);
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Account is locked out due to multiple failed login attempts.",
                        LockoutTimeRemaining = lockoutStatus.TimeRemaining,
                        Errors = new List<string> { $"Account locked until {lockoutStatus.LockoutEnd:yyyy-MM-dd HH:mm:ss}" }
                    };
                }

                // Check IP rate limiting
                if (!string.IsNullOrEmpty(request.IpAddress))
                {
                    var rateLimitStatus = await _lockoutService.CheckIpRateLimitAsync(request.IpAddress, cancellationToken);
                    if (rateLimitStatus.IsRateLimited)
                    {
                        _logger.LogWarning("Rate limit exceeded for IP: {IpAddress}", request.IpAddress);
                        return new AuthenticationResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "Too many login attempts from this IP address. Please try again later.",
                            Errors = new List<string> { $"Rate limited until {rateLimitStatus.NextAllowedTime:yyyy-MM-dd HH:mm:ss}" }
                        };
                    }
                }

                // Prepare login request DTO
                var loginRequest = new LoginRequestDto
                {
                    Email = request.Email,
                    Password = request.Password,
                    RememberMe = request.RememberMe
                };

                // Attempt authentication
                var result = await _authService.LoginAsync(
                    loginRequest,
                    request.IpAddress,
                    request.UserAgent,
                    request.DeviceFingerprint,
                    cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Successful login for user: {Email}", request.Email);
                }
                else
                {
                    _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing login command for email: {Email}", request.Email);
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred during login. Please try again.",
                    Errors = new List<string> { "Internal server error" }
                };
            }
        }
    }
}