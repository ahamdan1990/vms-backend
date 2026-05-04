using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Application.Queries.CivilDefense;

public class CdGetLiveBuildingOccupancyQueryHandler
    : IRequestHandler<CdGetLiveBuildingOccupancyQuery, CdLiveOccupancyDto>
{
    private readonly ApplicationDbContext _context;

    public CdGetLiveBuildingOccupancyQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CdLiveOccupancyDto> Handle(
        CdGetLiveBuildingOccupancyQuery request, CancellationToken cancellationToken)
    {
        var staffOccupants = await _context.StaffAttendances
            .AsNoTracking()
            .Where(a => a.CheckOutTime == null && a.IsActive)
            .Include(a => a.User)
            .Include(a => a.Camera)
            .Select(a => new CdOccupantDto
            {
                Id            = a.UserId,
                FullName      = a.User != null ? a.User.FullName : $"User {a.UserId}",
                IsStaff       = true,
                CheckInTime   = a.CheckInTime,
                CheckOutTime  = a.CheckOutTime,
                EntryMethod   = a.Method.ToString(),
                CameraId      = a.CameraId,
                CameraName    = a.Camera != null ? a.Camera.Name : null,
                AttendanceId  = a.Id,
            })
            .ToListAsync(cancellationToken);

        var visitorOccupants = await _context.Invitations
            .AsNoTracking()
            .Where(i => i.Status == InvitationStatus.Active && !i.IsDeleted)
            .Include(i => i.Visitor)
            .Select(i => new CdOccupantDto
            {
                Id           = i.VisitorId,
                FullName     = i.Visitor != null ? i.Visitor.FirstName + " " + i.Visitor.LastName : $"Visitor {i.VisitorId}",
                PhoneNumber  = i.Visitor != null && i.Visitor.PhoneNumber != null ? i.Visitor.PhoneNumber.Value : string.Empty,
                NationalId   = i.Visitor != null ? i.Visitor.GovernmentId : null,
                IsStaff      = false,
                CheckInTime  = i.CheckedInAt ?? i.ScheduledStartTime,
                CheckOutTime = i.CheckedOutAt,
                EntryMethod  = "Manual",
                InvitationId = i.Id,
            })
            .ToListAsync(cancellationToken);

        var all = staffOccupants.Concat(visitorOccupants)
            .OrderBy(o => o.CheckInTime)
            .ToList();

        return new CdLiveOccupancyDto
        {
            TotalInside    = all.Count,
            StaffInside    = staffOccupants.Count,
            VisitorsInside = visitorOccupants.Count,
            AsOf           = DateTime.UtcNow,
            Occupants      = all,
        };
    }
}
