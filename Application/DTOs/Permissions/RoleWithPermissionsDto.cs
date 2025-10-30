namespace VisitorManagementSystem.Api.Application.DTOs.Permissions;

/// <summary>
/// DTO for role with its permissions
/// </summary>
public class RoleWithPermissionsDto
{
    /// <summary>
    /// Role ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Role name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Role description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Hierarchy level
    /// </summary>
    public int HierarchyLevel { get; set; }

    /// <summary>
    /// Whether this is a system role
    /// </summary>
    public bool IsSystemRole { get; set; }

    /// <summary>
    /// Whether the role is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Color for UI
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Icon for UI
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// List of permissions assigned to this role
    /// </summary>
    public List<PermissionDto> Permissions { get; set; } = new();

    /// <summary>
    /// Total permission count
    /// </summary>
    public int PermissionCount => Permissions.Count;
}
