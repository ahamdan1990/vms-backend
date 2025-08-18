// Application/DTOs/Users/CreateUserDto.cs
using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Users;

public class CreateUserDto
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;

    // Enhanced phone fields
    [Phone(ErrorMessage = "Please enter a valid phone number")]
    public string? PhoneNumber { get; set; }

    public string? PhoneCountryCode { get; set; }

    public string PhoneType { get; set; } = "Mobile";

    [Required(ErrorMessage = "Role is required")]
    public string Role { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters")]
    public string? Department { get; set; }

    [StringLength(100, ErrorMessage = "Job title cannot exceed 100 characters")]
    public string? JobTitle { get; set; }

    [StringLength(50, ErrorMessage = "Employee ID cannot exceed 50 characters")]
    public string? EmployeeId { get; set; }

    // User preferences
    [StringLength(50, ErrorMessage = "Timezone cannot exceed 50 characters")]
    public string TimeZone { get; set; } = "UTC";

    [StringLength(10, ErrorMessage = "Language cannot exceed 10 characters")]
    public string Language { get; set; } = "en-US";

    [StringLength(20, ErrorMessage = "Theme cannot exceed 20 characters")]
    public string Theme { get; set; } = "light";

    // Enhanced address fields
    public string? AddressType { get; set; } = "Home";

    [StringLength(100, ErrorMessage = "Street address cannot exceed 100 characters")]
    public string? Street1 { get; set; }

    [StringLength(100, ErrorMessage = "Street address line 2 cannot exceed 100 characters")]
    public string? Street2 { get; set; }

    [StringLength(50, ErrorMessage = "City cannot exceed 50 characters")]
    public string? City { get; set; }

    [StringLength(50, ErrorMessage = "State cannot exceed 50 characters")]
    public string? State { get; set; }

    [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
    public string? PostalCode { get; set; }

    [StringLength(50, ErrorMessage = "Country cannot exceed 50 characters")]
    public string? Country { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public bool MustChangePassword { get; set; } = true;
    public bool SendWelcomeEmail { get; set; } = true;
}