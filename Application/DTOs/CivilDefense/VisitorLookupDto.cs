namespace VisitorManagementSystem.Api.Application.DTOs.CivilDefense;

public class VisitorLookupDto
{
    public int VisitorId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public bool IsCurrentlyInside { get; set; }
    public int? ActiveInvitationId { get; set; }
    public bool IsVip { get; set; }
    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }
}
