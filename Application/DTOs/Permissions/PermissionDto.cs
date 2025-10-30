namespace VisitorManagementSystem.Api.Application.DTOs.Permissions;

/// <summary>
/// DTO for permission information
/// </summary>
public class PermissionDto
{
    /// <summary>
    /// Permission ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Permission name (e.g., "User.Create")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Permission category (e.g., "User", "Visitor")
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Risk level (1=Low, 2=Medium, 3=High, 4=Very High, 5=Critical)
    /// </summary>
    public int RiskLevel { get; set; }

    /// <summary>
    /// Whether the permission is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether this is a system permission (cannot be deleted)
    /// </summary>
    public bool IsSystemPermission { get; set; }

    /// <summary>
    /// Display order for UI sorting
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// When the permission was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
