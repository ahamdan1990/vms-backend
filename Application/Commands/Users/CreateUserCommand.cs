using MediatR;
using AutoMapper;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Application.DTOs.Users;

namespace VisitorManagementSystem.Api.Application.Commands.Users;

/// <summary>
/// Command for creating a new user
/// </summary>
public class CreateUserCommand : IRequest<UserDto>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? EmployeeId { get; set; }
    public string? TimeZone { get; set; }
    public string? Language { get; set; }
    public bool MustChangePassword { get; set; } = true;
    public string? TemporaryPassword { get; set; }
    public int CreatedBy { get; set; }
}

