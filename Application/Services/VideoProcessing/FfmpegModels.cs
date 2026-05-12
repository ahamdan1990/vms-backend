using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Services.VideoProcessing;

/// <summary>
/// FFmpeg runtime options.
/// </summary>
public sealed class FfmpegOptions
{
    public const string SectionName = "FFmpeg";

    public string? Directory { get; set; }
    public string? FFmpegPath { get; set; }
    public string? FFprobePath { get; set; }
    public int DefaultFrameCaptureTimeoutSeconds { get; set; } = 15;
    public int MaxCapturedFrameBytes { get; set; } = 10 * 1024 * 1024;
    public int CapabilityCacheSeconds { get; set; } = 300;
    public int ProcessStopTimeoutMs { get; set; } = 2000;
}

/// <summary>
/// FFmpeg hardware and binary availability information.
/// </summary>
public sealed class FfmpegCapabilities
{
    public bool IsFfmpegAvailable { get; set; }
    public string FfmpegPath { get; set; } = string.Empty;
    public IReadOnlyList<string> HardwareAccelerations { get; set; } = Array.Empty<string>();
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool HasCuda => HardwareAccelerations.Any(
        h => string.Equals(h, "cuda", StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Captured frame payload from FFmpeg.
/// </summary>
public sealed class FfmpegFrame
{
    public int CameraId { get; set; }
    public int SequenceNumber { get; set; }
    public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "image/jpeg";
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Result of one FFmpeg frame capture attempt.
/// </summary>
public sealed class FfmpegFrameCaptureResult
{
    public bool IsSuccess { get; set; }
    public byte[]? FrameBytes { get; set; }
    public string ContentType { get; set; } = "image/jpeg";
    public string? ErrorMessage { get; set; }
    public int ElapsedMs { get; set; }
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;
    public FfmpegHardwareAcceleration HardwareAcceleration { get; set; } = FfmpegHardwareAcceleration.Cpu;
    public bool UsedCpuFallback { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();

    public static FfmpegFrameCaptureResult Success(
        byte[] frameBytes,
        int elapsedMs,
        FfmpegHardwareAcceleration hardwareAcceleration,
        Dictionary<string, object>? details = null,
        bool usedCpuFallback = false)
    {
        return new FfmpegFrameCaptureResult
        {
            IsSuccess = true,
            FrameBytes = frameBytes,
            ElapsedMs = elapsedMs,
            HardwareAcceleration = hardwareAcceleration,
            UsedCpuFallback = usedCpuFallback,
            Details = details ?? new Dictionary<string, object>()
        };
    }

    public static FfmpegFrameCaptureResult Failure(
        string errorMessage,
        int elapsedMs = 0,
        FfmpegHardwareAcceleration hardwareAcceleration = FfmpegHardwareAcceleration.Cpu,
        Dictionary<string, object>? details = null)
    {
        return new FfmpegFrameCaptureResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ElapsedMs = elapsedMs,
            HardwareAcceleration = hardwareAcceleration,
            Details = details ?? new Dictionary<string, object>()
        };
    }
}

/// <summary>
/// Process execution result for FFmpeg/FFprobe.
/// </summary>
public sealed class FfmpegProcessResult
{
    public int ExitCode { get; set; }
    public byte[] StandardOutput { get; set; } = Array.Empty<byte>();
    public string StandardError { get; set; } = string.Empty;
    public int ElapsedMs { get; set; }
    public bool TimedOut { get; set; }
    public string? StartError { get; set; }

    public string StandardOutputText => System.Text.Encoding.UTF8.GetString(StandardOutput);
}

internal sealed record FfmpegBuiltCommand(
    string ExecutablePath,
    IReadOnlyList<string> Arguments,
    IReadOnlyList<string> SafeArguments,
    FfmpegHardwareAcceleration HardwareAcceleration,
    string InputKind);
