using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Permissions;
using VisitorManagementSystem.Api.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Application.Queries.Permissions;

/// <summary>
/// Handler for GetAllRolesQuery
/// </summary>
public class GetAllRolesQueryHandler : IRequestHandler<GetAllRolesQuery, List<RoleDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllRolesQueryHandler> _logger;

    public GetAllRolesQueryHandler(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<GetAllRolesQueryHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<RoleDto>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.Roles.AsQueryable();

            if (request.IsActive.HasValue)
            {
                query = query.Where(r => r.IsActive == request.IsActive.Value);
            }

            var roles = await query
                .OrderBy(r => r.HierarchyLevel)
                .ThenBy(r => r.DisplayOrder)
                .ToListAsync(cancellationToken);

            var roleDtos = _mapper.Map<List<RoleDto>>(roles);

            // Include counts if requested
            if (request.IncludeCounts)
            {
                foreach (var roleDto in roleDtos)
                {
                    roleDto.PermissionCount = await _context.RolePermissions
                        .CountAsync(rp => rp.RoleId == roleDto.Id, cancellationToken);

                    roleDto.UserCount = await _context.Users
                        .CountAsync(u => u.RoleId == roleDto.Id, cancellationToken);
                }
            }

            return roleDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            throw;
        }
    }
}
