using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Users;

/// <summary>
/// Update user profile request data transfer object for self-service profile updates
/// Excludes admin-only fields like role and status
/// </summary>
public class UpdateUserProfileDto
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
    /// Phone country code
    /// </summary>
    public string? PhoneCountryCode { get; set; }

    /// <summary>
    /// Phone type (Mobile, Landline, etc.)
    /// </summary>
    public string? PhoneType { get; set; }

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
    /// Address type (Home, Work, etc.)
    /// </summary>
    public string? AddressType { get; set; }

    /// <summary>
    /// Street address line 1
    /// </summary>
    [StringLength(100, ErrorMessage = "Street address cannot exceed 100 characters")]
    public string? Street1 { get; set; }

    /// <summary>
    /// Street address line 2 (optional)
    /// </summary>
    [StringLength(100, ErrorMessage = "Street address line 2 cannot exceed 100 characters")]
    public string? Street2 { get; set; }

    /// <summary>
    /// City name
    /// </summary>
    [StringLength(50, ErrorMessage = "City cannot exceed 50 characters")]
    public string? City { get; set; }

    /// <summary>
    /// State or province
    /// </summary>
    [StringLength(50, ErrorMessage = "State cannot exceed 50 characters")]
    public string? State { get; set; }

    /// <summary>
    /// Postal or ZIP code
    /// </summary>
    [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Country name
    /// </summary>
    [StringLength(50, ErrorMessage = "Country cannot exceed 50 characters")]
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