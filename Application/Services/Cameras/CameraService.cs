using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

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
    private readonly Dictionary<int, CameraStreamInfo> _activeStreams = new();
    private readonly Dictionary<int, bool> _activeFacialRecognition = new();
    private readonly object _streamLock = new();

    public CameraService(
        IUnitOfWork unitOfWork,
        ILogger<CameraService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Connection Management

    public async Task<bool> TestConnectionAsync(int cameraId, bool updateStatus = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var camera = await _unitOfWork.Cameras.GetByIdAsync(cameraId, cancellationToken);
            if (camera == null)
            {
                _logger.LogWarning("Camera not found for connection test: {CameraId}", cameraId);
                return false;
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

            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing camera connection for camera {CameraId}", cameraId);
            
            if (updateStatus)
            {
                await UpdateCameraStatusAsync(cameraId, CameraStatus.Error, ex.Message, null, cancellationToken);
            }
            
            return false;
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
                CameraType.USB => await TestUsbCameraConnectionAsync(connectionString, timeoutSeconds, cancellationToken),
                CameraType.ONVIF => await TestOnvifConnectionAsync(connectionString, username, password, timeoutSeconds, cancellationToken),
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

    public async Task<bool> StopStreamAsync(int cameraId, bool graceful = true, CancellationToken cancellationToken = default)
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
                    
                    return true;
                }
            }

            _logger.LogDebug("No active stream found for camera {CameraId}", cameraId);
            return true; // Not an error - stream wasn't active
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping stream for camera {CameraId}", cameraId);
            return false;
        }

        await Task.CompletedTask; // Satisfy async signature
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
            var connectionTest = await TestConnectionAsync(cameraId, false, cancellationToken);

            var result = connectionTest
                ? CameraHealthCheckResult.Healthy(cameraId, camera.Name, 0)
                : CameraHealthCheckResult.Unhealthy(cameraId, camera.Name, "Connection failed", CameraStatus.Error, camera.FailureCount + 1);

            result.PreviousStatus = previousStatus;
            
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
        // Placeholder implementation - would capture actual frame in real implementation
        await Task.CompletedTask;
        _logger.LogDebug("Captured frame from camera {CameraId}", cameraId);
        return null; // Would return actual frame data
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
            _ => (true, string.Empty)
        };
    }

    /// <summary>
    /// Tests RTSP camera connection
    /// </summary>
    private async Task<CameraConnectionTestResult> TestRtspConnectionAsync(string connectionString, 
        string? username, string? password, int timeoutSeconds, CancellationToken cancellationToken)
    {
        // Placeholder implementation - would use RTSP client library
        await Task.Delay(100, cancellationToken); // Simulate connection time
        
        // Basic validation
        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            return CameraConnectionTestResult.Failure("Invalid RTSP URL format");

        // TODO: Implement actual RTSP connection test using library like FFMpeg.NET or similar
        _logger.LogDebug("Testing RTSP connection to {Host}:{Port}", uri.Host, uri.Port);
        
        return CameraConnectionTestResult.Success(100);
    }

    /// <summary>
    /// Tests IP camera connection
    /// </summary>
    private async Task<CameraConnectionTestResult> TestIpCameraConnectionAsync(string connectionString,
        string? username, string? password, int timeoutSeconds, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"));
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            }

            var response = await httpClient.GetAsync(connectionString, cancellationToken);
            
            return response.IsSuccessStatusCode
                ? CameraConnectionTestResult.Success()
                : CameraConnectionTestResult.Failure($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
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
        // Placeholder implementation - would enumerate USB devices and test access
        await Task.Delay(50, cancellationToken);
        
        // TODO: Implement actual USB camera detection and test using OpenCV or DirectShow
        _logger.LogDebug("Testing USB camera: {DevicePath}", connectionString);
        
        return CameraConnectionTestResult.Success(50);
    }

    /// <summary>
    /// Tests ONVIF camera connection
    /// </summary>
    private async Task<CameraConnectionTestResult> TestOnvifConnectionAsync(string connectionString,
        string? username, string? password, int timeoutSeconds, CancellationToken cancellationToken)
    {
        // Placeholder implementation - would use ONVIF client library
        await Task.Delay(200, cancellationToken);
        
        // TODO: Implement actual ONVIF device discovery and connection test
        _logger.LogDebug("Testing ONVIF camera: {ConnectionString}", GetSafeConnectionString(connectionString));
        
        return CameraConnectionTestResult.Success(200);
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