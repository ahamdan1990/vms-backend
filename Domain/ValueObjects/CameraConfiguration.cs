using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace VisitorManagementSystem.Api.Domain.ValueObjects;

/// <summary>
/// Value object representing camera configuration settings
/// Encapsulates technical parameters for camera operation and streaming
/// </summary>
public class CameraConfiguration
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
    public bool AutoStart { get; set; } = false;

    /// <summary>
    /// Maximum number of concurrent connections allowed to this camera
    /// </summary>
    [Range(1, 50)]
    public int MaxConnections { get; set; } = 5;

    /// <summary>
    /// Connection timeout in seconds for establishing camera connection
    /// </summary>
    [Range(5, 300)]
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Retry interval in seconds for reconnection attempts
    /// </summary>
    [Range(5, 300)]
    public int RetryIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of retry attempts for failed connections
    /// </summary>
    [Range(1, 20)]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Whether to enable motion detection for this camera
    /// </summary>
    public bool EnableMotionDetection { get; set; } = false;

    /// <summary>
    /// Motion detection sensitivity (0-100, higher is more sensitive)
    /// </summary>
    [Range(0, 100)]
    public int? MotionSensitivity { get; set; }

    /// <summary>
    /// Whether to record video streams
    /// </summary>
    public bool EnableRecording { get; set; } = false;

    /// <summary>
    /// Recording duration in minutes (0 for continuous)
    /// </summary>
    [Range(0, 1440)] // Max 24 hours
    public int? RecordingDurationMinutes { get; set; }

    /// <summary>
    /// Whether to enable facial recognition processing for this camera
    /// </summary>
    public bool EnableFacialRecognition { get; set; } = true;

    /// <summary>
    /// Facial recognition confidence threshold (0-100)
    /// </summary>
    [Range(0, 100)]
    public int? FacialRecognitionThreshold { get; set; } = 80;

    /// <summary>
    /// Additional camera-specific configuration parameters
    /// Stored as JSON for flexible extension
    /// </summary>
    public string? ExtendedConfiguration { get; set; }

    /// <summary>
    /// Creates a default camera configuration
    /// </summary>
    public static CameraConfiguration Default => new()
    {
        ResolutionWidth = 1920,
        ResolutionHeight = 1080,
        FrameRate = 30,
        Quality = 75,
        AutoStart = false,
        MaxConnections = 5,
        ConnectionTimeoutSeconds = 30,
        RetryIntervalSeconds = 60,
        MaxRetryAttempts = 3,
        EnableMotionDetection = false,
        EnableRecording = false,
        EnableFacialRecognition = true,
        FacialRecognitionThreshold = 80
    };

    /// <summary>
    /// Gets the resolution as a formatted string (e.g., "1920x1080")
    /// </summary>
    public string GetResolutionString()
    {
        if (ResolutionWidth.HasValue && ResolutionHeight.HasValue)
            return $"{ResolutionWidth}x{ResolutionHeight}";
        return "Auto";
    }

    /// <summary>
    /// Validates the camera configuration
    /// </summary>
    public bool IsValid(out List<string> validationErrors)
    {
        validationErrors = new List<string>();

        if (ResolutionWidth.HasValue && ResolutionWidth <= 0)
            validationErrors.Add("Resolution width must be greater than 0");

        if (ResolutionHeight.HasValue && ResolutionHeight <= 0)
            validationErrors.Add("Resolution height must be greater than 0");

        if (FrameRate.HasValue && (FrameRate <= 0 || FrameRate > 60))
            validationErrors.Add("Frame rate must be between 1 and 60 FPS");

        if (Quality.HasValue && (Quality < 0 || Quality > 100))
            validationErrors.Add("Quality must be between 0 and 100");

        if (EnableMotionDetection && (!MotionSensitivity.HasValue || MotionSensitivity < 0 || MotionSensitivity > 100))
            validationErrors.Add("Motion sensitivity must be between 0 and 100 when motion detection is enabled");

        if (EnableFacialRecognition && (!FacialRecognitionThreshold.HasValue || FacialRecognitionThreshold < 0 || FacialRecognitionThreshold > 100))
            validationErrors.Add("Facial recognition threshold must be between 0 and 100 when facial recognition is enabled");

        return validationErrors.Count == 0;
    }

    /// <summary>
    /// Serializes configuration to JSON string
    /// </summary>
    public string ToJsonString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }

    /// <summary>
    /// Deserializes configuration from JSON string
    /// </summary>
    public static CameraConfiguration? FromJsonString(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<CameraConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }
}