using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Permissions;

/// <summary>
/// DTO for revoking permissions from a role
/// </summary>
public class RevokePermissionsDto
{
    /// <summary>
    /// List of permission IDs to revoke
    /// </summary>
    [Required(ErrorMessage = "Permission IDs are required")]
    [MinLength(1, ErrorMessage = "At least one permission ID is required")]
    public List<int> PermissionIds { get; set; } = new();

    /// <summary>
    /// Optional reason for revoking these permissions
    /// </summary>
    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string? Reason { get; set; }
}
