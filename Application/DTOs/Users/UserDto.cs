// Application/DTOs/Users/UserDto.cs
namespace VisitorManagementSystem.Api.Application.DTOs.Users;

public class UserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Enhanced phone fields
    public string? PhoneNumber { get; set; }
    public string? PhoneCountryCode { get; set; }
    public string? PhoneType { get; set; }

    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? EmployeeId { get; set; }
    public string? ProfilePhotoUrl { get; set; }

    // User preferences
    public string TimeZone { get; set; } = "UTC";
    public string Language { get; set; } = "en-US";
    public string Theme { get; set; } = "light";

    // Enhanced address fields
    public string? AddressType { get; set; }
    public string? Street1 { get; set; }
    public string? Street2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateTime? LastLoginDate { get; set; }
    public DateTime CreatedOn { get; set; }
    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
    public int FailedLoginAttempts { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime? PasswordChangedDate { get; set; }
}