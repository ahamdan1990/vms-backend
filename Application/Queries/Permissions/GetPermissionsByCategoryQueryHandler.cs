using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Permissions;
using VisitorManagementSystem.Api.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Application.Queries.Permissions;

/// <summary>
/// Handler for GetPermissionsByCategoryQuery
/// </summary>
public class GetPermissionsByCategoryQueryHandler : IRequestHandler<GetPermissionsByCategoryQuery, List<PermissionCategoryDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetPermissionsByCategoryQueryHandler> _logger;

    public GetPermissionsByCategoryQueryHandler(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<GetPermissionsByCategoryQueryHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<PermissionCategoryDto>> Handle(GetPermissionsByCategoryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.Permissions.AsQueryable();

            if (request.IsActive.HasValue)
            {
                query = query.Where(p => p.IsActive == request.IsActive.Value);
            }

            var permissions = await query
                .OrderBy(p => p.Category)
                .ThenBy(p => p.DisplayOrder)
                .ToListAsync(cancellationToken);

            var groupedPermissions = permissions
                .GroupBy(p => p.Category)
                .Select(g => new PermissionCategoryDto
                {
                    CategoryName = g.Key,
                    Permissions = _mapper.Map<List<PermissionDto>>(g.ToList())
                })
                .OrderBy(c => c.CategoryName)
                .ToList();

            return groupedPermissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions by category");
            throw;
        }
    }
}
