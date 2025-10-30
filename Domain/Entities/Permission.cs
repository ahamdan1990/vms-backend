using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents a system permission that can be granted to roles
/// </summary>
public class Permission
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Permission name (e.g., "User.Create", "Invitation.Read")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Permission category (e.g., "User", "Invitation", "Visitor")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of what this permission allows
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI (e.g., "Create Users")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Risk level: 1=Low, 2=Medium, 3=High, 4=Very High, 5=Critical
    /// </summary>
    [Required]
    public int RiskLevel { get; set; } = 1;

    /// <summary>
    /// Whether this permission is currently active and can be granted
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a system permission that cannot be deleted
    /// </summary>
    [Required]
    public bool IsSystemPermission { get; set; } = true;

    /// <summary>
    /// Display order for UI sorting
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// When this permission was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created this permission (null for system-seeded permissions)
    /// </summary>
    public int? CreatedBy { get; set; }

    // Navigation properties

    /// <summary>
    /// Roles that have been granted this permission
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    /// <summary>
    /// User who created this permission
    /// </summary>
    public User? CreatedByUser { get; set; }
}
