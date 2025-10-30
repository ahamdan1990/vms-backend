using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents the many-to-many relationship between roles and permissions
/// </summary>
public class RolePermission
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Role
    /// </summary>
    [Required]
    public int RoleId { get; set; }

    /// <summary>
    /// Foreign key to Permission
    /// </summary>
    [Required]
    public int PermissionId { get; set; }

    /// <summary>
    /// When this permission was granted to the role
    /// </summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who granted this permission to the role
    /// </summary>
    [Required]
    public int GrantedBy { get; set; }

    // Navigation properties

    /// <summary>
    /// The role this permission is granted to
    /// </summary>
    public Role Role { get; set; } = null!;

    /// <summary>
    /// The permission being granted
    /// </summary>
    public Permission Permission { get; set; } = null!;

    /// <summary>
    /// User who granted this permission
    /// </summary>
    public User GrantedByUser { get; set; } = null!;
}
