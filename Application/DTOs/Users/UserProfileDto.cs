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
    public int Id { get; set; }

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
    /// Phone country code
    /// </summary>
    public string? PhoneCountryCode { get; set; }

    /// <summary>
    /// Phone type (Mobile, Landline, etc.)
    /// </summary>
    public string? PhoneType { get; set; }

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
    /// Profile photo URL
    /// </summary>
    public string? ProfilePhotoUrl { get; set; }

    /// <summary>
    /// Employee ID
    /// </summary>
    public string? EmployeeId { get; set; }

    /// <summary>
    /// Department
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// Job title
    /// </summary>
    public string? JobTitle { get; set; }

    // Enhanced address fields
    /// <summary>
    /// Address type (Home, Work, etc.)
    /// </summary>
    public string? AddressType { get; set; }

    /// <summary>
    /// Street address line 1
    /// </summary>
    public string? Street1 { get; set; }

    /// <summary>
    /// Street address line 2
    /// </summary>
    public string? Street2 { get; set; }

    /// <summary>
    /// City
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// State or province
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Postal or ZIP code
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Country
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Latitude coordinate
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Longitude coordinate
    /// </summary>
    public double? Longitude { get; set; }
}
