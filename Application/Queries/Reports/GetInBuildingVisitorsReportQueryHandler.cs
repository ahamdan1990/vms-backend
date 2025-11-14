using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Reports;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using System.Linq;

namespace VisitorManagementSystem.Api.Application.Queries.Reports;

/// <summary>
/// Handler that returns a snapshot of all visitors currently inside the facility.
/// </summary>
public class GetInBuildingVisitorsReportQueryHandler : IRequestHandler<GetInBuildingVisitorsReportQuery, WhoIsInBuildingReportDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetInBuildingVisitorsReportQueryHandler> _logger;

    public GetInBuildingVisitorsReportQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetInBuildingVisitorsReportQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WhoIsInBuildingReportDto> Handle(GetInBuildingVisitorsReportQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        try
        {
            var query = _unitOfWork.Invitations
                .GetQueryable()
                .Include(i => i.Visitor)
                .Include(i => i.Host)
                .Include(i => i.Location)
                .Where(i =>
                    !i.IsDeleted &&
                    i.Status == InvitationStatus.Active &&
                    i.CheckedInAt != null &&
                    i.CheckedOutAt == null);

            if (request.LocationId.HasValue)
            {
                query = query.Where(i => i.LocationId == request.LocationId.Value);
            }

            var invitations = await query
                .AsNoTracking()
                .OrderBy(i => i.CheckedInAt)
                .ToListAsync(cancellationToken);

            var occupants = invitations.Select(invitation =>
            {
                var checkedInAt = invitation.CheckedInAt;
                var scheduledEnd = invitation.ScheduledEndTime;
                var minutesOnSite = checkedInAt.HasValue
                    ? (int)Math.Max(0, (now - checkedInAt.Value).TotalMinutes)
                    : 0;

                var isOverdue = scheduledEnd < now;

                return new InBuildingVisitorDto
                {
                    InvitationId = invitation.Id,
                    VisitorId = invitation.VisitorId,
                    VisitorName = $"{invitation.Visitor.FirstName} {invitation.Visitor.LastName}".Trim(),
                    Company = invitation.Visitor.Company ?? invitation.Visitor.CompanyEntity?.Name,
                    VisitorPhone = invitation.Visitor.PhoneNumber?.Value,
                    VisitorEmail = invitation.Visitor.Email.Value,
                    HostId = invitation.HostId,
                    HostName = invitation.Host.FullName,
                    HostDepartment = invitation.Host.Department,
                    HostEmail = invitation.Host.Email?.Value,
                    HostPhone = invitation.Host.PhoneNumber?.Value,
                    LocationId = invitation.LocationId,
                    LocationName = invitation.Location?.Name,
                    CheckedInAt = checkedInAt,
                    ScheduledEndTime = scheduledEnd,
                    MinutesOnSite = minutesOnSite,
                    IsOverdue = isOverdue,
                    Status = invitation.Status.ToString()
                };
            }).ToList();

            string? locationName = null;
            if (request.LocationId.HasValue)
            {
                locationName = invitations
                    .Select(i => i.Location?.Name)
                    .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

                if (string.IsNullOrWhiteSpace(locationName))
                {
                    var location = await _unitOfWork.Locations.GetByIdAsync(request.LocationId.Value, cancellationToken);
                    locationName = location?.Name;
                }
            }

            var report = new WhoIsInBuildingReportDto
            {
                GeneratedAt = now,
                LastUpdated = now,
                LocationId = request.LocationId,
                LocationName = locationName,
                TotalVisitors = occupants.Count,
                OverdueVisitors = occupants.Count(o => o.IsOverdue),
                Occupants = occupants
            };

            _logger.LogInformation("Generated in-building report with {Count} occupants (LocationId: {LocationId})",
                report.TotalVisitors, request.LocationId);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate in-building report for location {LocationId}", request.LocationId);
            throw;
        }
    }
}
