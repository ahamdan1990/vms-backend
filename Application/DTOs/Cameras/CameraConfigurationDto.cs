using System.ComponentModel.DataAnnotations;

using VisitorManagementSystem.Api.Domain.Enums;

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
    public int? FacialRecognitionThreshold { get; set; }

    /// <summary>
    /// Face detection confidence threshold (0-100).
    /// </summary>
    [Range(0, 100)]
    public int? FaceDetectionThreshold { get; set; }

    /// <summary>
    /// Threshold below which a detected face is treated as unknown (0-100).
    /// </summary>
    [Range(0, 100)]
    public int? UnknownFaceThreshold { get; set; }

    /// <summary>
    /// Minimum accepted face size in pixels.
    /// </summary>
    [Range(20, 2000)]
    public int? MinimumFaceSizePixels { get; set; }

    /// <summary>
    /// Minimum accepted face quality score (0-100).
    /// </summary>
    [Range(0, 100)]
    public int? FaceQualityThreshold { get; set; }

    /// <summary>
    /// Blur rejection threshold (0-100), interpreted by the pipeline implementation.
    /// </summary>
    [Range(0, 100)]
    public int? BlurThreshold { get; set; }

    /// <summary>
    /// Maximum allowed yaw angle in degrees.
    /// </summary>
    [Range(0, 90)]
    public int? YawLimitDegrees { get; set; }

    /// <summary>
    /// Maximum allowed pitch angle in degrees.
    /// </summary>
    [Range(0, 90)]
    public int? PitchLimitDegrees { get; set; }

    /// <summary>
    /// Maximum allowed roll angle in degrees.
    /// </summary>
    [Range(0, 180)]
    public int? RollLimitDegrees { get; set; }

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
    public int? CaptureFpsLimit { get; set; }

    /// <summary>
    /// Target frames per second that enter face inference.
    /// </summary>
    [Range(1, 60)]
    public int? InferenceFps { get; set; }

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
    /// </summary>
    public string? ExtendedConfiguration { get; set; }
}
