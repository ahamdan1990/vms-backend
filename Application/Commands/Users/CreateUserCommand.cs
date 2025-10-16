// Application/Commands/Users/CreateUserCommand.cs
using MediatR;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Application.DTOs.Users;

namespace VisitorManagementSystem.Api.Application.Commands.Users;

public class CreateUserCommand : IRequest<UserDto>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Enhanced phone fields
    public string? PhoneNumber { get; set; }
    public string? PhoneCountryCode { get; set; }
    public string? PhoneType { get; set; } = "Mobile";

    public UserRole Role { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? EmployeeId { get; set; }

    // User preferences
    public string? TimeZone { get; set; }
    public string? Language { get; set; }
    public string? Theme { get; set; }

    // Enhanced address fields
    public string? AddressType { get; set; } = "Home";
    public string? Street1 { get; set; }
    public string? Street2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public bool MustChangePassword { get; set; } = true;
    public bool SendWelcomeEmail { get; set; } = true;
    public string? TemporaryPassword { get; set; }
    public int CreatedBy { get; set; }
}