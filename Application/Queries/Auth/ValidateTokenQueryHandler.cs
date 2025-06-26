using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Auth;
using VisitorManagementSystem.Api.Application.Services.Auth;

namespace VisitorManagementSystem.Api.Application.Queries.Auth
{
    /// <summary>
    /// Handler for validate token query
    /// </summary>
    public class ValidateTokenQueryHandler : IRequestHandler<ValidateTokenQuery, TokenValidationResult>
    {
        private readonly IAuthService _authService;
        private readonly ILogger<ValidateTokenQueryHandler> _logger;

        public ValidateTokenQueryHandler(
            IAuthService authService,
            ILogger<ValidateTokenQueryHandler> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public async Task<TokenValidationResult> Handle(ValidateTokenQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing validate token query");

                if (string.IsNullOrWhiteSpace(request.AccessToken))
                {
                    _logger.LogWarning("Validate token query received with empty token");
                    return new TokenValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Access token is required"
                    };
                }

                var result = await _authService.ValidateTokenAsync(request.AccessToken, cancellationToken);

                if (result.IsValid)
                {
                    _logger.LogDebug("Token validation successful for user: {UserId}", result.UserId);
                }
                else
                {
                    _logger.LogWarning("Token validation failed: {Error}", result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing validate token query");
                return new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Token validation failed due to an error"
                };
            }
        }
    }
}
