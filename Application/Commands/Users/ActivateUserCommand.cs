using MediatR;
using AutoMapper;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.Services.Auth;

namespace VisitorManagementSystem.Api.Application.Commands.Users;

/// <summary>
/// Command for activating a user account
/// </summary>
public class ActivateUserCommand : IRequest<UserDto>
{
    public int Id { get; set; }
    public int ActivatedBy { get; set; }
    public string? Reason { get; set; }
    public bool ResetFailedAttempts { get; set; } = true;
}

