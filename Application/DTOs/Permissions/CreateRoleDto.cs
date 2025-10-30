using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Permissions;

/// <summary>
/// DTO for creating a new role
/// </summary>
public class CreateRoleDto
{
    /// <summary>
    /// Role name (unique)
    /// </summary>
    [Required(ErrorMessage = "Role name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Role name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI
    /// </summary>
    [Required(ErrorMessage = "Display name is required")]
    [StringLength(150, ErrorMessage = "Display name cannot exceed 150 characters")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Role description
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Hierarchy level (determines role precedence)
    /// </summary>
    [Range(1, 10, ErrorMessage = "Hierarchy level must be between 1 and 10")]
    public int HierarchyLevel { get; set; }

    /// <summary>
    /// Display order for UI
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Hex color for UI badges
    /// </summary>
    [RegularExpression("^#([A-Fa-f0-9]{6})$", ErrorMessage = "Color must be a valid hex color (e.g., #3B82F6)")]
    public string? Color { get; set; }

    /// <summary>
    /// Icon name for UI
    /// </summary>
    [StringLength(50, ErrorMessage = "Icon name cannot exceed 50 characters")]
    public string? Icon { get; set; }

    /// <summary>
    /// Permission IDs to assign to this role
    /// </summary>
    public List<int> PermissionIds { get; set; } = new();
}
