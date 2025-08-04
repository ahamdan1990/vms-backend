using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Controllers; // For RoleDto
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Queries.Users;

/// <summary>
/// Handler for GetAvailableRolesQuery
/// </summary>
public class GetAvailableRolesQueryHandler : IRequestHandler<GetAvailableRolesQuery, List<RoleDto>>
{
    private readonly ILogger<GetAvailableRolesQueryHandler> _logger;

    public GetAvailableRolesQueryHandler(ILogger<GetAvailableRolesQueryHandler> logger)
    {
        _logger = logger;
    }

    public async Task<List<RoleDto>> Handle(GetAvailableRolesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing GetAvailableRolesQuery for UserId: {UserId}", request.UserId);

            var roles = new List<RoleDto>();

            foreach (UserRole role in Enum.GetValues<UserRole>())
            {
                roles.Add(new RoleDto
                {
                    Name = role.ToString(),
                    DisplayName = role.ToString(),
                    Description = GetRoleDescription(role),
                    HierarchyLevel = GetRoleLevel(role),
                    CanAssign = true // You can add logic here to determine if user can assign this role
                });
            }

            return await Task.FromResult(roles.OrderBy(r => r.HierarchyLevel).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetAvailableRolesQuery");
            throw;
        }
    }

    private static string GetRoleDescription(UserRole role)
    {
        return role switch
        {
            UserRole.Administrator => "Full system access with all administrative privileges",
            UserRole.Staff => "Standard user with invitation management capabilities",
            UserRole.Operator => "Front desk operations with check-in/out and walk-in management",
            _ => role.ToString()
        };
    }

    private static int GetRoleLevel(UserRole role)
    {
        return role switch
        {
            UserRole.Administrator => 1,
            UserRole.Staff => 2,
            UserRole.Operator => 3,
            _ => 99
        };
    }
}