namespace VisitorManagementSystem.Api.Domain.Entities;

public enum TemporaryLeavePersonType { Staff = 1, Visitor = 2 }

public class TemporaryLeave : BaseEntity
{
    public TemporaryLeavePersonType PersonType { get; set; }
    public int? StaffPresenceId { get; set; }
    public int? InvitationId { get; set; }
    public DateTime LeftAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public string? Reason { get; set; }
    public int RecordedById { get; set; }
    public int? RecordedByReferenceId { get; set; }
    public int? ReturnedById { get; set; }

    // Navigation
    public StaffPresence? StaffPresence { get; set; }
    public Invitation? Invitation { get; set; }
    public User? RecordedByUser { get; set; }
    public User? ReturnedByUser { get; set; }
}
