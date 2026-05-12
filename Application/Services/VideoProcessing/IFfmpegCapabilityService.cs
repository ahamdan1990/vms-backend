using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Services.VideoProcessing;

public interface IFfmpegCapabilityService
{
    Task<FfmpegCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);

    Task<bool> IsHardwareAccelerationAvailableAsync(
        FfmpegHardwareAcceleration hardwareAcceleration,
        CancellationToken cancellationToken = default);
}
