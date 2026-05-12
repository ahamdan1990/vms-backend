using System.Buffers;
using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace VisitorManagementSystem.Api.Application.Services.VideoProcessing;

public sealed class FfmpegProcessRunner : IFfmpegProcessRunner
{
    private readonly FfmpegOptions _options;
    private readonly ILogger<FfmpegProcessRunner> _logger;

    public FfmpegProcessRunner(
        IOptions<FfmpegOptions> options,
        ILogger<FfmpegProcessRunner> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FfmpegProcessResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        int timeoutSeconds,
        int? maxStandardOutputBytes = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        try
        {
            if (!process.Start())
            {
                return new FfmpegProcessResult
                {
                    ExitCode = -1,
                    ElapsedMs = 0,
                    StartError = "Process could not be started"
                };
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds)));

            var stdoutTask = ReadStreamToMemoryAsync(
                process.StandardOutput.BaseStream,
                maxStandardOutputBytes,
                timeoutCts.Token);

            var stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
                var stdout = await stdoutTask;
                var stderr = await stderrTask;

                stopwatch.Stop();
                return new FfmpegProcessResult
                {
                    ExitCode = process.ExitCode,
                    StandardOutput = stdout,
                    StandardError = stderr,
                    ElapsedMs = (int)stopwatch.ElapsedMilliseconds
                };
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                KillProcessTree(process);
                stopwatch.Stop();

                return new FfmpegProcessResult
                {
                    ExitCode = -1,
                    StandardOutput = stdoutTask.IsCompletedSuccessfully ? stdoutTask.Result : Array.Empty<byte>(),
                    StandardError = stderrTask.IsCompletedSuccessfully ? stderrTask.Result : string.Empty,
                    ElapsedMs = (int)stopwatch.ElapsedMilliseconds,
                    TimedOut = true
                };
            }
            catch (InvalidOperationException ex)
            {
                KillProcessTree(process);
                stopwatch.Stop();

                return new FfmpegProcessResult
                {
                    ExitCode = -1,
                    StandardOutput = Array.Empty<byte>(),
                    StandardError = ex.Message,
                    ElapsedMs = (int)stopwatch.ElapsedMilliseconds
                };
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogDebug(ex, "FFmpeg process start failed for {FileName}", fileName);
            return new FfmpegProcessResult
            {
                ExitCode = -1,
                StandardError = ex.Message,
                StartError = ex.Message,
                ElapsedMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    private async Task<byte[]> ReadStreamToMemoryAsync(
        Stream stream,
        int? maxBytes,
        CancellationToken cancellationToken)
    {
        await using var memory = new MemoryStream();
        var buffer = ArrayPool<byte>.Shared.Rent(81920);

        try
        {
            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                if (maxBytes.HasValue && memory.Length + read > maxBytes.Value)
                {
                    throw new InvalidOperationException(
                        $"FFmpeg stdout exceeded the configured limit of {maxBytes.Value} bytes");
                }

                await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            return memory.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
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
            _logger.LogDebug(ex, "Failed to kill FFmpeg process tree");
        }
    }
}
