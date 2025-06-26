namespace VisitorManagementSystem.Api.Application.DTOs.Auth;

/// <summary>
/// Current user information data transfer object
/// </summary>
public class CurrentUserDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User role
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// User status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// User department
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// User job title
    /// </summary>
    public string? JobTitle { get; set; }

    /// <summary>
    /// Employee ID
    /// </summary>
    public string? EmployeeId { get; set; }

    /// <summary>
    /// Profile photo URL
    /// </summary>
    public string? ProfilePhotoUrl { get; set; }

    /// <summary>
    /// User timezone
    /// </summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// User language preference
    /// </summary>
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// User theme preference
    /// </summary>
    public string Theme { get; set; } = "light";

    /// <summary>
    /// User permissions
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// Last login date
    /// </summary>
    public DateTime? LastLoginDate { get; set; }

    /// <summary>
    /// Password changed date
    /// </summary>
    public DateTime? PasswordChangedDate { get; set; }
}
