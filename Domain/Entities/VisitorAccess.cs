using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents user access to a visitor record (many-to-many relationship)
/// Tracks which users can view and manage specific visitors
/// </summary>
public class VisitorAccess : BaseEntity
{
    /// <summary>
    /// The user who has access to the visitor
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// The visitor that the user has access to
    /// </summary>
    public int VisitorId { get; set; }

    /// <summary>
    /// The type of access (Creator, SharedDuplicate, GrantedByAdmin)
    /// </summary>
    public VisitorAccessType AccessType { get; set; }

    /// <summary>
    /// User ID who granted the access (for audit trail)
    /// Null if system-granted (e.g., duplicate detection)
    /// </summary>
    public int? GrantedBy { get; set; }

    /// <summary>
    /// When the access was granted
    /// </summary>
    public DateTime GrantedOn { get; set; }

    // Navigation properties

    /// <summary>
    /// The user who has access
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// The visitor being accessed
    /// </summary>
    public Visitor Visitor { get; set; } = null!;

    /// <summary>
    /// The user who granted the access
    /// </summary>
    public User? GrantedByUser { get; set; }
}
