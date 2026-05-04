using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Application.Queries.Users;

public class GetAvailableRolesQueryHandler : IRequestHandler<GetAvailableRolesQuery, List<UserRoleDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetAvailableRolesQueryHandler> _logger;

    public GetAvailableRolesQueryHandler(ApplicationDbContext context, ILogger<GetAvailableRolesQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<UserRoleDto>> Handle(GetAvailableRolesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing GetAvailableRolesQuery for UserId: {UserId}", request.UserId);

        var roles = await _context.Roles
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.HierarchyLevel)
            .ThenBy(r => r.DisplayOrder)
            .Select(r => new UserRoleDto
            {
                Name        = r.Name,
                DisplayName = r.DisplayName ?? r.Name,
                Description = r.Description ?? string.Empty,
                HierarchyLevel = r.HierarchyLevel,
                CanAssign   = true
            })
            .ToListAsync(cancellationToken);

        return roles;
    }
}