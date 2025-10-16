using VisitorManagementSystem.Api.Application.DTOs.Auth;
using VisitorManagementSystem.Api.Controllers;

namespace VisitorManagementSystem.Api.Application.DTOs.Users;

/// <summary>
/// User list item data transfer object
/// </summary>
public class UserListDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Enhanced phone fields
    /// </summary>
    public string? PhoneNumber { get; set; }
    public string? PhoneCountryCode { get; set; }
    public string? PhoneType { get; set; }

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

    public string? JobTitle { get; set; }
    public string? EmployeeId { get; set; }
    /// <summary>
    /// Last login date
    /// </summary>
    public DateTime? LastLoginDate { get; set; }

    /// <summary>
    /// User creation date
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Whether user is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether user is locked out
    /// </summary>
    public bool IsLockedOut { get; set; }
    public int FailedLoginAttempts { get; set; }
}


