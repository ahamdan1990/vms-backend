using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Users;

/// <summary>
/// Create user request data transfer object
/// </summary>
public class CreateUserDto
{
    /// <summary>
    /// User first name
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User last name
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User phone number
    /// </summary>
    [Phone(ErrorMessage = "Please enter a valid phone number")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// User role
    /// </summary>
    [Required(ErrorMessage = "Role is required")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// User department
    /// </summary>
    [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters")]
    public string? Department { get; set; }

    /// <summary>
    /// User job title
    /// </summary>
    [StringLength(100, ErrorMessage = "Job title cannot exceed 100 characters")]
    public string? JobTitle { get; set; }

    /// <summary>
    /// Employee ID
    /// </summary>
    [StringLength(50, ErrorMessage = "Employee ID cannot exceed 50 characters")]
    public string? EmployeeId { get; set; }

    /// <summary>
    /// User timezone
    /// </summary>
    [StringLength(50, ErrorMessage = "Timezone cannot exceed 50 characters")]
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// User language preference
    /// </summary>
    [StringLength(10, ErrorMessage = "Language cannot exceed 10 characters")]
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// User theme preference
    /// </summary>
    [StringLength(20, ErrorMessage = "Theme cannot exceed 20 characters")]
    public string Theme { get; set; } = "light";

    /// <summary>
    /// Whether user must change password on first login
    /// </summary>
    public bool MustChangePassword { get; set; } = true;

    /// <summary>
    /// Send welcome email to user
    /// </summary>
    public bool SendWelcomeEmail { get; set; } = true;
}