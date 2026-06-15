using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Entities;

public class StaffPresence : BaseEntity
{
    public int UserId { get; set; }
    public int? UserReferenceId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public string? UserDepartment { get; set; }
    public string? UserJobTitle { get; set; }
    public DateTime CheckedInAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public StaffPresenceStatus Status { get; set; } = StaffPresenceStatus.Active;
    public int? LocationId { get; set; }
    public int CheckedInById { get; set; }
    public int? CheckedInByReferenceId { get; set; }
    public int? CheckedOutById { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public Location? Location { get; set; }
    public User? CheckedInByUser { get; set; }
    public User? CheckedOutByUser { get; set; }
}
