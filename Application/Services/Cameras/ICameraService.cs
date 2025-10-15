using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Services.Cameras;

/// <summary>
/// Application service interface for camera management and operations
/// Provides high-level camera functionality for controllers and background services
/// </summary>
public interface ICameraService
{
    #region Connection Management

    /// <summary>
    /// Tests camera connection and updates camera status
    /// </summary>
    /// <param name="cameraId">Camera ID to test</param>
    /// <param name="updateStatus">Whether to update camera status in database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection successful, false otherwise</returns>
    Task<bool> TestConnectionAsync(int cameraId, bool updateStatus = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests camera connection using provided connection parameters
    /// </summary>
    /// <param name="cameraType">Type of camera</param>
    /// <param name="connectionString">Connection string</param>
    /// <param name="username">Optional username</param>
    /// <param name="password">Optional password</param>
    /// <param name="timeoutSeconds">Connection timeout in seconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connection test result with details</returns>
    Task<CameraConnectionTestResult> TestConnectionAsync(
        CameraType cameraType, 
        string connectionString,
        string? username = null, 
        string? password = null, 
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current connection status of a camera
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current connection status</returns>
    Task<CameraStatus> GetConnectionStatusAsync(int cameraId, CancellationToken cancellationToken = default);

    #endregion

    #region Streaming Management

    /// <summary>
    /// Starts camera stream
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if stream started successfully</returns>
    Task<bool> StartStreamAsync(int cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops camera stream
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="graceful">Whether to stop gracefully or immediately</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if stream stopped successfully</returns>
    Task<bool> StopStreamAsync(int cameraId, bool graceful = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if camera is currently streaming
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if camera is streaming</returns>
    Task<bool> IsStreamingAsync(int cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current stream information
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream information or null if not streaming</returns>
    Task<CameraStreamInfo?> GetStreamInfoAsync(int cameraId, CancellationToken cancellationToken = default);

    #endregion

    #region Facial Recognition Management

    /// <summary>
    /// Checks if camera has active facial recognition processes
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if facial recognition is active</returns>
    Task<bool> HasActiveFacialRecognitionAsync(int cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts facial recognition processing for a camera
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if facial recognition started successfully</returns>
    Task<bool> StartFacialRecognitionAsync(int cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops facial recognition processing for a camera
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if facial recognition stopped successfully</returns>
    Task<bool> StopFacialRecognitionAsync(int cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels any pending facial recognition tasks for a camera
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CancelFacialRecognitionTasksAsync(int cameraId, CancellationToken cancellationToken = default);

    #endregion

    #region Health Monitoring

    /// <summary>
    /// Performs health check on a camera and updates its status
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check result</returns>
    Task<CameraHealthCheckResult> PerformHealthCheckAsync(int cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs health check on all active cameras
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of health check results</returns>
    Task<IEnumerable<CameraHealthCheckResult>> PerformHealthCheckAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates camera status in the database
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="status">New status</param>
    /// <param name="errorMessage">Optional error message</param>
    /// <param name="userId">User ID for audit trail</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateCameraStatusAsync(int cameraId, CameraStatus status, string? errorMessage = null, 
        int? userId = null, CancellationToken cancellationToken = default);

    #endregion

    #region Resource Management

    /// <summary>
    /// Clears cache for a specific camera
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearCacheAsync(int cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all camera-related caches
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearAllCachesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up file system resources for a camera
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="permanent">Whether this is for permanent deletion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CleanupFileSystemResourcesAsync(int cameraId, bool permanent = false, CancellationToken cancellationToken = default);

    #endregion

    #region Notifications and Integration

    /// <summary>
    /// Notifies other services about camera deletion
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="permanent">Whether deletion is permanent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task NotifyCameraDeletionAsync(int cameraId, bool permanent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates monitoring systems about camera changes
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="removed">Whether camera was removed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateMonitoringSystemsAsync(int cameraId, bool removed = false, CancellationToken cancellationToken = default);

    #endregion

    #region Frame Capture (for future facial recognition)

    /// <summary>
    /// Captures a single frame from the camera for testing or processing
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Captured frame data or null if failed</returns>
    Task<byte[]?> CaptureFrameAsync(int cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts continuous frame capture for facial recognition processing
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if capture started successfully</returns>
    Task<bool> StartFrameCaptureAsync(int cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops continuous frame capture
    /// </summary>
    /// <param name="cameraId">Camera ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if capture stopped successfully</returns>
    Task<bool> StopFrameCaptureAsync(int cameraId, CancellationToken cancellationToken = default);

    #endregion
}