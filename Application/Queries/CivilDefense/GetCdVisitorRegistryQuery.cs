using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;
using VisitorManagementSystem.Api.Application.Services.Common;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.CivilDefense;

public class GetCdVisitorRegistryQuery : IRequest<CdVisitorRegistryResultDto>
{
    public string? SearchTerm { get; set; }
    public bool? IsVip { get; set; }
    public bool? IsBlacklisted { get; set; }
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 50;
}

public class GetCdVisitorRegistryQueryHandler
    : IRequestHandler<GetCdVisitorRegistryQuery, CdVisitorRegistryResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUrlResolverService _urlResolver;

    public GetCdVisitorRegistryQueryHandler(IUnitOfWork unitOfWork, IUrlResolverService urlResolver)
    {
        _unitOfWork = unitOfWork;
        _urlResolver = urlResolver;
    }

    public async Task<CdVisitorRegistryResultDto> Handle(
        GetCdVisitorRegistryQuery request,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Visitors
            .GetQueryable()
            .Where(v => !v.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(v =>
                (v.FirstName + " " + v.LastName).ToLower().Contains(term) ||
                (v.PhoneNumber != null && v.PhoneNumber.Value.Contains(term)) ||
                (v.Company != null && v.Company.ToLower().Contains(term)));
        }

        if (request.IsVip.HasValue)
            query = query.Where(v => v.IsVip == request.IsVip.Value);

        if (request.IsBlacklisted.HasValue)
            query = query.Where(v => v.IsBlacklisted == request.IsBlacklisted.Value);

        var total = await query.CountAsync(cancellationToken);

        var raw = await query
            .OrderByDescending(v => v.LastVisitDate)
            .ThenByDescending(v => v.Id)
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(v => new
            {
                v.Id,
                Name = (v.FirstName + " " + v.LastName).Trim(),
                Phone = v.PhoneNumber != null ? v.PhoneNumber.Value : null,
                v.Company,
                v.IsVip,
                v.IsBlacklisted,
                v.BlacklistReason,
                v.VisitCount,
                v.LastVisitDate,
                v.ProfilePhotoPath,
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var items = raw.Select(v => new CdVisitorRegistryDto
        {
            Id = v.Id,
            Name = v.Name,
            Phone = v.Phone,
            Company = v.Company,
            IsVip = v.IsVip,
            IsBlacklisted = v.IsBlacklisted,
            BlacklistReason = v.BlacklistReason,
            VisitCount = v.VisitCount,
            LastVisitDate = v.LastVisitDate,
            ProfilePhotoUrl = string.IsNullOrWhiteSpace(v.ProfilePhotoPath)
                ? null : _urlResolver.GetAbsoluteUrl(v.ProfilePhotoPath),
        }).ToList();

        return new CdVisitorRegistryResultDto
        {
            Items = items,
            TotalCount = total,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }
}
