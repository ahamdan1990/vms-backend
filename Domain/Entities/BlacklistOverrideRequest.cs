namespace VisitorManagementSystem.Api.Domain.Entities;

public class BlacklistOverrideRequest : BaseEntity
{
    public Guid Token { get; set; } = Guid.NewGuid();
    public int VisitorId { get; set; }
    public int RequestedByUserId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending | Granted | Denied
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? Notes { get; set; }

    public Visitor Visitor { get; set; } = null!;
    public User RequestedByUser { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
}
