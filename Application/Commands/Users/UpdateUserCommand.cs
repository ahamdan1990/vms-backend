using MediatR;
using AutoMapper;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;
using VisitorManagementSystem.Api.Application.DTOs.Users;

namespace VisitorManagementSystem.Api.Application.Commands.Users;

/// <summary>
/// Command for updating an existing user
/// </summary>
public class UpdateUserCommand : IRequest<UserDto>
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // Enhanced phone fields
    public string? PhoneNumber { get; set; }
    public string? PhoneCountryCode { get; set; }
    public string? PhoneType { get; set; }
    
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? EmployeeId { get; set; }
    
    // User preferences
    public string? TimeZone { get; set; }
    public string? Language { get; set; }
    public string? Theme { get; set; }
    
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
    
    public int ModifiedBy { get; set; }
    public bool UpdateSecurityStamp { get; set; } = false;
}

