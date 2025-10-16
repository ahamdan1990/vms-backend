using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Users;

namespace VisitorManagementSystem.Api.Application.Commands.Users
{
    /// <summary>
    /// Command for deactivating a user account
    /// </summary>
    public class DeactivateUserCommand : IRequest<UserDto>
    {
        public int Id { get; set; }
        public int DeactivatedBy { get; set; }
        public string? Reason { get; set; }
        public bool RevokeAllSessions { get; set; } = true;
    }
}
