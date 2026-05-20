using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.Services.Common;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.CivilDefense;

public class GetCdStaffSearchQuery : IRequest<List<CdStaffSearchResultDto>>
{
    public string? SearchTerm { get; set; }
    public int PageSize { get; set; } = 30;
}

public class CdStaffSearchResultDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}

public class GetCdStaffSearchQueryHandler : IRequestHandler<GetCdStaffSearchQuery, List<CdStaffSearchResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUrlResolverService _urlResolver;

    public GetCdStaffSearchQueryHandler(IUnitOfWork unitOfWork, IUrlResolverService urlResolver)
    {
        _unitOfWork = unitOfWork;
        _urlResolver = urlResolver;
    }

    public async Task<List<CdStaffSearchResultDto>> Handle(
        GetCdStaffSearchQuery request,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Users
            .GetQueryable()
            .Where(u => u.Status == UserStatus.Active && !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(u =>
                (u.FirstName + " " + u.LastName).ToLower().Contains(term) ||
                (u.Department != null && u.Department.ToLower().Contains(term)) ||
                (u.EmployeeId != null && u.EmployeeId.ToLower().Contains(term)));
        }

        var users = await query
            .AsNoTracking()
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Take(Math.Min(request.PageSize, 100))
            .Select(u => new CdStaffSearchResultDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = (u.FirstName + " " + u.LastName).Trim(),
                Department = u.Department,
                JobTitle = u.JobTitle,
                ProfilePhotoUrl = u.ProfilePhotoPath,
            })
            .ToListAsync(cancellationToken);

        foreach (var u in users)
        {
            if (!string.IsNullOrWhiteSpace(u.ProfilePhotoUrl))
                u.ProfilePhotoUrl = _urlResolver.GetAbsoluteUrl(u.ProfilePhotoUrl);
            else
                u.ProfilePhotoUrl = null;
        }

        return users;
    }
}
