using MediatR;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Application.DTOs.Auth;

namespace VisitorManagementSystem.Api.Application.Commands.Auth;

/// <summary>
/// Command for initiating password reset
/// </summary>
public class InitiatePasswordResetCommand : IRequest<PasswordResetResultDto>
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Command for completing password reset
/// </summary>
public class ResetPasswordCommand : IRequest<PasswordResetResultDto>
{
    public string Email { get; set; } = string.Empty;
    public string ResetToken { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Handler for initiate password reset command
/// </summary>
public class InitiatePasswordResetCommandHandler : IRequestHandler<InitiatePasswordResetCommand, PasswordResetResultDto>
{
    private readonly IAuthService _authService;
    private readonly ILogger<InitiatePasswordResetCommandHandler> _logger;

    public InitiatePasswordResetCommandHandler(
        IAuthService authService,
        ILogger<InitiatePasswordResetCommandHandler> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task<PasswordResetResultDto> Handle(InitiatePasswordResetCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing password reset initiation for email: {Email}", request.Email);

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return new PasswordResetResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = "Email address is required.",
                    Errors = new List<string> { "Invalid email address" }
                };
            }

            var result = await _authService.InitiatePasswordResetAsync(request.Email, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Password reset initiated for email: {Email}", request.Email);
            }
            else
            {
                _logger.LogWarning("Password reset initiation failed for email: {Email}: {Error}",
                    request.Email, result.ErrorMessage);
            }

            // Always return success for security reasons (don't reveal if email exists)
            return new PasswordResetResultDto
            {
                IsSuccess = true,
                EmailSent = true,
                ErrorMessage = null,
                Errors = new List<string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing password reset initiation for email: {Email}", request.Email);
            return new PasswordResetResultDto
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred while processing the password reset request.",
                Errors = new List<string> { "Internal server error" }
            };
        }
    }
}

