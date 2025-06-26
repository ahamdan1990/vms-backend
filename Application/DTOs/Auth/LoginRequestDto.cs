using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Auth;

/// <summary>
/// Login request data transfer object
/// </summary>
public class LoginRequestDto
{
    /// <summary>
    /// User email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Remember me flag for extended session
    /// </summary>
    public bool RememberMe { get; set; } = false;

    /// <summary>
    /// Device fingerprint for security
    /// </summary>
    public string? DeviceFingerprint { get; set; }
}