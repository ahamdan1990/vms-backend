using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Durable record of a single face-detection/recognition event from a camera frame.
/// One row per face per emitted alert. Unknown events expire after 7 days;
/// known-candidate events are retained for FIFO template enrichment (up to 5 per person).
/// </summary>
public class CameraFaceEvent : BaseEntity
{
    /// <summary>Client-visible GUID that ties this record to the SignalR/alert payload.</summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");

    public int CameraId { get; set; }
    public string CameraName { get; set; } = string.Empty;
    public int? LocationId { get; set; }

    public DateTime CapturedAt { get; set; }

    public bool IsKnown { get; set; }
    public string PersonType { get; set; } = "Unknown";
    public int? PersonId { get; set; }
    public string? SubjectId { get; set; }

    public float? Similarity { get; set; }
    public float? Confidence { get; set; }

    /// <summary>Relative path under wwwroot, e.g. face-snapshots/unknown/5/2024/01/15/{guid}.jpg</summary>
    public string? SnapshotPath { get; set; }

    public int? BoundingBoxX { get; set; }
    public int? BoundingBoxY { get; set; }
    public int? BoundingBoxWidth { get; set; }
    public int? BoundingBoxHeight { get; set; }

    public string SuggestedAction { get; set; } = "none";

    public FaceEventReviewStatus ReviewStatus { get; set; } = FaceEventReviewStatus.Pending;
    public int? ReviewedById { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }

    /// <summary>True when this snapshot is eligible for FIFO face-template enrichment.</summary>
    public bool IsKnownCandidate { get; set; }

    // ── Rejection metadata (populated for unknown events that had a near-match candidate) ──

    /// <summary>PersonId of the closest matching candidate when the face was rejected (similarity below threshold).</summary>
    public int? BestCandidatePersonId { get; set; }

    /// <summary>PersonType of the closest matching candidate (e.g. "Visitor", "Staff").</summary>
    public string? BestCandidatePersonType { get; set; }

    /// <summary>Raw similarity score (0–1) of the closest candidate that was rejected.</summary>
    public float? BestCandidateSimilarity { get; set; }

    /// <summary>Recognition threshold (0–1) that was applied when this event was created.</summary>
    public float? AppliedThreshold { get; set; }

    /// <summary>Human-readable rejection reason, e.g. "BelowRecognitionThreshold".</summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Null for known events (kept indefinitely).
    /// Set to UtcNow + 7 days for unknown events; the retention job hard-deletes after this.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Linked NotificationAlert id (set after alert is saved).</summary>
    public int? AlertId { get; set; }

    public Camera? Camera { get; set; }
}
