using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Permissions;

/// <summary>
/// DTO for granting permissions to a role
/// </summary>
public class GrantPermissionsDto
{
    /// <summary>
    /// List of permission IDs to grant
    /// </summary>
    [Required(ErrorMessage = "Permission IDs are required")]
    [MinLength(1, ErrorMessage = "At least one permission ID is required")]
    public List<int> PermissionIds { get; set; } = new();

    /// <summary>
    /// Optional reason for granting these permissions
    /// </summary>
    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string? Reason { get; set; }
}
