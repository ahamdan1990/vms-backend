using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Cameras;

/// <summary>
/// Camera configuration data transfer object
/// Represents technical parameters for camera operation and streaming
/// </summary>
public class CameraConfigurationDto
{
    /// <summary>
    /// Video resolution width in pixels
    /// </summary>
    public int? ResolutionWidth { get; set; }

    /// <summary>
    /// Video resolution height in pixels
    /// </summary>
    public int? ResolutionHeight { get; set; }

    /// <summary>
    /// Resolution as formatted string (e.g., "1920x1080")
    /// </summary>
    public string ResolutionDisplay { get; set; } = "Auto";

    /// <summary>
    /// Frame rate in frames per second (FPS)
    /// </summary>
    [Range(1, 60)]
    public int? FrameRate { get; set; }

    /// <summary>
    /// Video encoding quality (0-100, higher is better quality)
    /// </summary>
    [Range(0, 100)]
    public int? Quality { get; set; }

    /// <summary>
    /// Whether the camera should automatically start streaming on system startup
    /// </summary>
    public bool AutoStart { get; set; }

    /// <summary>
    /// Maximum number of concurrent connections allowed to this camera
    /// </summary>
    [Range(1, 50)]
    public int MaxConnections { get; set; }

    /// <summary>
    /// Connection timeout in seconds for establishing camera connection
    /// </summary>
    [Range(5, 300)]
    public int ConnectionTimeoutSeconds { get; set; }

    /// <summary>
    /// Retry interval in seconds for reconnection attempts
    /// </summary>
    [Range(5, 300)]
    public int RetryIntervalSeconds { get; set; }

    /// <summary>
    /// Maximum number of retry attempts for failed connections
    /// </summary>
    [Range(1, 20)]
    public int MaxRetryAttempts { get; set; }

    /// <summary>
    /// Whether to enable motion detection for this camera
    /// </summary>
    public bool EnableMotionDetection { get; set; }

    /// <summary>
    /// Motion detection sensitivity (0-100, higher is more sensitive)
    /// </summary>
    [Range(0, 100)]
    public int? MotionSensitivity { get; set; }

    /// <summary>
    /// Whether to record video streams
    /// </summary>
    public bool EnableRecording { get; set; }

    /// <summary>
    /// Recording duration in minutes (0 for continuous)
    /// </summary>
    [Range(0, 1440)] // Max 24 hours
    public int? RecordingDurationMinutes { get; set; }

    /// <summary>
    /// Whether to enable facial recognition processing for this camera
    /// </summary>
    public bool EnableFacialRecognition { get; set; }

    /// <summary>
    /// Facial recognition confidence threshold (0-100)
    /// </summary>
    [Range(0, 100)]
    public int? FacialRecognitionThreshold { get; set; }

    /// <summary>
    /// Additional camera-specific configuration parameters
    /// </summary>
    public string? ExtendedConfiguration { get; set; }
}