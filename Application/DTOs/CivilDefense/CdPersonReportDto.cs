namespace VisitorManagementSystem.Api.Application.DTOs.CivilDefense;

public class CdPersonReportDto
{
    public PersonSummaryDto Person { get; set; } = null!;
    public List<PersonVisitRecordDto> Records { get; set; } = new();
    public List<PersonFaceEventDto> FaceEvents { get; set; } = new();
}

public class PersonSummaryDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string PersonType { get; set; } = string.Empty;
    public bool IsVip { get; set; }
    public bool IsBlacklisted { get; set; }
}

public class PersonVisitRecordDto
{
    public int Id { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Host { get; set; }
    public string? Notes { get; set; }
}

public class PersonFaceEventDto
{
    public int Id { get; set; }
    public DateTime CapturedAt { get; set; }
    public string? SnapshotUrl { get; set; }
    public double? Similarity { get; set; }
    public string? CameraName { get; set; }
}
