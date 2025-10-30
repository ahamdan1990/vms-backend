using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Permissions;
using VisitorManagementSystem.Api.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Application.Queries.Permissions;

/// <summary>
/// Handler for GetRoleWithPermissionsQuery
/// </summary>
public class GetRoleWithPermissionsQueryHandler : IRequestHandler<GetRoleWithPermissionsQuery, RoleWithPermissionsDto?>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetRoleWithPermissionsQueryHandler> _logger;

    public GetRoleWithPermissionsQueryHandler(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<GetRoleWithPermissionsQueryHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<RoleWithPermissionsDto?> Handle(GetRoleWithPermissionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

            if (role == null)
            {
                _logger.LogWarning("Role with ID {RoleId} not found", request.RoleId);
                return null;
            }

            return _mapper.Map<RoleWithPermissionsDto>(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role with permissions for ID {RoleId}", request.RoleId);
            throw;
        }
    }
}
