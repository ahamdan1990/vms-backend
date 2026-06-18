using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.DTOs.Cameras;

/// <summary>
/// Multipart form payload for a client-captured camera frame.
/// </summary>
public class CameraFrameProcessingRequestDto
{
    /// <summary>
    /// JPEG/PNG frame captured by the browser or frame grabber.
    /// </summary>
    public IFormFile? Frame { get; set; }

    /// <summary>
    /// Client-side camera device identifier when the source is a workstation USB camera.
    /// </summary>
    public string? SourceDeviceId { get; set; }

    /// <summary>
    /// Optional caller-supplied track identifier. Real tracker IDs will be assigned by the pipeline phase.
    /// </summary>
    public string? ClientTrackId { get; set; }

    /// <summary>
    /// Timestamp when the client captured the frame.
    /// </summary>
    public DateTime? CapturedAt { get; set; }
}

/// <summary>
/// Result of processing one camera frame through the available face recognition engine.
/// </summary>
public class CameraFrameRecognitionResultDto
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public int CameraId { get; set; }
    public string CameraName { get; set; } = string.Empty;
    public int? CameraLocationId { get; set; }
    public string? CameraLocationName { get; set; }
    public CameraType CameraType { get; set; }
    public CameraRole CameraRole { get; set; } = CameraRole.General;
    public CameraWorkflowMode WorkflowMode { get; set; } = CameraWorkflowMode.Manual;
    public FaceRecognitionEngine PreferredEngine { get; set; } = FaceRecognitionEngine.Hybrid;

    public string EngineUsed { get; set; } = "none";
    public bool FallbackUsed { get; set; }
    public bool RecognitionSkipped { get; set; }
    public string? SkipReason { get; set; }
    public Dictionary<string, object> EngineStatus { get; set; } = new();

    public DateTime CapturedAt { get; set; }
    public DateTime ProcessedAt { get; set; }
    public long FrameSizeBytes { get; set; }
    public string? SourceDeviceId { get; set; }
    public string? ClientTrackId { get; set; }

    public int DetectedFaceCount { get; set; }
    public int RecognizedFaceCount { get; set; }
    public int KnownFaceCount { get; set; }
    public int UnknownFaceCount { get; set; }
    public double RecognitionThreshold { get; set; }
    public double DetectionThreshold { get; set; }

    public string SuggestedWorkflowAction { get; set; } = "none";
    public List<CameraFrameFaceResultDto> Faces { get; set; } = new();
    public List<CameraFrameFaceEventDto> Events { get; set; } = new();

    /// <summary>Raw frame bytes — not serialized to JSON, carried internally for snapshot saving.</summary>
    [JsonIgnore]
    public byte[]? FrameBytes { get; set; }

    public static CameraFrameRecognitionResultDto Failure(
        int cameraId,
        string errorCode,
        string errorMessage)
    {
        return new CameraFrameRecognitionResultDto
        {
            Success = false,
            CameraId = cameraId,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            CapturedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// One detected or recognized face in a processed frame.
/// </summary>
public class CameraFrameFaceResultDto
{
    public int Index { get; set; }
    public bool IsKnown { get; set; }
    public string PersonType { get; set; } = "Unknown";
    public int? PersonId { get; set; }
    public string? SubjectId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? EmployeeId { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string? SnapshotDataUrl { get; set; }

    public double Similarity { get; set; }
    public double Confidence { get; set; }
    public CameraFrameFaceBoxDto? BoundingBox { get; set; }

    public bool IsVip { get; set; }
    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }

    public string SuggestedAction { get; set; } = "none";

    /// <summary>
    /// Raw face template bytes forwarded from the recognition engine for track-level identity comparison.
    /// Not serialized to API responses.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public byte[]? TemplateBytes { get; set; }

    /// <summary>
    /// Recognition threshold (0–1) applied when classifying this face as known/unknown.
    /// Populated by CameraFrameRecognitionService.BuildRecognizedFaceAsync so the event
    /// service can record the exact threshold used for operator review.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public float AppliedThreshold { get; set; }

    /// <summary>
    /// Human-readable rejection reason when IsKnown=false, e.g. "BelowRecognitionThreshold".
    /// Null for truly unknown faces with no candidate.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Luxand composite quality score (0–100). Used by BestFaceEmissionBuffer for cross-frame ranking.
    /// Not serialized to API responses.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public double? DetectionQualityScore { get; set; }

    /// <summary>Left-right head rotation in degrees. Used for cross-frame best-face ranking.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public float? DetectionYaw { get; set; }

    /// <summary>Up-down head rotation in degrees. Used for cross-frame best-face ranking.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public float? DetectionPitch { get; set; }

    /// <summary>In-plane face rotation in degrees. Used for cross-frame best-face ranking.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public float? DetectionRoll { get; set; }
}

/// <summary>
/// Event publication state for one face in a processed frame.
/// </summary>
public class CameraFrameFaceEventDto
{
    public int FaceIndex { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public bool Emitted { get; set; }
    public string? SuppressedReason { get; set; }
    public DateTime? NextAllowedAt { get; set; }
    public int? AlertId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = "none";
    /// <summary>DB Id of the persisted CameraFaceEvent — set after event is emitted and saved.</summary>
    public int? FaceEventDbId { get; set; }
}

/// <summary>
/// Face rectangle in image pixel coordinates.
/// </summary>
public class CameraFrameFaceBoxDto
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double Confidence { get; set; }
}
