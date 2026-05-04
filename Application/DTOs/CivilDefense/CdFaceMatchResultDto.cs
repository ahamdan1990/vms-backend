namespace VisitorManagementSystem.Api.Application.DTOs.CivilDefense;

public class CdFaceMatchResultDto
{
    public bool FaceDetected { get; set; }
    public bool Matched { get; set; }
    public double Similarity { get; set; }
    public double Confidence { get; set; }
    public int? VisitorId { get; set; }
    public string? VisitorName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? NationalId { get; set; }
    public bool IsCurrentlyInside { get; set; }
    public int? ActiveInvitationId { get; set; }
    public bool IsBlacklisted { get; set; }
    public bool IsStaff { get; set; }
    public int? UserId { get; set; }
    public int? CameraId { get; set; }
    public string CameraRole { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
