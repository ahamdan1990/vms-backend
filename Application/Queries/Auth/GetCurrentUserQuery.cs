using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Auth;

namespace VisitorManagementSystem.Api.Application.Queries.Auth
{
    /// <summary>
    /// Query for getting current user information
    /// UPDATED: Now uses UserId from JWT claims instead of AccessToken
    /// </summary>
    public class GetCurrentUserQuery : IRequest<CurrentUserDto?>
    {
        public int UserId { get; set; }
    }
}