namespace VisitorManagementSystem.Api.Application.Services.FaceDetection;

/// <summary>
/// Forces local face engines to initialize during API startup so failures are visible before live frames arrive.
/// </summary>
public sealed class FaceEngineWarmupService : IHostedService
{
    private readonly LuxandFaceService _luxandFaceService;
    private readonly ILogger<FaceEngineWarmupService> _logger;

    public FaceEngineWarmupService(
        LuxandFaceService luxandFaceService,
        ILogger<FaceEngineWarmupService> logger)
    {
        _luxandFaceService = luxandFaceService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Face engine startup status. LuxandAvailable={LuxandAvailable}, LuxandStatus={LuxandStatus}, LuxandReturnCode={LuxandReturnCode}, LuxandError={LuxandError}",
            _luxandFaceService.IsAvailable,
            _luxandFaceService.InitializationStatus,
            _luxandFaceService.LastReturnCode,
            _luxandFaceService.InitializationError);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
