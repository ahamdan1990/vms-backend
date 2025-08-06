using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Users;

/// <summary>
/// Update user preferences request data transfer object
/// Handles only user-controllable preference settings
/// </summary>
public class UpdateUserPreferencesDto
{
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
}