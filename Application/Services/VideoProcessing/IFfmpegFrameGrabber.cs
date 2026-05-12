using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Application.Services.VideoProcessing;

public interface IFfmpegFrameGrabber
{
    Task<FfmpegFrameCaptureResult> CaptureFrameAsync(
        Camera camera,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<FfmpegFrame> CaptureFramesAsync(
        Camera camera,
        CancellationToken cancellationToken = default);
}
