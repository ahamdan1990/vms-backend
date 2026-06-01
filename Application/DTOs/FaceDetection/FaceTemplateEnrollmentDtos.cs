namespace VisitorManagementSystem.Api.Application.DTOs.FaceDetection;

public class FaceTemplateDto
{
    public int Id { get; set; }
    public string PersonType { get; set; } = string.Empty;
    public int PersonId { get; set; }
    public string Engine { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public decimal? QualityScore { get; set; }
    public string? SourceImagePath { get; set; }
    public string? SourceImageUrl { get; set; }
    public string? Source { get; set; }
    public DateTime? LastMatchedAt { get; set; }
    public int MatchCount { get; set; }
    public DateTime CreatedOn { get; set; }
    public bool IsActive { get; set; }
}

public class FaceTemplateEnrollmentBatchResultDto
{
    public int VisitorsScanned { get; set; }
    public int UsersScanned { get; set; }
    public int Enrolled { get; set; }
    public int SkippedExisting { get; set; }
    public int SkippedMissingPhoto { get; set; }
    public int Failed { get; set; }
    public List<FaceTemplateEnrollmentItemResultDto> Items { get; set; } = [];
}

public class FaceTemplateEnrollmentItemResultDto
{
    public string PersonType { get; set; } = string.Empty;
    public int PersonId { get; set; }
    public string SubjectId { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }
    public bool Success { get; set; }
    public bool Skipped { get; set; }
    public string? Message { get; set; }
    public string? ImageId { get; set; }
    public int? TemplateId { get; set; }
    public decimal? NewTemplateQuality { get; set; }
    public decimal? PrimaryTemplateQuality { get; set; }
    public bool SuggestPromoteToPrimary { get; set; }

    // Set when enrollment is blocked by a cross-person duplicate match
    public bool IsDuplicate { get; set; }
    public string? ConflictPersonType { get; set; }
    public int? ConflictPersonId { get; set; }
    public float? ConflictSimilarity { get; set; }
}

/// <summary>Result returned by FindCrossPersonDuplicateAsync when a conflict is detected.</summary>
public record FaceDuplicateConflict(string PersonType, int PersonId, float Similarity);

public class CandidateSnapshotDto
{
    public int FaceEventId { get; set; }
    public string? SnapshotUrl { get; set; }
    public float? Similarity { get; set; }
    public float? Confidence { get; set; }
    public string? CameraName { get; set; }
    public DateTime CapturedAt { get; set; }
}
