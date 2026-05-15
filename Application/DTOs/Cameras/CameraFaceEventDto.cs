using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.DTOs.Cameras;

public class CameraFaceEventDto
{
    public int Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public int CameraId { get; set; }
    public string CameraName { get; set; } = string.Empty;
    public int? LocationId { get; set; }
    public DateTime CapturedAt { get; set; }

    public bool IsKnown { get; set; }
    public string PersonType { get; set; } = "Unknown";
    public int? PersonId { get; set; }
    public string? SubjectId { get; set; }
    public string? FullName { get; set; }

    public float? Similarity { get; set; }
    public float? Confidence { get; set; }

    public string? SnapshotUrl { get; set; }

    public int? BoundingBoxX { get; set; }
    public int? BoundingBoxY { get; set; }
    public int? BoundingBoxWidth { get; set; }
    public int? BoundingBoxHeight { get; set; }

    public string SuggestedAction { get; set; } = "none";
    public FaceEventReviewStatus ReviewStatus { get; set; }
    public int? ReviewedById { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }

    public bool IsKnownCandidate { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? AlertId { get; set; }
}

public class CameraFaceEventPagedDto
{
    public List<CameraFaceEventDto> Events { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class ReviewFaceEventRequestDto
{
    public FaceEventReviewStatus Status { get; set; }
    public string? Notes { get; set; }
    /// <summary>
    /// If provided, links this face event to an existing visitor/staff person.
    /// Format: "visitor:42" or "staff:17"
    /// </summary>
    public string? MatchedPersonKey { get; set; }
}
