using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.DTOs.Cameras;

public class CameraFaceEventContextDto
{
    public CameraFaceEventDto FaceEvent { get; set; } = null!;
    public PersonContextDto? Person { get; set; }
}

public class PersonContextDto
{
    public int PersonId { get; set; }
    public string PersonType { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public bool IsVip { get; set; }
    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }
    public bool HasFaceEnrollment { get; set; }
    /// <summary>
    /// null = staff (presence not tracked in VMS)
    /// true = currently inside, false = not inside
    /// </summary>
    public bool? IsInsideBuilding { get; set; }
    /// <summary>true when person has an active TemporaryLeave record (went out temporarily)</summary>
    public bool IsTempAway { get; set; }
    public InvitationSummaryDto? ActiveVisit { get; set; }
    public List<InvitationSummaryDto> TodaysInvitations { get; set; } = new();
}

public class InvitationSummaryDto
{
    public int Id { get; set; }
    public string InvitationNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public InvitationStatus Status { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public string? HostName { get; set; }
}
