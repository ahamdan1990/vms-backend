namespace VisitorManagementSystem.Api.Application.DTOs.FaceDetection;

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
}
