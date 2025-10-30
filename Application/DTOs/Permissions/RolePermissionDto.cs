namespace VisitorManagementSystem.Api.Application.DTOs.Permissions;

/// <summary>
/// DTO for role-permission mapping information
/// </summary>
public class RolePermissionDto
{
    /// <summary>
    /// Role-Permission mapping ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Role ID
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Role name
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Permission ID
    /// </summary>
    public int PermissionId { get; set; }

    /// <summary>
    /// Permission name
    /// </summary>
    public string PermissionName { get; set; } = string.Empty;

    /// <summary>
    /// Permission display name
    /// </summary>
    public string PermissionDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// When this permission was granted to the role
    /// </summary>
    public DateTime GrantedAt { get; set; }

    /// <summary>
    /// User ID who granted this permission
    /// </summary>
    public int GrantedBy { get; set; }

    /// <summary>
    /// User name who granted this permission
    /// </summary>
    public string GrantedByUserName { get; set; } = string.Empty;
}
