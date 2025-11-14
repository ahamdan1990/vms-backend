using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Reports;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Reports;

/// <summary>
/// Handler for visitor statistics and analytics report
/// </summary>
public class GetVisitorStatisticsQueryHandler : IRequestHandler<GetVisitorStatisticsQuery, VisitorStatisticsReportDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetVisitorStatisticsQueryHandler> _logger;

    public GetVisitorStatisticsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetVisitorStatisticsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<VisitorStatisticsReportDto> Handle(GetVisitorStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTime.UtcNow;
            var startDate = request.StartDate ?? now.AddMonths(-1);
            var endDate = request.EndDate ?? now;

            var query = _unitOfWork.Invitations
                .GetQueryable()
                .Include(i => i.Visitor)
                .Include(i => i.Host)
                .Include(i => i.Location)
                .Include(i => i.VisitPurpose)
                .Where(i => !i.IsDeleted && i.ScheduledStartTime >= startDate && i.ScheduledStartTime <= endDate);

            if (request.LocationId.HasValue)
            {
                query = query.Where(i => i.LocationId == request.LocationId.Value);
            }

            var invitations = await query.AsNoTracking().ToListAsync(cancellationToken);

            // Overall statistics
            var totalVisitors = invitations.Count;
            var totalCheckedIn = invitations.Count(i => i.CheckedInAt.HasValue);
            var totalCheckedOut = invitations.Count(i => i.CheckedOutAt.HasValue);
            var totalNoShow = invitations.Count(i => i.Status == InvitationStatus.Expired && !i.CheckedInAt.HasValue);
            var totalCancelled = invitations.Count(i => i.Status == InvitationStatus.Cancelled);

            var averageDuration = invitations
                .Where(i => i.CheckedInAt.HasValue && i.CheckedOutAt.HasValue)
                .Select(i => (i.CheckedOutAt!.Value - i.CheckedInAt!.Value).TotalMinutes)
                .DefaultIfEmpty(0)
                .Average();

            // Group by location
            var byLocation = invitations
                .GroupBy(i => new { i.LocationId, i.Location!.Name })
                .Select(g => new LocationStatisticsDto
                {
                    LocationId = g.Key.LocationId,
                    LocationName = g.Key.Name,
                    VisitorCount = g.Count(),
                    CheckedInCount = g.Count(i => i.CheckedInAt.HasValue),
                    Percentage = totalVisitors > 0 ? (double)g.Count() / totalVisitors * 100 : 0
                })
                .OrderByDescending(l => l.VisitorCount)
                .ToList();

            // Group by department
            var byDepartment = invitations
                .Where(i => !string.IsNullOrWhiteSpace(i.Host.Department))
                .GroupBy(i => i.Host.Department!)
                .Select(g => new DepartmentStatisticsDto
                {
                    Department = g.Key,
                    VisitorCount = g.Count(),
                    CheckedInCount = g.Count(i => i.CheckedInAt.HasValue),
                    Percentage = totalVisitors > 0 ? (double)g.Count() / totalVisitors * 100 : 0
                })
                .OrderByDescending(d => d.VisitorCount)
                .Take(10)
                .ToList();

            // Group by visit purpose
            var byPurpose = invitations
                .Where(i => i.VisitPurpose != null)
                .GroupBy(i => new { i.VisitPurposeId, i.VisitPurpose!.Name })
                .Select(g => new VisitPurposeStatisticsDto
                {
                    VisitPurposeId = g.Key.VisitPurposeId,
                    VisitPurposeName = g.Key.Name,
                    VisitorCount = g.Count(),
                    CheckedInCount = g.Count(i => i.CheckedInAt.HasValue),
                    Percentage = totalVisitors > 0 ? (double)g.Count() / totalVisitors * 100 : 0
                })
                .OrderByDescending(p => p.VisitorCount)
                .ToList();

            // Top hosts
            var topHosts = invitations
                .GroupBy(i => new { i.HostId, i.Host.FullName })
                .Select(g => new HostStatisticsDto
                {
                    HostId = g.Key.HostId,
                    HostName = g.Key.FullName,
                    VisitorCount = g.Count(),
                    CheckedInCount = g.Count(i => i.CheckedInAt.HasValue)
                })
                .OrderByDescending(h => h.VisitorCount)
                .Take(10)
                .ToList();

            // Time series data
            var timeSeries = GenerateTimeSeries(invitations, startDate, endDate, request.GroupBy);

            _logger.LogInformation(
                "Generated visitor statistics report: {TotalVisitors} visitors from {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}",
                totalVisitors, startDate, endDate);

            return new VisitorStatisticsReportDto
            {
                GeneratedAt = now,
                StartDate = startDate,
                EndDate = endDate,
                TotalVisitors = totalVisitors,
                TotalCheckedIn = totalCheckedIn,
                TotalCheckedOut = totalCheckedOut,
                TotalNoShow = totalNoShow,
                TotalCancelled = totalCancelled,
                AverageDurationMinutes = (int)averageDuration,
                CheckInRate = totalVisitors > 0 ? (double)totalCheckedIn / totalVisitors * 100 : 0,
                ByLocation = byLocation,
                ByDepartment = byDepartment,
                ByPurpose = byPurpose,
                TopHosts = topHosts,
                TimeSeries = timeSeries
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate visitor statistics report");
            throw;
        }
    }

    private List<TimeSeriesDataPointDto> GenerateTimeSeries(
        List<Domain.Entities.Invitation> invitations,
        DateTime startDate,
        DateTime endDate,
        string groupBy)
    {
        return groupBy?.ToLower() switch
        {
            "weekly" => GenerateWeeklyTimeSeries(invitations, startDate, endDate),
            "monthly" => GenerateMonthlyTimeSeries(invitations, startDate, endDate),
            _ => GenerateDailyTimeSeries(invitations, startDate, endDate)
        };
    }

    private List<TimeSeriesDataPointDto> GenerateDailyTimeSeries(
        List<Domain.Entities.Invitation> invitations,
        DateTime startDate,
        DateTime endDate)
    {
        var grouped = invitations
            .GroupBy(i => i.ScheduledStartTime.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<TimeSeriesDataPointDto>();
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var dayInvitations = grouped.GetValueOrDefault(date, new List<Domain.Entities.Invitation>());
            result.Add(new TimeSeriesDataPointDto
            {
                Date = date,
                Label = date.ToString("MMM dd"),
                TotalVisitors = dayInvitations.Count,
                CheckedIn = dayInvitations.Count(i => i.CheckedInAt.HasValue),
                CheckedOut = dayInvitations.Count(i => i.CheckedOutAt.HasValue)
            });
        }

        return result;
    }

    private List<TimeSeriesDataPointDto> GenerateWeeklyTimeSeries(
        List<Domain.Entities.Invitation> invitations,
        DateTime startDate,
        DateTime endDate)
    {
        var grouped = invitations
            .GroupBy(i => GetWeekStartDate(i.ScheduledStartTime))
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<TimeSeriesDataPointDto>();
        for (var date = GetWeekStartDate(startDate); date <= endDate; date = date.AddDays(7))
        {
            var weekInvitations = grouped.GetValueOrDefault(date, new List<Domain.Entities.Invitation>());
            result.Add(new TimeSeriesDataPointDto
            {
                Date = date,
                Label = $"{date:MMM dd}",
                TotalVisitors = weekInvitations.Count,
                CheckedIn = weekInvitations.Count(i => i.CheckedInAt.HasValue),
                CheckedOut = weekInvitations.Count(i => i.CheckedOutAt.HasValue)
            });
        }

        return result;
    }

    private List<TimeSeriesDataPointDto> GenerateMonthlyTimeSeries(
        List<Domain.Entities.Invitation> invitations,
        DateTime startDate,
        DateTime endDate)
    {
        var grouped = invitations
            .GroupBy(i => new DateTime(i.ScheduledStartTime.Year, i.ScheduledStartTime.Month, 1))
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<TimeSeriesDataPointDto>();
        for (var date = new DateTime(startDate.Year, startDate.Month, 1);
             date <= endDate;
             date = date.AddMonths(1))
        {
            var monthInvitations = grouped.GetValueOrDefault(date, new List<Domain.Entities.Invitation>());
            result.Add(new TimeSeriesDataPointDto
            {
                Date = date,
                Label = date.ToString("MMM yyyy"),
                TotalVisitors = monthInvitations.Count,
                CheckedIn = monthInvitations.Count(i => i.CheckedInAt.HasValue),
                CheckedOut = monthInvitations.Count(i => i.CheckedOutAt.HasValue)
            });
        }

        return result;
    }

    private DateTime GetWeekStartDate(DateTime date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var daysToSubtract = dayOfWeek == 0 ? 6 : dayOfWeek - 1; // Start week on Monday
        return date.Date.AddDays(-daysToSubtract);
    }
}
