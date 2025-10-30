namespace VisitorManagementSystem.Api.Application.DTOs.Permissions;

/// <summary>
/// DTO for permission category with its permissions
/// </summary>
public class PermissionCategoryDto
{
    /// <summary>
    /// Category name (e.g., "User", "Visitor", "Invitation")
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// List of permissions in this category
    /// </summary>
    public List<PermissionDto> Permissions { get; set; } = new();

    /// <summary>
    /// Total permission count in this category
    /// </summary>
    public int PermissionCount => Permissions.Count;

    /// <summary>
    /// Number of active permissions in this category
    /// </summary>
    public int ActivePermissionCount => Permissions.Count(p => p.IsActive);
}
