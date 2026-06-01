using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
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
    private readonly IDataProtector _protector;

    public FfmpegFrameGrabber(
        IFfmpegCapabilityService capabilityService,
        IFfmpegToolLocator toolLocator,
        IFfmpegProcessRunner processRunner,
        IOptions<FfmpegOptions> options,
        ILogger<FfmpegFrameGrabber> logger,
        IDataProtectionProvider dataProtectionProvider)
    {
        _capabilityService = capabilityService;
        _toolLocator = toolLocator;
        _processRunner = processRunner;
        _options = options.Value;
        _logger = logger;
        _protector = (dataProtectionProvider ?? throw new ArgumentNullException(nameof(dataProtectionProvider)))
            .CreateProtector("CameraPasswords");
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
        var accelerationAttempts = hardwareAcceleration != FfmpegHardwareAcceleration.Cpu && configuration.CpuFallbackEnabled
            ? new[] { hardwareAcceleration, FfmpegHardwareAcceleration.Cpu }
            : new[] { hardwareAcceleration };

        foreach (var accelerationAttempt in accelerationAttempts)
        {
            var command = BuildCommand(camera, configuration, accelerationAttempt, singleFrame: false);
            _logger.LogInformation(
                "Starting FFmpeg frame stream for camera {CameraId} with {HardwareAcceleration}. Command: {Arguments}",
                camera.Id,
                accelerationAttempt,
                string.Join(" ", command.SafeArguments.Select(QuoteForDisplay)));
            using var process = StartStreamingProcess(command);

            var sequence = 0;
            var emittedFrame = false;
            // Pre-allocated 128 KB ring-style buffer; grows on demand up to MaxCapturedFrameBytes.
            var frameBuffer = new MemoryStream(131072);
            var windowStart = 0;
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

                    if (AppendToFrameBuffer(frameBuffer, ref windowStart, readBuffer, read, _options.MaxCapturedFrameBytes))
                    {
                        _logger.LogWarning(
                            "Camera {CameraId}: JPEG frame buffer overflow — buffer cleared. Increase MaxCapturedFrameBytes or reduce CaptureFpsLimit.",
                            camera.Id);
                    }

                    while (TryExtractJpegFrame(frameBuffer, ref windowStart, out var frameBytes))
                    {
                        emittedFrame = true;
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
                                ["hardwareAcceleration"] = accelerationAttempt.ToString(),
                                ["inputKind"] = command.InputKind,
                                ["arguments"] = string.Join(" ", command.SafeArguments.Select(QuoteForDisplay))
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

                if (emittedFrame || cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                if (accelerationAttempt != FfmpegHardwareAcceleration.Cpu &&
                    accelerationAttempts.Contains(FfmpegHardwareAcceleration.Cpu))
                {
                    _logger.LogWarning(
                        "FFmpeg stream for camera {CameraId} ended before producing frames with {HardwareAcceleration}. Retrying with CPU fallback.",
                        camera.Id,
                        accelerationAttempt);
                    continue;
                }

                yield break;
            }
            finally
            {
                KillProcessTree(process);
            }
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

    private static IEnumerable<string> BuildOutputArguments(
        CameraConfiguration configuration,
        bool singleFrame)
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
                    "-i",
                    input
                });
                safeArgs.AddRange(new[]
                {
                    "-rtsp_transport",
                    "tcp",
                    "-timeout",
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

    // Returns true if the buffer was cleared due to overflow.
    private static bool AppendToFrameBuffer(MemoryStream buffer, ref int windowStart, byte[] source, int count, int maxBytes)
    {
        var validBytes = (int)buffer.Length - windowStart;
        var overflowed = false;
        if (validBytes + count > maxBytes)
        {
            // Drop everything — a partial frame in progress will be skipped by the JPEG marker scanner.
            buffer.SetLength(0);
            windowStart = 0;
            overflowed = true;
        }

        buffer.Seek(0, SeekOrigin.End);
        buffer.Write(source, 0, count);

        // Compact when the consumed prefix exceeds 64 KB to keep the working set small.
        if (windowStart > 65536)
        {
            CompactFrameBuffer(buffer, ref windowStart);
        }

        return overflowed;
    }

    private static void CompactFrameBuffer(MemoryStream buffer, ref int windowStart)
    {
        var remaining = (int)buffer.Length - windowStart;
        if (remaining <= 0)
        {
            buffer.SetLength(0);
            windowStart = 0;
            return;
        }

        var internalBuffer = buffer.GetBuffer();
        Buffer.BlockCopy(internalBuffer, windowStart, internalBuffer, 0, remaining);
        buffer.SetLength(remaining);
        windowStart = 0;
    }

    private static bool TryExtractJpegFrame(MemoryStream buffer, ref int windowStart, out byte[] frameBytes)
    {
        frameBytes = Array.Empty<byte>();
        var validLen = (int)buffer.Length - windowStart;
        if (validLen < 4)
        {
            return false;
        }

        var internalBuffer = buffer.GetBuffer();
        var span = internalBuffer.AsSpan(windowStart, validLen);

        var soiOffset = IndexOfMarker(span, 0xFF, 0xD8, 0);
        if (soiOffset < 0)
        {
            // Advance past all but the last byte (it could be 0xFF starting the next SOI).
            windowStart += Math.Max(0, validLen - 1);
            return false;
        }

        windowStart += soiOffset;
        span = internalBuffer.AsSpan(windowStart, validLen - soiOffset);

        var eoiOffset = IndexOfMarker(span, 0xFF, 0xD9, 2);
        if (eoiOffset < 0)
        {
            return false;
        }

        var frameLen = eoiOffset + 2;
        frameBytes = span.Slice(0, frameLen).ToArray();
        windowStart += frameLen;
        return true;
    }

    private static int IndexOfMarker(ReadOnlySpan<byte> span, byte first, byte second, int startIndex)
    {
        for (var i = startIndex; i < span.Length - 1; i++)
        {
            if (span[i] == first && span[i + 1] == second)
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
            var configuredDevice = trimmed["video=".Length..].Trim().Trim('"');
            return $"video={configuredDevice}";
        }

        return $"video={trimmed.Trim('"')}";
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

    private string? DecryptPassword(string? encryptedPassword)
    {
        if (string.IsNullOrEmpty(encryptedPassword))
            return null;

        try
        {
            return _protector.Unprotect(encryptedPassword);
        }
        catch
        {
            // Fall back for legacy passwords stored as plain Base64
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(encryptedPassword));
            }
            catch
            {
                return encryptedPassword;
            }
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
