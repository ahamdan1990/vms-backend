using Microsoft.Extensions.Options;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Services.VideoProcessing;

public sealed class FfmpegCapabilityService : IFfmpegCapabilityService
{
    private readonly IFfmpegToolLocator _toolLocator;
    private readonly IFfmpegProcessRunner _processRunner;
    private readonly FfmpegOptions _options;
    private readonly ILogger<FfmpegCapabilityService> _logger;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    private FfmpegCapabilities? _cachedCapabilities;
    private DateTimeOffset _cacheExpiresAt = DateTimeOffset.MinValue;

    public FfmpegCapabilityService(
        IFfmpegToolLocator toolLocator,
        IFfmpegProcessRunner processRunner,
        IOptions<FfmpegOptions> options,
        ILogger<FfmpegCapabilityService> logger)
    {
        _toolLocator = toolLocator;
        _processRunner = processRunner;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FfmpegCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedCapabilities != null && DateTimeOffset.UtcNow < _cacheExpiresAt)
        {
            return _cachedCapabilities;
        }

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedCapabilities != null && DateTimeOffset.UtcNow < _cacheExpiresAt)
            {
                return _cachedCapabilities;
            }

            var ffmpegPath = _toolLocator.ResolveToolPath("ffmpeg");
            var result = await _processRunner.RunAsync(
                ffmpegPath,
                new[] { "-hide_banner", "-hwaccels" },
                timeoutSeconds: 10,
                cancellationToken: cancellationToken);

            var combinedOutput = $"{result.StandardOutputText}{Environment.NewLine}{result.StandardError}";
            var hardwareAccelerations = ParseHardwareAccelerations(combinedOutput);

            var capabilities = new FfmpegCapabilities
            {
                IsFfmpegAvailable = result.ExitCode == 0,
                FfmpegPath = ffmpegPath,
                HardwareAccelerations = hardwareAccelerations,
                ErrorMessage = result.ExitCode == 0
                    ? null
                    : GetFailureMessage(result),
                CheckedAt = DateTimeOffset.UtcNow
            };

            _cachedCapabilities = capabilities;
            _cacheExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, _options.CapabilityCacheSeconds));

            if (capabilities.IsFfmpegAvailable)
            {
                _logger.LogInformation(
                    "FFmpeg detected at {FfmpegPath}. Hardware acceleration: {HardwareAccelerations}",
                    ffmpegPath,
                    string.Join(", ", hardwareAccelerations));
            }
            else
            {
                _logger.LogWarning(
                    "FFmpeg capability check failed for {FfmpegPath}: {ErrorMessage}",
                    ffmpegPath,
                    capabilities.ErrorMessage);
            }

            return capabilities;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task<bool> IsHardwareAccelerationAvailableAsync(
        FfmpegHardwareAcceleration hardwareAcceleration,
        CancellationToken cancellationToken = default)
    {
        if (hardwareAcceleration == FfmpegHardwareAcceleration.Cpu)
        {
            return true;
        }

        var capabilities = await GetCapabilitiesAsync(cancellationToken);
        var ffmpegName = ToFfmpegHardwareName(hardwareAcceleration);

        return capabilities.HardwareAccelerations.Any(
            h => string.Equals(h, ffmpegName, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> ParseHardwareAccelerations(string output)
    {
        var accelerations = new List<string>();
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var inSection = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (line.Equals("Hardware acceleration methods:", StringComparison.OrdinalIgnoreCase))
            {
                inSection = true;
                continue;
            }

            if (!inSection || line.StartsWith("ffmpeg version", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.All(c => char.IsLetterOrDigit(c) || c is '_' or '-'))
            {
                accelerations.Add(line);
            }
        }

        return accelerations.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string ToFfmpegHardwareName(FfmpegHardwareAcceleration hardwareAcceleration)
    {
        return hardwareAcceleration switch
        {
            FfmpegHardwareAcceleration.Cuda => "cuda",
            FfmpegHardwareAcceleration.Dxva2 => "dxva2",
            FfmpegHardwareAcceleration.D3d11va => "d3d11va",
            FfmpegHardwareAcceleration.D3d12va => "d3d12va",
            FfmpegHardwareAcceleration.Qsv => "qsv",
            FfmpegHardwareAcceleration.Vaapi => "vaapi",
            FfmpegHardwareAcceleration.OpenCl => "opencl",
            FfmpegHardwareAcceleration.Vulkan => "vulkan",
            _ => "cpu"
        };
    }

    private static string GetFailureMessage(FfmpegProcessResult result)
    {
        if (result.TimedOut)
        {
            return $"FFmpeg capability check timed out after {result.ElapsedMs}ms";
        }

        if (!string.IsNullOrWhiteSpace(result.StartError))
        {
            return result.StartError;
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            return result.StandardError.Trim();
        }

        return $"FFmpeg exited with code {result.ExitCode}";
    }
}
