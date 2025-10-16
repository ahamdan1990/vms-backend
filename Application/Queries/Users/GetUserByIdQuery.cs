using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Users;

/// <summary>
/// Query for getting a specific user by ID
/// </summary>
public class GetUserByIdQuery : IRequest<UserDetailDto?>
{
    public int UserId { get; set; }
    public bool IncludeActivity { get; set; } = true; // Default to true for user details
    public bool IncludeSessions { get; set; } = true; // Default to true for user details
}