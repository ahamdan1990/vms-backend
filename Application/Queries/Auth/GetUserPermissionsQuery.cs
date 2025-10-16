using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Auth;

namespace VisitorManagementSystem.Api.Application.Queries.Auth
{
    /// <summary>
    /// Query for getting user permissions
    /// </summary>
    public class GetUserPermissionsQuery : IRequest<UserPermissionsDto>
    {
        public int UserId { get; set; }
    }

}
