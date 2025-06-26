// Update GetAvailableRolesQuery to return RoleDto instead of SelectListItemDto
using MediatR;
using VisitorManagementSystem.Api.Controllers; // For RoleDto

namespace VisitorManagementSystem.Api.Application.Queries.Users;

/// <summary>
/// Query for getting available user roles
/// </summary>
public class GetAvailableRolesQuery : IRequest<List<RoleDto>>
{
    public int UserId { get; set; } // Add missing UserId property
    public bool IncludeDescriptions { get; set; } = false;
}