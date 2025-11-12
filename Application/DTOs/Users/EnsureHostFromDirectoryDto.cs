using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Users;

/// <summary>
/// Request payload for ensuring a host exists by pulling them from the corporate directory.
/// </summary>
public class EnsureHostFromDirectoryDto
{
    /// <summary>
    /// Username, sAMAccountName or email that identifies the directory user.
    /// </summary>
    [Required]
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Optional email if the identifier is not an email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Optional hint for first name (used when directory response is missing and we rehydrate from UI).
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Optional hint for last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Optional target role for the imported host (Staff, Receptionist, Administrator).
    /// </summary>
    public string? Role { get; set; }
}
