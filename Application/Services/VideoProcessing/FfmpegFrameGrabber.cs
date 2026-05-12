using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Application.Services.VideoProcessing;

public sealed class FfmpegFrameGrabber : IFfmpegFrameGrabber
{
    private const string JpegContentType = "image/jpeg";
    private readonly IFfmpegCapabilityService _capabilityService;
    private readonly IFfmpegToolLocator _toolLocator;
    private readonly IFfmpegProcessRunner _processRunner;
    private readonly FfmpegOptions _options;
    private readonly ILogger<FfmpegFrameGrabber> _logger;

    public FfmpegFrameGrabber(
        IFfmpegCapabilityService capabilityService,
        IFfmpegToolLocator toolLocator,
        IFfmpegProcessRunner processRunner,
        IOptions<FfmpegOptions> options,
        ILogger<FfmpegFrameGrabber> logger)
    {
        _capabilityService = capabilityService;
        _toolLocator = toolLocator;
        _processRunner = processRunner;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FfmpegFrameCaptureResult> CaptureFrameAsync(
        Camera camera,
        CancellationToken cancellationToken = default)
    {
        var configuration = camera.GetConfiguration();
        var hardwareAcceleration = await ResolveHardwareAccelerationAsync(camera, configuration, cancellationToken);
        var firstAttempt = await CaptureFrameWithAccelerationAsync(
            camera,
            configuration,
            hardwareAcceleration,
            usedCpuFallback: false,
            useConfiguredUsbInputOptions: true,
            cancellationToken);

        if (!firstAttempt.IsSuccess && camera.CameraType == CameraType.USB && HasConfiguredUsbInputOptions(configuration))
        {
            _logger.LogWarning(
                "FFmpeg USB frame capture failed for camera {CameraId} with configured DirectShow options. Retrying with device auto mode. Error: {ErrorMessage}",
                camera.Id,
                firstAttempt.ErrorMessage);

            var autoUsbAttempt = await CaptureFrameWithAccelerationAsync(
                camera,
                configuration,
                FfmpegHardwareAcceleration.Cpu,
                usedCpuFallback: false,
                useConfiguredUsbInputOptions: false,
                cancellationToken);

            autoUsbAttempt.Details["configuredUsbAttemptError"] = firstAttempt.ErrorMessage ?? string.Empty;
            if (autoUsbAttempt.IsSuccess)
            {
                autoUsbAttempt.Details["usbInputFallback"] = "auto";
                return autoUsbAttempt;
            }

            firstAttempt = autoUsbAttempt;
        }

        if (firstAttempt.IsSuccess ||
            hardwareAcceleration == FfmpegHardwareAcceleration.Cpu ||
            !configuration.CpuFallbackEnabled)
        {
            return firstAttempt;
        }

        _logger.LogWarning(
            "FFmpeg frame capture failed for camera {CameraId} with {HardwareAcceleration}. Retrying with CPU fallback. Error: {ErrorMessage}",
            camera.Id,
            hardwareAcceleration,
            firstAttempt.ErrorMessage);

        var fallbackAttempt = await CaptureFrameWithAccelerationAsync(
            camera,
            configuration,
            FfmpegHardwareAcceleration.Cpu,
            usedCpuFallback: true,
            useConfiguredUsbInputOptions: true,
            cancellationToken);

        fallbackAttempt.Details["hardwareAttemptError"] = firstAttempt.ErrorMessage ?? string.Empty;
        fallbackAttempt.Details["hardwareAttempt"] = hardwareAcceleration.ToString();
        return fallbackAttempt;
    }

    public async IAsyncEnumerable<FfmpegFrame> CaptureFramesAsync(
        Camera camera,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var configuration = camera.GetConfiguration();
        var hardwareAcceleration = await ResolveHardwareAccelerationAsync(camera, configuration, cancellationToken);
        var command = BuildCommand(camera, configuration, hardwareAcceleration, singleFrame: false);
        using var process = StartStreamingProcess(command);

        var sequence = 0;
        var buffer = new List<byte>(1024 * 1024);
        var readBuffer = new byte[81920];
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var read = await process.StandardOutput.BaseStream.ReadAsync(readBuffer, cancellationToken);
                if (read == 0)
                {
                    break;
                }

                AppendBytes(buffer, readBuffer, read, _options.MaxCapturedFrameBytes);

                while (TryExtractJpegFrame(buffer, out var frameBytes))
                {
                    sequence++;
                    yield return new FfmpegFrame
                    {
                        CameraId = camera.Id,
                        SequenceNumber = sequence,
                        ImageBytes = frameBytes,
                        ContentType = JpegContentType,
                        CapturedAt = DateTimeOffset.UtcNow,
                        Metadata =
                        {
                            ["hardwareAcceleration"] = hardwareAcceleration.ToString(),
                            ["inputKind"] = command.InputKind
                        }
                    };
                }
            }

            await process.WaitForExitAsync(cancellationToken);
            var stderr = await stderrTask;
            if (process.ExitCode != 0)
            {
                _logger.LogWarning(
                    "FFmpeg frame stream exited for camera {CameraId} with code {ExitCode}: {Error}",
                    camera.Id,
                    process.ExitCode,
                    Truncate(stderr, 1000));
            }
        }
        finally
        {
            KillProcessTree(process);
        }
    }

    private async Task<FfmpegFrameCaptureResult> CaptureFrameWithAccelerationAsync(
        Camera camera,
        CameraConfiguration configuration,
        FfmpegHardwareAcceleration hardwareAcceleration,
        bool usedCpuFallback,
        bool useConfiguredUsbInputOptions,
        CancellationToken cancellationToken)
    {
        var command = BuildCommand(
            camera,
            configuration,
            hardwareAcceleration,
            singleFrame: true,
            useConfiguredUsbInputOptions);
        var timeoutSeconds = Math.Max(
            5,
            configuration.ConnectionTimeoutSeconds > 0
                ? configuration.ConnectionTimeoutSeconds
                : _options.DefaultFrameCaptureTimeoutSeconds);

        var result = await _processRunner.RunAsync(
            command.ExecutablePath,
            command.Arguments,
            timeoutSeconds,
            _options.MaxCapturedFrameBytes,
            cancellationToken);

        var details = BuildDetails(command, result);

        if (result.ExitCode != 0)
        {
            return FfmpegFrameCaptureResult.Failure(
                GetProcessError(result, "FFmpeg frame capture failed"),
                result.ElapsedMs,
                hardwareAcceleration,
                details);
        }

        if (result.StandardOutput.Length == 0)
        {
            return FfmpegFrameCaptureResult.Failure(
                "FFmpeg completed without returning image data",
                result.ElapsedMs,
                hardwareAcceleration,
                details);
        }

        if (!LooksLikeJpeg(result.StandardOutput))
        {
            return FfmpegFrameCaptureResult.Failure(
                "FFmpeg returned data that is not a JPEG frame",
                result.ElapsedMs,
                hardwareAcceleration,
                details);
        }

        details["frameSizeBytes"] = result.StandardOutput.Length;
        return FfmpegFrameCaptureResult.Success(
            result.StandardOutput,
            result.ElapsedMs,
            hardwareAcceleration,
            details,
            usedCpuFallback);
    }

    private async Task<FfmpegHardwareAcceleration> ResolveHardwareAccelerationAsync(
        Camera camera,
        CameraConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (camera.CameraType == CameraType.USB ||
            !configuration.GpuDecodingEnabled ||
            configuration.HardwareAcceleration == FfmpegHardwareAcceleration.Cpu)
        {
            return FfmpegHardwareAcceleration.Cpu;
        }

        if (configuration.HardwareAcceleration != FfmpegHardwareAcceleration.Auto)
        {
            var preferredAvailable = await _capabilityService.IsHardwareAccelerationAvailableAsync(
                configuration.HardwareAcceleration,
                cancellationToken);

            return preferredAvailable
                ? configuration.HardwareAcceleration
                : FfmpegHardwareAcceleration.Cpu;
        }

        var preferredOrder = new[]
        {
            FfmpegHardwareAcceleration.Cuda,
            FfmpegHardwareAcceleration.D3d11va,
            FfmpegHardwareAcceleration.Dxva2,
            FfmpegHardwareAcceleration.D3d12va,
            FfmpegHardwareAcceleration.Qsv,
            FfmpegHardwareAcceleration.Vaapi,
            FfmpegHardwareAcceleration.Vulkan,
            FfmpegHardwareAcceleration.OpenCl
        };

        foreach (var candidate in preferredOrder)
        {
            if (await _capabilityService.IsHardwareAccelerationAvailableAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        return FfmpegHardwareAcceleration.Cpu;
    }

    private FfmpegBuiltCommand BuildCommand(
        Camera camera,
        CameraConfiguration configuration,
        FfmpegHardwareAcceleration hardwareAcceleration,
        bool singleFrame,
        bool useConfiguredUsbInputOptions = true)
    {
        var executablePath = _toolLocator.ResolveToolPath("ffmpeg");
        var args = new List<string>
        {
            "-hide_banner",
            "-nostdin",
            "-loglevel",
            "warning",
            "-fflags",
            "+discardcorrupt"
        };
        var safeArgs = new List<string>(args);

        if (hardwareAcceleration != FfmpegHardwareAcceleration.Cpu)
        {
            args.AddRange(new[] { "-hwaccel", ToFfmpegHardwareName(hardwareAcceleration) });
            safeArgs.AddRange(new[] { "-hwaccel", ToFfmpegHardwareName(hardwareAcceleration) });
        }

        var inputKind = AddInputArguments(camera, configuration, args, safeArgs, singleFrame, useConfiguredUsbInputOptions);

        args.AddRange(BuildOutputArguments(configuration, singleFrame));
        safeArgs.AddRange(BuildOutputArguments(configuration, singleFrame));

        return new FfmpegBuiltCommand(
            executablePath,
            args,
            safeArgs,
            hardwareAcceleration,
            inputKind);
    }

    private static IEnumerable<string> BuildOutputArguments(CameraConfiguration configuration, bool singleFrame)
    {
        var args = new List<string>
        {
            "-an",
            "-sn",
            "-map",
            "0:v:0"
        };

        var filters = new List<string>();
        if (!singleFrame && configuration.InferenceFps is > 0)
        {
            filters.Add($"fps={configuration.InferenceFps.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        if (filters.Count > 0)
        {
            args.AddRange(new[] { "-vf", string.Join(",", filters) });
        }

        if (singleFrame)
        {
            args.AddRange(new[] { "-frames:v", "1" });
        }

        args.AddRange(new[]
        {
            "-f",
            "image2pipe",
            "-vcodec",
            "mjpeg",
            "-q:v",
            "3",
            "pipe:1"
        });

        return args;
    }

    private string AddInputArguments(
        Camera camera,
        CameraConfiguration configuration,
        List<string> args,
        List<string> safeArgs,
        bool singleFrame,
        bool useConfiguredUsbInputOptions)
    {
        var timeoutMicroseconds = Math.Max(5, configuration.ConnectionTimeoutSeconds) * 1_000_000;

        switch (camera.CameraType)
        {
            case CameraType.USB:
                AddUsbInputArguments(camera.ConnectionString, configuration, args, safeArgs, useConfiguredUsbInputOptions);
                return useConfiguredUsbInputOptions ? "directshow" : "directshow-auto";

            case CameraType.RTSP:
            {
                var input = BuildAuthenticatedInput(camera, out var safeInput);
                args.AddRange(new[]
                {
                    "-rtsp_transport",
                    "tcp",
                    "-timeout",
                    timeoutMicroseconds.ToString(CultureInfo.InvariantCulture),
                    "-rw_timeout",
                    timeoutMicroseconds.ToString(CultureInfo.InvariantCulture),
                    "-i",
                    input
                });
                safeArgs.AddRange(new[]
                {
                    "-rtsp_transport",
                    "tcp",
                    "-timeout",
                    timeoutMicroseconds.ToString(CultureInfo.InvariantCulture),
                    "-rw_timeout",
                    timeoutMicroseconds.ToString(CultureInfo.InvariantCulture),
                    "-i",
                    safeInput
                });
                return "rtsp";
            }

            case CameraType.IP:
            case CameraType.HttpMjpeg:
            case CameraType.ONVIF:
            {
                var input = BuildAuthenticatedInput(camera, out var safeInput);
                args.AddRange(new[]
                {
                    "-rw_timeout",
                    timeoutMicroseconds.ToString(CultureInfo.InvariantCulture),
                    "-reconnect",
                    "1",
                    "-reconnect_streamed",
                    "1",
                    "-reconnect_on_network_error",
                    "1",
                    "-reconnect_delay_max",
                    "2",
                    "-i",
                    input
                });
                safeArgs.AddRange(new[]
                {
                    "-rw_timeout",
                    timeoutMicroseconds.ToString(CultureInfo.InvariantCulture),
                    "-reconnect",
                    "1",
                    "-reconnect_streamed",
                    "1",
                    "-reconnect_on_network_error",
                    "1",
                    "-reconnect_delay_max",
                    "2",
                    "-i",
                    safeInput
                });
                return camera.CameraType == CameraType.ONVIF ? "onvif-stream-url" : "http";
            }

            case CameraType.File:
                if (!singleFrame)
                {
                    args.AddRange(new[] { "-stream_loop", "-1" });
                    safeArgs.AddRange(new[] { "-stream_loop", "-1" });
                }

                args.AddRange(new[] { "-i", camera.ConnectionString });
                safeArgs.AddRange(new[] { "-i", camera.GetSafeConnectionString() });
                return "file";

            default:
                throw new InvalidOperationException($"Unsupported camera type: {camera.CameraType}");
        }
    }

    private static void AddUsbInputArguments(
        string connectionString,
        CameraConfiguration configuration,
        List<string> args,
        List<string> safeArgs,
        bool useConfiguredInputOptions)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            args.AddRange(new[] { "-f", "dshow" });
            safeArgs.AddRange(new[] { "-f", "dshow" });

            if (useConfiguredInputOptions &&
                configuration.ResolutionWidth is > 0 &&
                configuration.ResolutionHeight is > 0)
            {
                var size = $"{configuration.ResolutionWidth.Value}x{configuration.ResolutionHeight.Value}";
                args.AddRange(new[] { "-video_size", size });
                safeArgs.AddRange(new[] { "-video_size", size });
            }

            var fps = useConfiguredInputOptions
                ? configuration.CaptureFpsLimit ?? configuration.FrameRate
                : null;
            if (fps is > 0)
            {
                args.AddRange(new[] { "-framerate", fps.Value.ToString(CultureInfo.InvariantCulture) });
                safeArgs.AddRange(new[] { "-framerate", fps.Value.ToString(CultureInfo.InvariantCulture) });
            }

            var deviceName = NormalizeDirectShowDeviceName(connectionString);
            args.AddRange(new[] { "-i", deviceName });
            safeArgs.AddRange(new[] { "-i", deviceName });
            return;
        }

        args.AddRange(new[] { "-i", connectionString });
        safeArgs.AddRange(new[] { "-i", connectionString });
    }

    private string BuildAuthenticatedInput(Camera camera, out string safeInput)
    {
        safeInput = camera.GetSafeConnectionString();

        if (!Uri.TryCreate(camera.ConnectionString, UriKind.Absolute, out var uri))
        {
            return camera.ConnectionString;
        }

        if (!string.IsNullOrWhiteSpace(uri.UserInfo) || string.IsNullOrWhiteSpace(camera.Username))
        {
            return camera.ConnectionString;
        }

        var builder = new UriBuilder(uri)
        {
            UserName = camera.Username,
            Password = DecryptPassword(camera.Password) ?? string.Empty
        };

        safeInput = new UriBuilder(uri)
        {
            UserName = "***",
            Password = string.Empty
        }.Uri.ToString();

        return builder.Uri.ToString();
    }

    private static Process StartStreamingProcess(FfmpegBuiltCommand command)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command.ExecutablePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        foreach (var argument in command.Arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        if (!process.Start())
        {
            throw new InvalidOperationException("FFmpeg process could not be started");
        }

        return process;
    }

    private void KillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(_options.ProcessStopTimeoutMs);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to stop FFmpeg streaming process");
        }
    }

    private static Dictionary<string, object> BuildDetails(
        FfmpegBuiltCommand command,
        FfmpegProcessResult result)
    {
        return new Dictionary<string, object>
        {
            ["tool"] = "ffmpeg",
            ["ffmpegPath"] = command.ExecutablePath,
            ["hardwareAcceleration"] = command.HardwareAcceleration.ToString(),
            ["inputKind"] = command.InputKind,
            ["arguments"] = string.Join(" ", command.SafeArguments.Select(QuoteForDisplay)),
            ["exitCode"] = result.ExitCode,
            ["timedOut"] = result.TimedOut,
            ["elapsedMs"] = result.ElapsedMs,
            ["stderr"] = Truncate(result.StandardError, 1000)
        };
    }

    private static void AppendBytes(List<byte> destination, byte[] source, int count, int maxBytes)
    {
        if (destination.Count + count > maxBytes)
        {
            var removeCount = Math.Min(destination.Count, destination.Count + count - maxBytes);
            if (removeCount > 0)
            {
                destination.RemoveRange(0, removeCount);
            }
        }

        for (var i = 0; i < count; i++)
        {
            destination.Add(source[i]);
        }
    }

    private static bool TryExtractJpegFrame(List<byte> buffer, out byte[] frameBytes)
    {
        frameBytes = Array.Empty<byte>();
        var start = IndexOfMarker(buffer, 0xFF, 0xD8, 0);
        if (start < 0)
        {
            if (buffer.Count > 1)
            {
                buffer.RemoveRange(0, buffer.Count - 1);
            }

            return false;
        }

        if (start > 0)
        {
            buffer.RemoveRange(0, start);
        }

        var end = IndexOfMarker(buffer, 0xFF, 0xD9, 2);
        if (end < 0)
        {
            return false;
        }

        var length = end + 2;
        frameBytes = buffer.GetRange(0, length).ToArray();
        buffer.RemoveRange(0, length);
        return true;
    }

    private static int IndexOfMarker(List<byte> buffer, byte first, byte second, int startIndex)
    {
        for (var i = startIndex; i < buffer.Count - 1; i++)
        {
            if (buffer[i] == first && buffer[i + 1] == second)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool LooksLikeJpeg(byte[] bytes)
    {
        return bytes.Length > 4 &&
               bytes[0] == 0xFF &&
               bytes[1] == 0xD8 &&
               bytes[^2] == 0xFF &&
               bytes[^1] == 0xD9;
    }

    private static bool HasConfiguredUsbInputOptions(CameraConfiguration configuration)
    {
        return (configuration.ResolutionWidth is > 0 && configuration.ResolutionHeight is > 0) ||
               configuration.CaptureFpsLimit is > 0 ||
               configuration.FrameRate is > 0;
    }

    private static string NormalizeDirectShowDeviceName(string connectionString)
    {
        var trimmed = connectionString.Trim();
        if (trimmed.StartsWith("video=", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        var escaped = trimmed.Replace("\"", "\\\"");
        return $"video=\"{escaped}\"";
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

    private static string? DecryptPassword(string? encryptedPassword)
    {
        if (string.IsNullOrEmpty(encryptedPassword))
        {
            return null;
        }

        try
        {
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encryptedPassword));
        }
        catch
        {
            return encryptedPassword;
        }
    }

    private static string GetProcessError(FfmpegProcessResult processResult, string fallback)
    {
        if (processResult.TimedOut)
        {
            return $"{fallback}: timed out after {processResult.ElapsedMs}ms";
        }

        var error = string.IsNullOrWhiteSpace(processResult.StandardError)
            ? processResult.StandardOutputText
            : processResult.StandardError;

        return string.IsNullOrWhiteSpace(error)
            ? fallback
            : $"{fallback}: {Truncate(error, 500)}";
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string QuoteForDisplay(string argument)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            return "\"\"";
        }

        return argument.Any(char.IsWhiteSpace)
            ? $"\"{argument.Replace("\"", "\\\"")}\""
            : argument;
    }
}
