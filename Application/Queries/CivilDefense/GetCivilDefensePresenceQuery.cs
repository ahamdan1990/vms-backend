using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;
using VisitorManagementSystem.Api.Application.Services.Common;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.CivilDefense;

public class GetCivilDefensePresenceQuery : IRequest<CivilDefensePresenceDto>
{
    public int? LocationId { get; set; }
    public string? SearchTerm { get; set; }
    public string? Type { get; set; } // "Staff" | "Visitor" | null = all
}

public class GetCivilDefensePresenceQueryHandler
    : IRequestHandler<GetCivilDefensePresenceQuery, CivilDefensePresenceDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUrlResolverService _urlResolver;

    public GetCivilDefensePresenceQueryHandler(IUnitOfWork unitOfWork, IUrlResolverService urlResolver)
    {
        _unitOfWork = unitOfWork;
        _urlResolver = urlResolver;
    }

    private string? ResolvePhoto(string? relativePath) =>
        string.IsNullOrWhiteSpace(relativePath) ? null : _urlResolver.GetAbsoluteUrl(relativePath);

    public async Task<CivilDefensePresenceDto> Handle(
        GetCivilDefensePresenceQuery request,
        CancellationToken cancellationToken)
    {
        var staff = new List<StaffPresenceEntryDto>();
        var visitors = new List<VisitorPresenceEntryDto>();

        if (request.Type == null || request.Type == "Staff")
        {
            var staffQuery = _unitOfWork.Repository<Domain.Entities.StaffPresence>()
                .GetQueryable()
                .Include(sp => sp.User)
                .Include(sp => sp.Location)
                .Where(sp => sp.Status == StaffPresenceStatus.Active
                          || sp.Status == StaffPresenceStatus.TemporarilyAbsent);

            if (request.LocationId.HasValue)
                staffQuery = staffQuery.Where(sp => sp.LocationId == request.LocationId.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                staffQuery = staffQuery.Where(sp =>
                    (sp.User.FirstName + " " + sp.User.LastName).ToLower().Contains(term) ||
                    sp.User.Department!.ToLower().Contains(term));
            }

            var staffEntities = await staffQuery
                .AsNoTracking()
                .OrderByDescending(sp => sp.CheckedInAt)
                .ToListAsync(cancellationToken);

            staff = staffEntities.Select(sp => new StaffPresenceEntryDto
            {
                StaffPresenceId = sp.Id,
                UserId = sp.UserId,
                Name = $"{sp.User.FirstName} {sp.User.LastName}".Trim(),
                Department = sp.User.Department,
                JobTitle = sp.User.JobTitle,
                ProfilePhotoUrl = ResolvePhoto(sp.User.ProfilePhotoPath),
                CheckedInAt = sp.CheckedInAt,
                LocationId = sp.LocationId,
                LocationName = sp.Location?.Name,
                Status = sp.Status.ToString(),
                IsTemporarilyAbsent = sp.Status == StaffPresenceStatus.TemporarilyAbsent
            }).ToList();
        }

        if (request.Type == null || request.Type == "Visitor")
        {
            var visitorQuery = _unitOfWork.Invitations
                .GetQueryable()
                .Include(i => i.Visitor)
                .Include(i => i.Host)
                .Include(i => i.Location)
                .Include(i => i.VisitPurpose)
                .Where(i => i.Status == InvitationStatus.Active && !i.IsDeleted);

            if (request.LocationId.HasValue)
                visitorQuery = visitorQuery.Where(i => i.LocationId == request.LocationId.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                visitorQuery = visitorQuery.Where(i =>
                    (i.Visitor.FirstName + " " + i.Visitor.LastName).ToLower().Contains(term) ||
                    i.Visitor.Company!.ToLower().Contains(term));
            }

            var invitations = await visitorQuery
                .AsNoTracking()
                .OrderByDescending(i => i.CheckedInAt)
                .ToListAsync(cancellationToken);

            // Load active temp leaves for these invitations
            var invitationIds = invitations.Select(i => i.Id).ToList();
            var activeTempLeaves = await _unitOfWork.Repository<Domain.Entities.TemporaryLeave>()
                .GetQueryable()
                .Where(tl => tl.InvitationId != null
                          && invitationIds.Contains(tl.InvitationId!.Value)
                          && tl.IsActive)
                .AsNoTracking()
                .ToDictionaryAsync(tl => tl.InvitationId!.Value, tl => tl.Id, cancellationToken);

            visitors = invitations.Select(i => new VisitorPresenceEntryDto
            {
                InvitationId = i.Id,
                VisitorId = i.VisitorId,
                Name = $"{i.Visitor.FirstName} {i.Visitor.LastName}".Trim(),
                Phone = i.Visitor.PhoneNumber?.Value,
                Company = i.Visitor.Company,
                IsCivilian = i.IsCivilian,
                AffiliatedOrganization = i.AffiliatedOrganization,
                HostId = i.HostId,
                HostName = i.Host.FullName,
                HostDepartment = i.Host.Department,
                LocationId = i.LocationId,
                LocationName = i.Location?.Name,
                Purpose = i.VisitPurpose?.Name ?? i.Subject,
                CheckedInAt = i.CheckedInAt,
                Status = i.Status.ToString(),
                ProfilePhotoUrl = ResolvePhoto(i.Visitor.ProfilePhotoPath),
                IsTemporarilyAbsent = activeTempLeaves.ContainsKey(i.Id),
                ActiveTemporaryLeaveId = activeTempLeaves.TryGetValue(i.Id, out var tlId) ? tlId : null
            }).ToList();
        }

        return new CivilDefensePresenceDto
        {
            Staff = staff,
            Visitors = visitors,
            Summary = new CivilDefensePresenceSummaryDto
            {
                StaffCount = staff.Count,
                VisitorCount = visitors.Count,
                TotalInside = staff.Count + visitors.Count
            },
            GeneratedAt = DateTime.UtcNow
        };
    }
}
