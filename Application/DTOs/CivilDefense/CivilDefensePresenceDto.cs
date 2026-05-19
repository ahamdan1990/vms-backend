namespace VisitorManagementSystem.Api.Application.DTOs.CivilDefense;

public class StaffPresenceEntryDto
{
    public int StaffPresenceId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public DateTime CheckedInAt { get; set; }
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsTemporarilyAbsent { get; set; }
}

public class VisitorPresenceEntryDto
{
    public int InvitationId { get; set; }
    public int VisitorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public bool? IsCivilian { get; set; }
    public string? AffiliatedOrganization { get; set; }
    public int HostId { get; set; }
    public string HostName { get; set; } = string.Empty;
    public string? HostDepartment { get; set; }
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string? Purpose { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public string Status { get; set; } = "Active";
    public string? ProfilePhotoUrl { get; set; }
    public bool IsTemporarilyAbsent { get; set; }
    public int? ActiveTemporaryLeaveId { get; set; }
}

public class CivilDefensePresenceSummaryDto
{
    public int TotalInside { get; set; }
    public int StaffCount { get; set; }
    public int VisitorCount { get; set; }
}

public class CivilDefensePresenceDto
{
    public List<StaffPresenceEntryDto> Staff { get; set; } = new();
    public List<VisitorPresenceEntryDto> Visitors { get; set; } = new();
    public CivilDefensePresenceSummaryDto Summary { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
