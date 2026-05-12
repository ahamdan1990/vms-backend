namespace VisitorManagementSystem.Api.Application.Services.VideoProcessing;

public interface IFfmpegProcessRunner
{
    Task<FfmpegProcessResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        int timeoutSeconds,
        int? maxStandardOutputBytes = null,
        CancellationToken cancellationToken = default);
}
