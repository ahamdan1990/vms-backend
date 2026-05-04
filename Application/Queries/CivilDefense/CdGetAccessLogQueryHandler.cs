using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Application.Queries.CivilDefense;

public class CdGetAccessLogQueryHandler : IRequestHandler<CdGetAccessLogQuery, List<CdAccessLogEntryDto>>
{
    private readonly ApplicationDbContext _context;

    public CdGetAccessLogQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CdAccessLogEntryDto>> Handle(
        CdGetAccessLogQuery request, CancellationToken cancellationToken)
    {
        var from = request.From ?? DateTime.UtcNow.Date;
        var to   = request.To   ?? DateTime.UtcNow;
        var skip = (request.Page - 1) * request.PageSize;

        var entries = new List<CdAccessLogEntryDto>();

        if (request.VisitorsOnly != true)
        {
            IQueryable<StaffAttendance> staffQuery = _context.StaffAttendances
                .AsNoTracking()
                .Where(a => a.CheckInTime >= from && a.CheckInTime <= to && a.IsActive)
                .Include(a => a.User)
                .Include(a => a.Camera);

            if (!string.IsNullOrWhiteSpace(request.SearchName))
                staffQuery = staffQuery.Where(a => a.User != null &&
                    (a.User.FirstName + " " + a.User.LastName).Contains(request.SearchName));

            var staffRows = await staffQuery
                .OrderByDescending(a => a.CheckInTime)
                .Select(a => new CdAccessLogEntryDto
                {
                    Id           = a.Id,
                    PersonName   = a.User != null ? a.User.FirstName + " " + a.User.LastName : $"User {a.UserId}",
                    IsStaff      = true,
                    CheckInTime  = a.CheckInTime,
                    CheckOutTime = a.CheckOutTime,
                    Duration     = a.CheckOutTime.HasValue
                        ? (TimeSpan?)(a.CheckOutTime.Value - a.CheckInTime)
                        : null,
                    EntryMethod  = a.Method.ToString(),
                    CameraName   = a.Camera != null ? a.Camera.Name : null,
                    Notes        = a.Notes,
                })
                .ToListAsync(cancellationToken);

            entries.AddRange(staffRows);
        }

        if (request.StaffOnly != true)
        {
            IQueryable<Invitation> invQuery = _context.Invitations
                .AsNoTracking()
                .Where(i => !i.IsDeleted
                    && i.CheckedInAt.HasValue
                    && i.CheckedInAt >= from
                    && i.CheckedInAt <= to)
                .Include(i => i.Visitor);

            if (!string.IsNullOrWhiteSpace(request.SearchName))
                invQuery = invQuery.Where(i => i.Visitor != null &&
                    (i.Visitor.FirstName + " " + i.Visitor.LastName).Contains(request.SearchName));

            var visitorRows = await invQuery
                .OrderByDescending(i => i.CheckedInAt)
                .Select(i => new CdAccessLogEntryDto
                {
                    Id           = i.Id,
                    PersonName   = i.Visitor != null ? i.Visitor.FirstName + " " + i.Visitor.LastName : $"Visitor {i.VisitorId}",
                    PhoneNumber  = i.Visitor != null && i.Visitor.PhoneNumber != null ? i.Visitor.PhoneNumber.Value : null,
                    NationalId   = i.Visitor != null ? i.Visitor.GovernmentId : null,
                    IsStaff      = false,
                    CheckInTime  = i.CheckedInAt!.Value,
                    CheckOutTime = i.CheckedOutAt,
                    Duration     = i.CheckedOutAt.HasValue
                        ? (TimeSpan?)(i.CheckedOutAt.Value - i.CheckedInAt!.Value)
                        : null,
                    EntryMethod  = "Invitation",
                })
                .ToListAsync(cancellationToken);

            entries.AddRange(visitorRows);
        }

        return entries
            .OrderByDescending(e => e.CheckInTime)
            .Skip(skip)
            .Take(request.PageSize)
            .ToList();
    }
}
