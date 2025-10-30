using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents a user role with associated permissions
/// </summary>
public class Role
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Role name (e.g., "Staff", "Receptionist", "Administrator")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the role
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Display name for UI
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Hierarchy level for role comparison (1=lowest, 5=highest)
    /// Staff=1, Receptionist=2, Administrator=4
    /// </summary>
    [Required]
    public int HierarchyLevel { get; set; }

    /// <summary>
    /// Whether this is a system role that cannot be deleted
    /// </summary>
    [Required]
    public bool IsSystemRole { get; set; } = false;

    /// <summary>
    /// Whether this role is currently active
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order for UI sorting
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Color code for UI badges (hex format, e.g., "#3B82F6")
    /// </summary>
    [MaxLength(20)]
    public string? Color { get; set; }

    /// <summary>
    /// Icon name for UI display
    /// </summary>
    [MaxLength(50)]
    public string? Icon { get; set; }

    /// <summary>
    /// When this role was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created this role
    /// </summary>
    public int? CreatedBy { get; set; }

    /// <summary>
    /// When this role was last modified
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified this role
    /// </summary>
    public int? ModifiedBy { get; set; }

    // Navigation properties

    /// <summary>
    /// Permissions granted to this role
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    /// <summary>
    /// Users assigned to this role
    /// </summary>
    public ICollection<User> Users { get; set; } = new List<User>();

    /// <summary>
    /// User who created this role
    /// </summary>
    public User? CreatedByUser { get; set; }

    /// <summary>
    /// User who last modified this role
    /// </summary>
    public User? ModifiedByUser { get; set; }
}
