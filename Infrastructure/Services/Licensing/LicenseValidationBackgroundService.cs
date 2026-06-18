using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.Services.Licensing;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Infrastructure.Services.Licensing;

public class LicenseValidationBackgroundService : BackgroundService
{
    private static readonly TimeSpan ValidationInterval = TimeSpan.FromHours(1);

    private readonly ILicenseValidatorService _licenseValidator;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<LicenseValidationBackgroundService> _logger;

    public LicenseValidationBackgroundService(
        ILicenseValidatorService licenseValidator,
        IHostApplicationLifetime lifetime,
        IHostEnvironment environment,
        ILogger<LicenseValidationBackgroundService> logger)
    {
        _licenseValidator = licenseValidator;
        _lifetime = lifetime;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_environment.IsDevelopment())
        {
            _logger.LogDebug("License background validation disabled in Development environment.");
            return;
        }

        // Initial delay to let the app finish starting up
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await _licenseValidator.ValidateCurrentLicenseAsync(stoppingToken);

                if (!result.IsValid && result.Status != LicenseStatus.NotActivated)
                {
                    _logger.LogCritical(
                        "Periodic license validation failed (Status: {Status}, Reason: {Reason}). Application is shutting down.",
                        result.Status, result.FailureReason);

                    // Give logs a moment to flush
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                    _lifetime.StopApplication();
                    return;
                }

                if (result.Status == LicenseStatus.NotActivated)
                {
                    _logger.LogWarning("License not activated — system is running in restricted mode.");
                }
                else
                {
                    _logger.LogDebug("Periodic license validation passed (Type: {Type}, Expires: {ExpiresAt})",
                        result.LicenseType, result.ExpiresAt?.ToString("yyyy-MM-dd"));
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during periodic license validation.");
            }

            await Task.Delay(ValidationInterval, stoppingToken);
        }
    }
}
