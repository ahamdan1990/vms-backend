using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Users;

namespace VisitorManagementSystem.Api.Application.Queries.Users;

/// <summary>
/// Query to get current user's profile information
/// </summary>
public class GetCurrentUserProfileQuery : IRequest<UserProfileDto>
{
    public int UserId { get; set; }
}
