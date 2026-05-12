using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using VisitorManagementSystem.Api.Domain.Enums;

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
    /// Whether this camera is enabled for runtime workers.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Entry/exit/monitoring role used by recognition workflows.
    /// </summary>
    public CameraRole CameraRole { get; set; } = CameraRole.General;

    /// <summary>
    /// Preferred face recognition engine for this camera.
    /// </summary>
    public FaceRecognitionEngine PreferredEngine { get; set; } = FaceRecognitionEngine.Hybrid;

    /// <summary>
    /// Workflow automation mode for entry/exit cameras.
    /// </summary>
    public CameraWorkflowMode WorkflowMode { get; set; } = CameraWorkflowMode.Manual;

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
    /// Whether face detection should run for this camera.
    /// </summary>
    public bool DetectionEnabled { get; set; } = true;

    /// <summary>
    /// Whether face tracking should run for this camera.
    /// </summary>
    public bool TrackingEnabled { get; set; } = true;

    /// <summary>
    /// Whether identity recognition should run for this camera.
    /// </summary>
    public bool RecognitionEnabled { get; set; } = true;

    /// <summary>
    /// Whether CompreFace may be used when Luxand fails.
    /// </summary>
    public bool CompreFaceFallbackEnabled { get; set; } = true;

    /// <summary>
    /// Facial recognition confidence threshold (0-100)
    /// </summary>
    [Range(0, 100)]
    public int? FacialRecognitionThreshold { get; set; } = 80;

    /// <summary>
    /// Face detection confidence threshold (0-100).
    /// </summary>
    [Range(0, 100)]
    public int? FaceDetectionThreshold { get; set; } = 80;

    /// <summary>
    /// Threshold below which a detected face is treated as unknown (0-100).
    /// </summary>
    [Range(0, 100)]
    public int? UnknownFaceThreshold { get; set; } = 70;

    /// <summary>
    /// Minimum accepted face size in pixels.
    /// </summary>
    [Range(20, 2000)]
    public int? MinimumFaceSizePixels { get; set; } = 80;

    /// <summary>
    /// Minimum accepted face quality score (0-100).
    /// </summary>
    [Range(0, 100)]
    public int? FaceQualityThreshold { get; set; } = 60;

    /// <summary>
    /// Blur rejection threshold (0-100), interpreted by the pipeline implementation.
    /// </summary>
    [Range(0, 100)]
    public int? BlurThreshold { get; set; }

    /// <summary>
    /// Maximum allowed yaw angle in degrees.
    /// </summary>
    [Range(0, 90)]
    public int? YawLimitDegrees { get; set; } = 30;

    /// <summary>
    /// Maximum allowed pitch angle in degrees.
    /// </summary>
    [Range(0, 90)]
    public int? PitchLimitDegrees { get; set; } = 30;

    /// <summary>
    /// Maximum allowed roll angle in degrees.
    /// </summary>
    [Range(0, 180)]
    public int? RollLimitDegrees { get; set; } = 45;

    /// <summary>
    /// Maximum faces to process per frame.
    /// </summary>
    [Range(1, 100)]
    public int MaxFacesPerFrame { get; set; } = 10;

    /// <summary>
    /// Maximum active tracks per camera.
    /// </summary>
    [Range(1, 200)]
    public int MaxConcurrentTracks { get; set; } = 25;

    /// <summary>
    /// Limit of frames captured per second before sampling.
    /// </summary>
    [Range(1, 120)]
    public int? CaptureFpsLimit { get; set; } = 15;

    /// <summary>
    /// Target frames per second that enter face inference.
    /// </summary>
    [Range(1, 60)]
    public int? InferenceFps { get; set; } = 5;

    /// <summary>
    /// Minimum interval between sampled frames in milliseconds.
    /// </summary>
    [Range(0, 60000)]
    public int FrameSamplingIntervalMs { get; set; } = 200;

    /// <summary>
    /// Minimum interval between recognition attempts for the same active track.
    /// </summary>
    [Range(100, 600000)]
    public int RecognitionIntervalPerTrackMs { get; set; } = 10000;

    /// <summary>
    /// Time a track may disappear before it is considered gone.
    /// </summary>
    [Range(1000, 600000)]
    public int TrackTimeoutMs { get; set; } = 10000;

    /// <summary>
    /// Time window used before allowing a disappeared person to be re-identified as a new event.
    /// </summary>
    [Range(1000, 600000)]
    public int ReIdentificationTimeoutMs { get; set; } = 10000;

    /// <summary>
    /// General alert cooldown in milliseconds.
    /// </summary>
    [Range(0, 86400000)]
    public int AlertCooldownMs { get; set; } = 30000;

    /// <summary>
    /// Cooldown for repeated known-person alerts/events.
    /// </summary>
    [Range(0, 86400000)]
    public int KnownFaceCooldownMs { get; set; } = 60000;

    /// <summary>
    /// Cooldown for repeated unknown-face alerts/events.
    /// </summary>
    [Range(0, 86400000)]
    public int UnknownFaceCooldownMs { get; set; } = 30000;

    /// <summary>
    /// Save face snapshots for recognition events.
    /// </summary>
    public bool SnapshotSavingEnabled { get; set; } = true;

    /// <summary>
    /// Save unknown face snapshots.
    /// </summary>
    public bool StoreUnknownFaceSnapshots { get; set; } = true;

    /// <summary>
    /// Save known face snapshots that may improve enrollment.
    /// </summary>
    public bool StoreKnownFaceSnapshots { get; set; } = true;

    /// <summary>
    /// Retention for unknown snapshots in days.
    /// </summary>
    [Range(1, 7)]
    public int UnknownSnapshotRetentionDays { get; set; } = 7;

    /// <summary>
    /// Number of additional known-face images to keep per identity beyond the original profile image.
    /// </summary>
    [Range(0, 20)]
    public int KnownAdditionalFaceLimit { get; set; } = 5;

    /// <summary>
    /// Enable hardware-accelerated FFmpeg decoding when available.
    /// </summary>
    public bool GpuDecodingEnabled { get; set; } = true;

    /// <summary>
    /// Fall back to CPU decoding when hardware acceleration fails.
    /// </summary>
    public bool CpuFallbackEnabled { get; set; } = true;

    /// <summary>
    /// Preferred FFmpeg hardware acceleration mode.
    /// </summary>
    public FfmpegHardwareAcceleration HardwareAcceleration { get; set; } = FfmpegHardwareAcceleration.Auto;

    /// <summary>
    /// Camera health check interval in seconds.
    /// </summary>
    [Range(5, 86400)]
    public int HealthCheckIntervalSeconds { get; set; } = 60;

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
        Enabled = true,
        CameraRole = CameraRole.General,
        PreferredEngine = FaceRecognitionEngine.Hybrid,
        WorkflowMode = CameraWorkflowMode.Manual,
        AutoStart = false,
        MaxConnections = 5,
        ConnectionTimeoutSeconds = 30,
        RetryIntervalSeconds = 60,
        MaxRetryAttempts = 3,
        EnableMotionDetection = false,
        EnableRecording = false,
        EnableFacialRecognition = true,
        DetectionEnabled = true,
        TrackingEnabled = true,
        RecognitionEnabled = true,
        CompreFaceFallbackEnabled = true,
        FacialRecognitionThreshold = 80,
        FaceDetectionThreshold = 80,
        UnknownFaceThreshold = 70,
        MinimumFaceSizePixels = 80,
        FaceQualityThreshold = 60,
        YawLimitDegrees = 30,
        PitchLimitDegrees = 30,
        RollLimitDegrees = 45,
        MaxFacesPerFrame = 10,
        MaxConcurrentTracks = 25,
        CaptureFpsLimit = 15,
        InferenceFps = 5,
        FrameSamplingIntervalMs = 200,
        RecognitionIntervalPerTrackMs = 10000,
        TrackTimeoutMs = 10000,
        ReIdentificationTimeoutMs = 10000,
        AlertCooldownMs = 30000,
        KnownFaceCooldownMs = 60000,
        UnknownFaceCooldownMs = 30000,
        SnapshotSavingEnabled = true,
        StoreUnknownFaceSnapshots = true,
        StoreKnownFaceSnapshots = true,
        UnknownSnapshotRetentionDays = 7,
        KnownAdditionalFaceLimit = 5,
        GpuDecodingEnabled = true,
        CpuFallbackEnabled = true,
        HardwareAcceleration = FfmpegHardwareAcceleration.Auto,
        HealthCheckIntervalSeconds = 60
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

        if (DetectionEnabled && (!FaceDetectionThreshold.HasValue || FaceDetectionThreshold < 0 || FaceDetectionThreshold > 100))
            validationErrors.Add("Face detection threshold must be between 0 and 100 when detection is enabled");

        if (RecognitionEnabled && (!UnknownFaceThreshold.HasValue || UnknownFaceThreshold < 0 || UnknownFaceThreshold > 100))
            validationErrors.Add("Unknown face threshold must be between 0 and 100 when recognition is enabled");

        if (InferenceFps.HasValue && CaptureFpsLimit.HasValue && InferenceFps > CaptureFpsLimit)
            validationErrors.Add("Inference FPS cannot be greater than capture FPS limit");

        if (MaxFacesPerFrame <= 0)
            validationErrors.Add("Max faces per frame must be greater than 0");

        if (MaxConcurrentTracks <= 0)
            validationErrors.Add("Max concurrent tracks must be greater than 0");

        if (RecognitionIntervalPerTrackMs <= 0)
            validationErrors.Add("Recognition interval per track must be greater than 0");

        if (TrackTimeoutMs <= 0)
            validationErrors.Add("Track timeout must be greater than 0");

        if (UnknownSnapshotRetentionDays is < 1 or > 7)
            validationErrors.Add("Unknown snapshot retention must be between 1 and 7 days");

        if (KnownAdditionalFaceLimit is < 0 or > 20)
            validationErrors.Add("Known additional face limit must be between 0 and 20");

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
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
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
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            });
        }
        catch
        {
            return null;
        }
    }
}
