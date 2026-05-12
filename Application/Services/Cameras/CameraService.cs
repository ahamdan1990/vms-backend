using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.Services.VideoProcessing;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;

namespace VisitorManagementSystem.Api.Application.Services.Cameras;

/// <summary>
/// Camera service implementation providing camera management and operations
/// Includes connection testing, streaming management, and health monitoring
/// Note: Actual camera hardware integration would require specialized libraries (OpenCV, camera SDKs)
/// </summary>
public class CameraService : ICameraService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CameraService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFfmpegFrameGrabber _ffmpegFrameGrabber;
    private readonly Dictionary<int, CameraStreamInfo> _activeStreams = new();
    private readonly Dictionary<int, bool> _activeFacialRecognition = new();
    private readonly object _streamLock = new();

    public CameraService(
        IUnitOfWork unitOfWork,
        ILogger<CameraService> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IFfmpegFrameGrabber ffmpegFrameGrabber)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _ffmpegFrameGrabber = ffmpegFrameGrabber ?? throw new ArgumentNullException(nameof(ffmpegFrameGrabber));
    }

    #region Connection Management

    public async Task<bool> TestConnectionAsync(int cameraId, bool updateStatus = true, CancellationToken cancellationToken = default)
    {
        var result = await TestConnectionDetailsAsync(cameraId, updateStatus, cancellationToken);
        return result.IsSuccess;
    }

    public async Task<CameraConnectionTestResult> TestConnectionDetailsAsync(
        int cameraId,
        bool updateStatus = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var camera = await _unitOfWork.Cameras.GetByIdAsync(cameraId, cancellationToken);
            if (camera == null)
            {
                _logger.LogWarning("Camera not found for connection test: {CameraId}", cameraId);
                return CameraConnectionTestResult.Failure("Camera not found", CameraStatus.Inactive);
            }

            var result = await TestConnectionAsync(
                camera.CameraType,
                camera.ConnectionString,
                camera.Username,
                await DecryptPassword(camera.Password), // Decrypt password for testing
                camera.GetConfiguration().ConnectionTimeoutSeconds,
                cancellationToken);

            if (updateStatus)
            {
                await UpdateCameraStatusAsync(cameraId, result.Status, result.ErrorMessage, null, cancellationToken);
            }

            result.Details["cameraId"] = cameraId;
            result.Details["cameraType"] = camera.CameraType.ToString();
            result.Details["updatedStatus"] = updateStatus;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing camera connection for camera {CameraId}", cameraId);
            
            if (updateStatus)
            {
                await UpdateCameraStatusAsync(cameraId, CameraStatus.Error, ex.Message, null, cancellationToken);
            }
            
            return CameraConnectionTestResult.Failure($"Connection test failed: {ex.Message}");
        }
    }

    public async Task<CameraConnectionTestResult> TestConnectionAsync(
        CameraType cameraType, 
        string connectionString, 
        string? username = null, 
        string? password = null, 
        int timeoutSeconds = 30, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Testing {CameraType} camera connection: {ConnectionString}", 
                cameraType, GetSafeConnectionString(connectionString));

            // Validate connection string format
            var formatValidation = ValidateConnectionStringFormat(cameraType, connectionString);
            if (!formatValidation.IsValid)
            {
                return CameraConnectionTestResult.Failure(formatValidation.ErrorMessage);
            }

            // Perform connection test based on camera type
            var testResult = cameraType switch
            {
                CameraType.RTSP => await TestRtspConnectionAsync(connectionString, username, password, timeoutSeconds, cancellationToken),
                CameraType.IP => await TestIpCameraConnectionAsync(connectionString, username, password, timeoutSeconds, cancellationToken),
                CameraType.HttpMjpeg => await TestIpCameraConnectionAsync(connectionString, username, password, timeoutSeconds, cancellationToken),
                CameraType.USB => await TestUsbCameraConnectionAsync(connectionString, timeoutSeconds, cancellationToken),
                CameraType.ONVIF => await TestOnvifConnectionAsync(connectionString, username, password, timeoutSeconds, cancellationToken),
                CameraType.File => await TestFileSourceConnectionAsync(connectionString, timeoutSeconds, cancellationToken),
                _ => CameraConnectionTestResult.Failure($"Unsupported camera type: {cameraType}")
            };

            stopwatch.Stop();
            testResult.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;

            _logger.LogDebug("Camera connection test completed in {ElapsedMs}ms. Success: {IsSuccess}",
                stopwatch.ElapsedMilliseconds, testResult.IsSuccess);

            return testResult;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Camera connection test cancelled for {CameraType}: {ConnectionString}",
                cameraType, GetSafeConnectionString(connectionString));
            return CameraConnectionTestResult.Failure("Connection test was cancelled", CameraStatus.Disconnected);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error testing camera connection for {CameraType}: {ConnectionString}",
                cameraType, GetSafeConnectionString(connectionString));
            
            return CameraConnectionTestResult.Failure($"Connection test failed: {ex.Message}");
        }
    }

    public async Task<CameraStatus> GetConnectionStatusAsync(int cameraId, CancellationToken cancellationToken = default)
    {
        try
        {
            var camera = await _unitOfWork.Cameras.GetByIdAsync(cameraId, cancellationToken);
            return camera?.Status ?? CameraStatus.Inactive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting camera status for camera {CameraId}", cameraId);
            return CameraStatus.Error;
        }
    }

    #endregion

    #region Streaming Management

    public async Task<bool> StartStreamAsync(int cameraId, CancellationToken cancellationToken = default)
    {
        try
        {
            var camera = await _unitOfWork.Cameras.GetByIdAsync(cameraId, cancellationToken);
            if (camera == null || !camera.IsOperational())
            {
                _logger.LogWarning("Cannot start stream for camera {CameraId}: not found or not operational", cameraId);
                return false;
            }

            lock (_streamLock)
            {
                if (_activeStreams.ContainsKey(cameraId))
                {
                    _logger.LogInformation("Stream already active for camera {CameraId}", cameraId);
                    return true;
                }

                // Create stream info (placeholder - actual implementation would start hardware stream)
                var streamInfo = new CameraStreamInfo
                {
                    CameraId = cameraId,
                    IsStreaming = true,
                    StartedAt = DateTime.UtcNow,
                    StreamUrl = GenerateStreamUrl(camera),
                    ActiveConnections = 0
                };

                _activeStreams[cameraId] = streamInfo;
            }

            _logger.LogInformation("Started stream for camera {CameraName} (ID: {CameraId})", camera.Name, cameraId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting stream for camera {CameraId}", cameraId);
            return false;
        }
    }

    public Task<bool> StopStreamAsync(int cameraId, bool graceful = true, CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_streamLock)
            {
                if (_activeStreams.TryGetValue(cameraId, out var streamInfo))
                {
                    _activeStreams.Remove(cameraId);

                    _logger.LogInformation("Stopped stream for camera {CameraId}. Duration: {Duration:F1} seconds",
                        cameraId, streamInfo.DurationSeconds);

                    return Task.FromResult(true);
                }
            }

            _logger.LogDebug("No active stream found for camera {CameraId}", cameraId);
            return Task.FromResult(true); // Not an error - stream wasn't active
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping stream for camera {CameraId}", cameraId);
            return Task.FromResult(false);
        }
    }

    public async Task<bool> IsStreamingAsync(int cameraId, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Satisfy async signature
        
        lock (_streamLock)
        {
            return _activeStreams.ContainsKey(cameraId);
        }
    }

    public async Task<CameraStreamInfo?> GetStreamInfoAsync(int cameraId, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Satisfy async signature
        
        lock (_streamLock)
        {
            _activeStreams.TryGetValue(cameraId, out var streamInfo);
            return streamInfo;
        }
    }

    #endregion

    #region Facial Recognition Management

    public async Task<bool> HasActiveFacialRecognitionAsync(int cameraId, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Satisfy async signature
        return _activeFacialRecognition.GetValueOrDefault(cameraId, false);
    }

    public async Task<bool> StartFacialRecognitionAsync(int cameraId, CancellationToken cancellationToken = default)
    {
        try
        {
            var camera = await _unitOfWork.Cameras.GetByIdAsync(cameraId, cancellationToken);
            if (camera == null || !camera.EnableFacialRecognition || !camera.IsOperational())
            {
                _logger.LogWarning("Cannot start facial recognition for camera {CameraId}: not enabled or not operational", cameraId);
                return false;
            }

            _activeFacialRecognition[cameraId] = true;
            
            _logger.LogInformation("Started facial recognition for camera {CameraName} (ID: {CameraId})", 
                camera.Name, cameraId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting facial recognition for camera {CameraId}", cameraId);
            return false;
        }
    }

    public async Task<bool> StopFacialRecognitionAsync(int cameraId, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Satisfy async signature
        
        _activeFacialRecognition.Remove(cameraId);
        _logger.LogInformation("Stopped facial recognition for camera {CameraId}", cameraId);
        
        return true;
    }

    public async Task CancelFacialRecognitionTasksAsync(int cameraId, CancellationToken cancellationToken = default)
    {
        await StopFacialRecognitionAsync(cameraId, cancellationToken);
        // Additional cleanup would go here in a real implementation
    }

    #endregion

    #region Health Monitoring

    public async Task<CameraHealthCheckResult> PerformHealthCheckAsync(int cameraId, CancellationToken cancellationToken = default)
    {
        try
        {
            var camera = await _unitOfWork.Cameras.GetByIdAsync(cameraId, cancellationToken);
            if (camera == null)
            {
                return CameraHealthCheckResult.Unhealthy(cameraId, "Unknown", "Camera not found");
            }

            var previousStatus = camera.Status;
            var connectionTest = await TestConnectionAsync(
                camera.CameraType,
                camera.ConnectionString,
                camera.Username,
                await DecryptPassword(camera.Password),
                camera.GetConfiguration().ConnectionTimeoutSeconds,
                cancellationToken);

            var result = connectionTest
                .IsSuccess
                    ? CameraHealthCheckResult.Healthy(cameraId, camera.Name, connectionTest.ResponseTimeMs)
                    : CameraHealthCheckResult.Unhealthy(
                        cameraId,
                        camera.Name,
                        connectionTest.ErrorMessage ?? "Connection failed",
                        connectionTest.Status,
                        camera.FailureCount + 1);

            result.PreviousStatus = previousStatus;
            result.ResponseTimeMs = connectionTest.ResponseTimeMs;
            result.Details = connectionTest.Details;

            await UpdateCameraStatusAsync(cameraId, connectionTest.Status, connectionTest.ErrorMessage, null, cancellationToken);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing health check for camera {CameraId}", cameraId);
            return CameraHealthCheckResult.Unhealthy(cameraId, "Unknown", $"Health check failed: {ex.Message}");
        }
    }

    public async Task<IEnumerable<CameraHealthCheckResult>> PerformHealthCheckAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var activeCameras = await _unitOfWork.Cameras
                .GetAsync(c => c.IsActive && !c.IsDeleted, cancellationToken);

            var tasks = activeCameras.Select(camera => PerformHealthCheckAsync(camera.Id, cancellationToken));
            
            return await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing health check on all cameras");
            return Array.Empty<CameraHealthCheckResult>();
        }
    }

    public async Task UpdateCameraStatusAsync(int cameraId, CameraStatus status, string? errorMessage = null, 
        int? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var camera = await _unitOfWork.Cameras.GetByIdAsync(cameraId, cancellationToken);
            if (camera == null)
            {
                _logger.LogWarning("Camera not found for status update: {CameraId}", cameraId);
                return;
            }

            camera.UpdateStatus(status, errorMessage, userId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Updated camera {CameraId} status to {Status}", cameraId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating camera status for camera {CameraId}", cameraId);
        }
    }

    #endregion

    #region Resource Management

    public async Task ClearCacheAsync(int cameraId, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Placeholder for cache clearing logic
        _logger.LogDebug("Cleared cache for camera {CameraId}", cameraId);
    }

    public async Task ClearAllCachesAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Placeholder for cache clearing logic
        _logger.LogDebug("Cleared all camera caches");
    }

    public async Task CleanupFileSystemResourcesAsync(int cameraId, bool permanent = false, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Placeholder for file system cleanup
        _logger.LogInformation("Cleaned up file system resources for camera {CameraId} (Permanent: {Permanent})", 
            cameraId, permanent);
    }

    #endregion

    #region Notifications and Integration

    public async Task NotifyCameraDeletionAsync(int cameraId, bool permanent, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Placeholder for notification logic
        _logger.LogInformation("Notified services about camera {CameraId} deletion (Permanent: {Permanent})", 
            cameraId, permanent);
    }

    public async Task UpdateMonitoringSystemsAsync(int cameraId, bool removed = false, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Placeholder for monitoring system updates
        _logger.LogDebug("Updated monitoring systems for camera {CameraId} (Removed: {Removed})", cameraId, removed);
    }

    #endregion

    #region Frame Capture

    public async Task<byte[]?> CaptureFrameAsync(int cameraId, CancellationToken cancellationToken = default)
    {
        var result = await CaptureFrameDetailsAsync(cameraId, cancellationToken);
        return result.IsSuccess ? result.FrameBytes : null;
    }

    public async Task<CameraFrameCaptureResult> CaptureFrameDetailsAsync(int cameraId, CancellationToken cancellationToken = default)
    {
        try
        {
            var camera = await _unitOfWork.Cameras.GetByIdAsync(cameraId, cancellationToken);
            if (camera == null || camera.IsDeleted)
            {
                return CameraFrameCaptureResult.Failure(cameraId, "Camera not found");
            }

            if (!camera.IsActive)
            {
                return CameraFrameCaptureResult.Failure(cameraId, "Camera is inactive");
            }

            var ffmpegResult = await _ffmpegFrameGrabber.CaptureFrameAsync(camera, cancellationToken);
            var result = new CameraFrameCaptureResult
            {
                CameraId = cameraId,
                IsSuccess = ffmpegResult.IsSuccess,
                FrameBytes = ffmpegResult.FrameBytes,
                ContentType = ffmpegResult.ContentType,
                ErrorMessage = ffmpegResult.ErrorMessage,
                ElapsedMs = ffmpegResult.ElapsedMs,
                CapturedAt = ffmpegResult.CapturedAt,
                HardwareAcceleration = ffmpegResult.HardwareAcceleration.ToString(),
                UsedCpuFallback = ffmpegResult.UsedCpuFallback,
                Details = ffmpegResult.Details
            };

            if (result.IsSuccess)
            {
                camera.UpdateStatus(CameraStatus.Active);
            }
            else
            {
                camera.UpdateStatus(CameraStatus.Error, result.ErrorMessage);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing frame from camera {CameraId}", cameraId);
            return CameraFrameCaptureResult.Failure(cameraId, $"Frame capture failed: {ex.Message}");
        }
    }

    public async Task<bool> StartFrameCaptureAsync(int cameraId, CancellationToken cancellationToken = default)
    {
        // Placeholder implementation - would start continuous frame capture
        await Task.CompletedTask;
        _logger.LogInformation("Started frame capture for camera {CameraId}", cameraId);
        return true;
    }

    public async Task<bool> StopFrameCaptureAsync(int cameraId, CancellationToken cancellationToken = default)
    {
        // Placeholder implementation - would stop frame capture
        await Task.CompletedTask;
        _logger.LogInformation("Stopped frame capture for camera {CameraId}", cameraId);
        return true;
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Validates connection string format based on camera type
    /// </summary>
    private static (bool IsValid, string ErrorMessage) ValidateConnectionStringFormat(CameraType cameraType, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, "Connection string cannot be empty");

        return cameraType switch
        {
            CameraType.RTSP when !connectionString.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) =>
                (false, "RTSP connection string must start with 'rtsp://'"),
            CameraType.IP when !connectionString.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                              !connectionString.StartsWith("https://", StringComparison.OrdinalIgnoreCase) =>
                (false, "IP camera connection string must start with 'http://' or 'https://'"),
            CameraType.HttpMjpeg when !connectionString.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                                      !connectionString.StartsWith("https://", StringComparison.OrdinalIgnoreCase) =>
                (false, "HTTP/MJPEG camera connection string must start with 'http://' or 'https://'"),
            CameraType.ONVIF when !connectionString.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                                  !connectionString.StartsWith("https://", StringComparison.OrdinalIgnoreCase) =>
                (false, "ONVIF camera connection string must start with 'http://' or 'https://'"),
            _ => (true, string.Empty)
        };
    }

    /// <summary>
    /// Tests RTSP camera connection
    /// </summary>
    private async Task<CameraConnectionTestResult> TestRtspConnectionAsync(string connectionString, 
        string? username, string? password, int timeoutSeconds, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            return CameraConnectionTestResult.Failure("Invalid RTSP URL format");

        var ffprobePath = ResolveFfmpegToolPath("ffprobe");
        if (string.IsNullOrWhiteSpace(ffprobePath))
        {
            return CameraConnectionTestResult.Failure("ffprobe was not found. Configure FFmpeg:FFprobePath or place ffprobe.exe in the backend ffmpeg folder.");
        }

        var testUrl = ApplyCredentialsToUri(uri, username, password);
        var timeoutMicroseconds = Math.Max(5, timeoutSeconds) * 1_000_000;

        var processResult = await RunProcessAsync(
            ffprobePath,
            new[]
            {
                "-v", "error",
                "-rtsp_transport", "tcp",
                "-timeout", timeoutMicroseconds.ToString(),
                "-select_streams", "v:0",
                "-show_entries", "stream=codec_type,width,height,r_frame_rate",
                "-of", "default=noprint_wrappers=1:nokey=0",
                testUrl
            },
            timeoutSeconds,
            cancellationToken);

        var result = processResult.ExitCode == 0
            ? CameraConnectionTestResult.Success(processResult.ElapsedMs)
            : CameraConnectionTestResult.Failure(GetProcessError(processResult, "RTSP stream probe failed"));

        result.Details["tool"] = "ffprobe";
        result.Details["host"] = uri.Host;
        result.Details["transport"] = "tcp";
        result.Details["stdout"] = Truncate(processResult.StdOut, 1000);
        result.Details["stderr"] = Truncate(processResult.StdErr, 1000);
        return result;
    }

    /// <summary>
    /// Tests IP camera connection
    /// </summary>
    private async Task<CameraConnectionTestResult> TestIpCameraConnectionAsync(string connectionString,
        string? username, string? password, int timeoutSeconds, CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, timeoutSeconds)));

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(5, timeoutSeconds));

            using var request = new HttpRequestMessage(HttpMethod.Head, connectionString);
            AddBasicAuthentication(request, username, password);

            using var response = await SendHttpProbeAsync(httpClient, request, timeoutCts.Token);
            var isSuccess = IsReachableHttpResponse(response);
            var result = isSuccess
                ? CameraConnectionTestResult.Success()
                : CameraConnectionTestResult.Failure($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");

            result.Status = isSuccess ? CameraStatus.Active : CameraStatus.Error;
            result.Details["httpStatusCode"] = (int)response.StatusCode;
            result.Details["httpReasonPhrase"] = response.ReasonPhrase ?? string.Empty;
            result.Details["contentType"] = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            
            return result;
        }
        catch (HttpRequestException ex)
        {
            return CameraConnectionTestResult.Failure($"HTTP connection failed: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return CameraConnectionTestResult.Failure("Connection timeout", CameraStatus.Disconnected);
        }
    }

    /// <summary>
    /// Tests USB camera connection
    /// </summary>
    private async Task<CameraConnectionTestResult> TestUsbCameraConnectionAsync(string connectionString, 
        int timeoutSeconds, CancellationToken cancellationToken)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return File.Exists(connectionString)
                ? CameraConnectionTestResult.Success()
                : CameraConnectionTestResult.Failure("USB camera testing currently supports Windows DirectShow or an existing device path");
        }

        var ffmpegPath = ResolveFfmpegToolPath("ffmpeg");
        if (string.IsNullOrWhiteSpace(ffmpegPath))
        {
            return CameraConnectionTestResult.Failure("ffmpeg was not found. Configure FFmpeg:FFmpegPath or place ffmpeg.exe in the backend ffmpeg folder.");
        }

        var deviceName = NormalizeDirectShowDeviceName(connectionString);
        var processResult = await RunProcessAsync(
            ffmpegPath,
            new[]
            {
                "-hide_banner",
                "-nostdin",
                "-loglevel", "error",
                "-f", "dshow",
                "-i", deviceName,
                "-frames:v", "1",
                "-f", "null",
                "-"
            },
            timeoutSeconds,
            cancellationToken);

        var result = processResult.ExitCode == 0
            ? CameraConnectionTestResult.Success(processResult.ElapsedMs)
            : CameraConnectionTestResult.Failure(GetProcessError(processResult, "USB camera frame probe failed"));

        result.Details["tool"] = "ffmpeg";
        result.Details["device"] = deviceName;
        result.Details["stderr"] = Truncate(processResult.StdErr, 1000);
        return result;
    }

    /// <summary>
    /// Tests ONVIF camera connection
    /// </summary>
    private async Task<CameraConnectionTestResult> TestOnvifConnectionAsync(string connectionString,
        string? username, string? password, int timeoutSeconds, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
        {
            return CameraConnectionTestResult.Failure("Invalid ONVIF URL format");
        }

        var candidates = BuildOnvifProbeUris(uri).Distinct().ToArray();
        var errors = new List<string>();

        foreach (var candidate in candidates)
        {
            try
            {
                var result = await TestIpCameraConnectionAsync(candidate, username, password, timeoutSeconds, cancellationToken);
                result.Details["probeUrl"] = GetSafeConnectionString(candidate);
                result.Details["onvifProbe"] = true;

                if (result.IsSuccess)
                {
                    return result;
                }

                errors.Add($"{GetSafeConnectionString(candidate)}: {result.ErrorMessage}");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                errors.Add($"{GetSafeConnectionString(candidate)}: {ex.Message}");
            }
        }

        var failure = CameraConnectionTestResult.Failure($"ONVIF probe failed. {string.Join(" | ", errors.Take(3))}");
        failure.Details["probeUrls"] = candidates.Select(GetSafeConnectionString).ToArray();
        return failure;
    }

    private async Task<CameraConnectionTestResult> TestFileSourceConnectionAsync(
        string connectionString,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(connectionString))
        {
            return CameraConnectionTestResult.Failure("File test source was not found");
        }

        var ffprobePath = ResolveFfmpegToolPath("ffprobe");
        if (string.IsNullOrWhiteSpace(ffprobePath))
        {
            var result = CameraConnectionTestResult.Success();
            result.Details["fileExists"] = true;
            result.Details["probeSkipped"] = "ffprobe was not found";
            return result;
        }

        var processResult = await RunProcessAsync(
            ffprobePath,
            new[]
            {
                "-v", "error",
                "-select_streams", "v:0",
                "-show_entries", "stream=codec_type,width,height,r_frame_rate",
                "-of", "default=noprint_wrappers=1:nokey=0",
                connectionString
            },
            timeoutSeconds,
            cancellationToken);

        var testResult = processResult.ExitCode == 0
            ? CameraConnectionTestResult.Success(processResult.ElapsedMs)
            : CameraConnectionTestResult.Failure(GetProcessError(processResult, "Video file probe failed"));

        testResult.Details["tool"] = "ffprobe";
        testResult.Details["fileExists"] = true;
        testResult.Details["stdout"] = Truncate(processResult.StdOut, 1000);
        testResult.Details["stderr"] = Truncate(processResult.StdErr, 1000);
        return testResult;
    }

    /// <summary>
    /// Generates stream URL for a camera (placeholder)
    /// </summary>
    private static string GenerateStreamUrl(Camera camera)
    {
        // In a real implementation, this would generate the actual streaming URL
        return $"/api/cameras/{camera.Id}/stream";
    }

    /// <summary>
    /// Gets connection string with credentials masked for logging
    /// </summary>
    private static string GetSafeConnectionString(string connectionString)
    {
        try
        {
            if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.UserInfo))
            {
                var builder = new UriBuilder(uri) { UserName = "***", Password = "" };
                return builder.ToString();
            }
        }
        catch
        {
            // Fall through to return masked string
        }
        
        return "***";
    }

    private string ResolveFfmpegToolPath(string toolName)
    {
        var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? $"{toolName}.exe"
            : toolName;

        var configuredPath = _configuration[$"FFmpeg:{toolName}Path"]
            ?? _configuration[$"FFmpeg:{char.ToUpperInvariant(toolName[0])}{toolName[1..]}Path"];

        var configuredDirectory = _configuration["FFmpeg:Directory"];
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(configuredPath))
            candidates.Add(configuredPath);

        if (!string.IsNullOrWhiteSpace(configuredDirectory))
            candidates.Add(Path.Combine(configuredDirectory, executableName));

        candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg", executableName));
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "ffmpeg", executableName));
        candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), executableName));

        var foundPath = candidates.FirstOrDefault(File.Exists);
        return foundPath ?? executableName;
    }

    private static string ApplyCredentialsToUri(Uri uri, string? username, string? password)
    {
        if (!string.IsNullOrWhiteSpace(uri.UserInfo) || string.IsNullOrWhiteSpace(username))
        {
            return uri.ToString();
        }

        var builder = new UriBuilder(uri)
        {
            UserName = username,
            Password = password ?? string.Empty
        };

        return builder.Uri.ToString();
    }

    private static void AddBasicAuthentication(HttpRequestMessage request, string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password ?? string.Empty}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    private static async Task<HttpResponseMessage> SendHttpProbeAsync(
        HttpClient httpClient,
        HttpRequestMessage initialRequest,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.SendAsync(initialRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode is not (HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound))
        {
            return response;
        }

        var uri = initialRequest.RequestUri;
        var auth = initialRequest.Headers.Authorization;
        response.Dispose();

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, uri);
        getRequest.Headers.Authorization = auth;
        return await httpClient.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private static bool IsReachableHttpResponse(HttpResponseMessage response)
    {
        if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 400)
        {
            return true;
        }

        return false;
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

    private static IEnumerable<string> BuildOnvifProbeUris(Uri uri)
    {
        yield return uri.ToString();

        var root = $"{uri.Scheme}://{uri.Authority}";
        yield return $"{root}/onvif/device_service";
        yield return $"{root}/onvif/DeviceService";
    }

    private static async Task<ProcessRunResult> RunProcessAsync(
        string fileName,
        IEnumerable<string> arguments,
        int timeoutSeconds,
        CancellationToken cancellationToken)
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

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) stdout.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) stderr.AppendLine(e.Data);
        };

        try
        {
            if (!process.Start())
            {
                return new ProcessRunResult(-1, string.Empty, "Process could not be started", 0, TimedOut: false);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, timeoutSeconds)));

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
                stopwatch.Stop();
                return new ProcessRunResult(process.ExitCode, stdout.ToString(), stderr.ToString(), (int)stopwatch.ElapsedMilliseconds, TimedOut: false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Best-effort process cleanup.
                }

                stopwatch.Stop();
                return new ProcessRunResult(-1, stdout.ToString(), stderr.ToString(), (int)stopwatch.ElapsedMilliseconds, TimedOut: true);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            return new ProcessRunResult(-1, stdout.ToString(), ex.Message, (int)stopwatch.ElapsedMilliseconds, TimedOut: false);
        }
    }

    private static string GetProcessError(ProcessRunResult processResult, string fallback)
    {
        if (processResult.TimedOut)
        {
            return $"{fallback}: timed out after {processResult.ElapsedMs}ms";
        }

        var error = string.IsNullOrWhiteSpace(processResult.StdErr)
            ? processResult.StdOut
            : processResult.StdErr;

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

    private sealed record ProcessRunResult(
        int ExitCode,
        string StdOut,
        string StdErr,
        int ElapsedMs,
        bool TimedOut);

    /// <summary>
    /// Decrypts password (placeholder implementation)
    /// </summary>
    private async Task<string?> DecryptPassword(string? encryptedPassword)
    {
        if (string.IsNullOrEmpty(encryptedPassword))
            return null;

        // TODO: Implement proper decryption
        await Task.CompletedTask;
        try
        {
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encryptedPassword));
        }
        catch
        {
            return null;
        }
    }

    #endregion
}
