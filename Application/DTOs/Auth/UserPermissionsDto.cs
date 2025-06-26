namespace VisitorManagementSystem.Api.Application.DTOs.Auth;

/// <summary>
/// User permissions data transfer object
/// </summary>
public class UserPermissionsDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// User role
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// User permissions
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// Permissions grouped by category
    /// </summary>
    public Dictionary<string, List<string>> PermissionsByCategory { get; set; } = new();

    /// <summary>
    /// Whether user has elevated privileges
    /// </summary>
    public bool HasElevatedPrivileges { get; set; }
}