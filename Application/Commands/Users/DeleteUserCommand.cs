using MediatR;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Application.Services.Auth;

namespace VisitorManagementSystem.Api.Application.Commands.Users;

/// <summary>
/// Command for deleting a user (soft delete)
/// </summary>
public class DeleteUserCommand : IRequest<bool>
{
    public int Id { get; set; }
    public int DeletedBy { get; set; }
    public string? Reason { get; set; }
    public bool HardDelete { get; set; } = false; // For emergency situations only
}