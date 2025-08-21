using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Users;

/// <summary>
/// User profile data transfer object
/// </summary>
public class UserProfileDto : UserDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public new int Id { get; set; }

    /// <summary>
    /// User first name
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
    public new string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User last name
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
    public new string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public new string Email { get; set; } = string.Empty;

    /// <summary>
    /// User phone number
    /// </summary>
    [Phone(ErrorMessage = "Please enter a valid phone number")]
    public new string? PhoneNumber { get; set; }

    /// <summary>
    /// Phone country code
    /// </summary>
    public new string? PhoneCountryCode { get; set; }

    /// <summary>
    /// Phone type (Mobile, Landline, etc.)
    /// </summary>
    public new string? PhoneType { get; set; }

    /// <summary>
    /// User timezone
    /// </summary>
    [StringLength(50, ErrorMessage = "Timezone cannot exceed 50 characters")]
    public new string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// User language preference
    /// </summary>
    [StringLength(10, ErrorMessage = "Language cannot exceed 10 characters")]
    public new string Language { get; set; } = "en-US";

    /// <summary>
    /// User theme preference
    /// </summary>
    [StringLength(20, ErrorMessage = "Theme cannot exceed 20 characters")]
    public new string Theme { get; set; } = "light";

    /// <summary>
    /// Profile photo URL
    /// </summary>
    public new string? ProfilePhotoUrl { get; set; }

    /// <summary>
    /// Employee ID
    /// </summary>
    public new string? EmployeeId { get; set; }

    /// <summary>
    /// Department
    /// </summary>
    public new string? Department { get; set; }

    /// <summary>
    /// Job title
    /// </summary>
    public new string? JobTitle { get; set; }

    // Enhanced address fields
    /// <summary>
    /// Address type (Home, Work, etc.)
    /// </summary>
    public new string? AddressType { get; set; }

    /// <summary>
    /// Street address line 1
    /// </summary>
    public new string? Street1 { get; set; }

    /// <summary>
    /// Street address line 2
    /// </summary>
    public new string? Street2 { get; set; }

    /// <summary>
    /// City
    /// </summary>
    public new string? City { get; set; }

    /// <summary>
    /// State or province
    /// </summary>
    public new string? State { get; set; }

    /// <summary>
    /// Postal or ZIP code
    /// </summary>
    public new string? PostalCode { get; set; }

    /// <summary>
    /// Country
    /// </summary>
    public new string? Country { get; set; }

    /// <summary>
    /// Latitude coordinate
    /// </summary>
    public new double? Latitude { get; set; }

    /// <summary>
    /// Longitude coordinate
    /// </summary>
    public new double? Longitude { get; set; }
}
