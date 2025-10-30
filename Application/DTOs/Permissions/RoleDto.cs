namespace VisitorManagementSystem.Api.Application.DTOs.Permissions;

/// <summary>
/// DTO for role information
/// </summary>
public class RoleDto
{
    /// <summary>
    /// Role ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Role name (e.g., "Staff", "Receptionist", "Administrator")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Role description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Hierarchy level (1=Staff, 2=Receptionist, 4=Administrator)
    /// </summary>
    public int HierarchyLevel { get; set; }

    /// <summary>
    /// Whether this is a system role (cannot be deleted)
    /// </summary>
    public bool IsSystemRole { get; set; }

    /// <summary>
    /// Whether the role is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Display order for UI
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Hex color for UI badges (e.g., "#3B82F6")
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Icon name for UI
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// When the role was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Number of permissions assigned to this role
    /// </summary>
    public int PermissionCount { get; set; }

    /// <summary>
    /// Number of users assigned to this role
    /// </summary>
    public int UserCount { get; set; }
}
