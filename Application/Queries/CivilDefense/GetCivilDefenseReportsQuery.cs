using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.CivilDefense;

public class GetCivilDefenseReportsQuery : IRequest<CivilDefenseReportDto>
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Type { get; set; } // "Staff" | "Visitor" | null = all
    public string? Status { get; set; } // "Active" | "CheckedOut" | "TemporarilyAbsent" | null = all
    public bool? IsCivilian { get; set; }
    public int? LocationId { get; set; }
    public string? SearchTerm { get; set; }
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 50;
}

public class GetCivilDefenseReportsQueryHandler
    : IRequestHandler<GetCivilDefenseReportsQuery, CivilDefenseReportDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCivilDefenseReportsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CivilDefenseReportDto> Handle(
        GetCivilDefenseReportsQuery request,
        CancellationToken cancellationToken)
    {
        var dateFrom = request.DateFrom?.Date ?? DateTime.UtcNow.Date;
        var dateTo = request.DateTo.HasValue
            ? request.DateTo.Value.Date.AddDays(1).AddTicks(-1)
            : DateTime.UtcNow;

        var entries = new List<CivilDefenseReportEntryDto>();

        if (request.Type == null || request.Type == "Staff")
        {
            var staffQuery = _unitOfWork.Repository<Domain.Entities.StaffPresence>()
                .GetQueryable()
                .Include(sp => sp.User)
                .Include(sp => sp.Location)
                .Where(sp => sp.CheckedInAt >= dateFrom && sp.CheckedInAt <= dateTo);

            if (request.LocationId.HasValue)
                staffQuery = staffQuery.Where(sp => sp.LocationId == request.LocationId.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                staffQuery = staffQuery.Where(sp =>
                    sp.UserDisplayName.ToLower().Contains(term) ||
                    (sp.User != null && (sp.User.FirstName + " " + sp.User.LastName).ToLower().Contains(term)));
            }

            var staffList = await staffQuery.AsNoTracking().ToListAsync(cancellationToken);

            entries.AddRange(staffList.Select(sp =>
            {
                var end = sp.CheckedOutAt ?? DateTime.UtcNow;
                return new CivilDefenseReportEntryDto
                {
                    EntryType = "Staff",
                    PersonId = sp.UserId,
                    Name = sp.User?.FullName ?? sp.UserDisplayName,
                    CompanyOrDepartment = sp.User?.Department ?? sp.UserDepartment,
                    LocationName = sp.Location?.Name,
                    CheckedInAt = sp.CheckedInAt,
                    CheckedOutAt = sp.CheckedOutAt,
                    Status = sp.Status.ToString(),
                    DurationMinutes = (int)(end - sp.CheckedInAt).TotalMinutes
                };
            }));
        }

        if (request.Type == null || request.Type == "Visitor")
        {
            var visitorQuery = _unitOfWork.Invitations
                .GetQueryable()
                .Include(i => i.Visitor)
                .Include(i => i.Host)
                .Include(i => i.Location)
                .Include(i => i.VisitPurpose)
                .Where(i =>
                    !i.IsDeleted &&
                    i.CheckedInAt != null &&
                    i.CheckedInAt >= dateFrom &&
                    i.CheckedInAt <= dateTo &&
                    (i.Status == InvitationStatus.Active || i.Status == InvitationStatus.Completed));

            if (request.LocationId.HasValue)
                visitorQuery = visitorQuery.Where(i => i.LocationId == request.LocationId.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                visitorQuery = visitorQuery.Where(i =>
                    (i.Visitor.FirstName + " " + i.Visitor.LastName).ToLower().Contains(term) ||
                    i.Visitor.Company!.ToLower().Contains(term));
            }

            var invitationList = await visitorQuery.AsNoTracking().ToListAsync(cancellationToken);

            entries.AddRange(invitationList.Select(i =>
            {
                var end = i.CheckedOutAt ?? DateTime.UtcNow;
                var start = i.CheckedInAt ?? i.ScheduledStartTime;
                return new CivilDefenseReportEntryDto
                {
                    EntryType = "Visitor",
                    PersonId = i.VisitorId,
                    Name = $"{i.Visitor.FirstName} {i.Visitor.LastName}".Trim(),
                    Phone = i.Visitor.PhoneNumber?.Value,
                    CompanyOrDepartment = i.Visitor.Company,
                    IsCivilian = i.IsCivilian,
                    AffiliatedOrganization = i.AffiliatedOrganization,
                    HostName = i.Host.FullName,
                    Purpose = i.VisitPurpose?.Name ?? i.Subject,
                    LocationName = i.Location?.Name,
                    CheckedInAt = start,
                    CheckedOutAt = i.CheckedOutAt,
                    Status = i.Status.ToString(),
                    DurationMinutes = (int)(end - start).TotalMinutes
                };
            }));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
            entries = entries.Where(e => string.Equals(e.Status, request.Status, StringComparison.OrdinalIgnoreCase)).ToList();

        if (request.IsCivilian.HasValue)
            entries = entries.Where(e => e.IsCivilian == request.IsCivilian.Value).ToList();

        entries = entries.OrderByDescending(e => e.CheckedInAt).ToList();
        var total = entries.Count;
        var paged = entries.Skip(request.PageIndex * request.PageSize).Take(request.PageSize).ToList();

        return new CivilDefenseReportDto
        {
            Items = paged,
            TotalCount = total,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }
}
