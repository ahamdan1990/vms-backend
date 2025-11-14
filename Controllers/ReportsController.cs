using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Linq;
using System.Text;
using VisitorManagementSystem.Api.Application.DTOs.Reports;
using VisitorManagementSystem.Api.Application.Queries.Reports;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Controller responsible for reporting endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IMediator mediator, ILogger<ReportsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Returns a snapshot of all visitors currently inside the building.
    /// </summary>
    /// <param name="locationId">Optional location filter.</param>
    [HttpGet("in-building")]
    [Authorize(Policy = "Permissions.Any.Report.Generate.All,Emergency.ViewRoster")]
    public async Task<IActionResult> GetInBuildingReport([FromQuery] int? locationId = null)
    {
        var query = new GetInBuildingVisitorsReportQuery
        {
            LocationId = locationId
        };

        var report = await _mediator.Send(query);
        return SuccessResponse(report);
    }

    /// <summary>
    /// Exports the "who is in the building" report as a CSV file.
    /// </summary>
    /// <param name="locationId">Optional location filter.</param>
    /// <param name="format">Export format (currently only CSV).</param>
    [HttpGet("in-building/export")]
    [Authorize(Policy = "Permissions.Any.Report.Export,Emergency.Export")]
    public async Task<IActionResult> ExportInBuildingReport(
        [FromQuery] int? locationId = null,
        [FromQuery] string format = "csv")
    {
        if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequestResponse("Only CSV export is currently supported. Use format=csv.");
        }

        var query = new GetInBuildingVisitorsReportQuery
        {
            LocationId = locationId
        };

        var report = await _mediator.Send(query);
        var csvBytes = GenerateCsv(report);

        var safeLocationName = string.IsNullOrWhiteSpace(report.LocationName)
            ? "all"
            : report.LocationName!.Replace(' ', '_');

        var fileName = $"in-building-report_{safeLocationName}_{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

        _logger.LogInformation("Who-is-in-building report exported (Location: {Location}, Count: {Count})",
            report.LocationName ?? "All", report.TotalVisitors);

        return File(csvBytes, "text/csv", fileName);
    }

    private static byte[] GenerateCsv(WhoIsInBuildingReportDto report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Visitor Name,Company,Visitor Phone,Visitor Email,Host Name,Host Department,Host Email,Host Phone,Location,Checked In At,Expected Checkout,Minutes On Site,Status,Overdue");

        foreach (var occupant in report.Occupants.OrderBy(o => o.CheckedInAt ?? DateTime.MaxValue))
        {
            builder.AppendLine(string.Join(",",
                Escape(occupant.VisitorName),
                Escape(occupant.Company),
                Escape(occupant.VisitorPhone),
                Escape(occupant.VisitorEmail),
                Escape(occupant.HostName),
                Escape(occupant.HostDepartment),
                Escape(occupant.HostEmail),
                Escape(occupant.HostPhone),
                Escape(occupant.LocationName),
                Escape(FormatDate(occupant.CheckedInAt)),
                Escape(FormatDate(occupant.ScheduledEndTime)),
                occupant.MinutesOnSite.ToString(CultureInfo.InvariantCulture),
                Escape(occupant.Status),
                occupant.IsOverdue ? "Yes" : "No"));
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string FormatDate(DateTime? dateTime)
    {
        return dateTime?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n');
        var cleaned = value.Replace("\"", "\"\"");
        return needsQuotes ? $"\"{cleaned}\"" : cleaned;
    }

    /// <summary>
    /// Gets a comprehensive visitor report with advanced filtering, pagination, and sorting.
    /// </summary>
    [HttpGet("comprehensive")]
    [Authorize(Policy = "Permissions.Any.Report.Generate.All")]
    public async Task<IActionResult> GetComprehensiveReport(
        [FromQuery] int? locationId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] InvitationStatus? status = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? hostId = null,
        [FromQuery] int? visitPurposeId = null,
        [FromQuery] string? department = null,
        [FromQuery] bool? checkedInOnly = null,
        [FromQuery] bool? checkedOutOnly = null,
        [FromQuery] bool? overdueOnly = null,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 50,
        [FromQuery] string sortBy = "CheckedInAt",
        [FromQuery] string sortDirection = "desc")
    {
        var query = new GetComprehensiveVisitorReportQuery
        {
            LocationId = locationId,
            StartDate = startDate,
            EndDate = endDate,
            Status = status,
            SearchTerm = searchTerm,
            HostId = hostId,
            VisitPurposeId = visitPurposeId,
            Department = department,
            CheckedInOnly = checkedInOnly,
            CheckedOutOnly = checkedOutOnly,
            OverdueOnly = overdueOnly,
            PageIndex = pageIndex,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var report = await _mediator.Send(query);

        _logger.LogInformation(
            "Comprehensive report generated: {TotalRecords} records, Page {Page}/{TotalPages}",
            report.Summary.TotalRecords, pageIndex + 1, report.Pagination.TotalPages);

        return SuccessResponse(report);
    }

    /// <summary>
    /// Exports comprehensive visitor report as CSV.
    /// </summary>
    [HttpGet("comprehensive/export")]
    [Authorize(Policy = "Permissions.Any.Report.Export")]
    public async Task<IActionResult> ExportComprehensiveReport(
        [FromQuery] int? locationId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] InvitationStatus? status = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? hostId = null,
        [FromQuery] int? visitPurposeId = null,
        [FromQuery] string? department = null,
        [FromQuery] bool? checkedInOnly = null,
        [FromQuery] bool? checkedOutOnly = null,
        [FromQuery] bool? overdueOnly = null,
        [FromQuery] string sortBy = "CheckedInAt",
        [FromQuery] string sortDirection = "desc",
        [FromQuery] string format = "csv")
    {
        if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequestResponse("Only CSV export is currently supported. Use format=csv.");
        }

        // Get all records (no pagination for export)
        var query = new GetComprehensiveVisitorReportQuery
        {
            LocationId = locationId,
            StartDate = startDate,
            EndDate = endDate,
            Status = status,
            SearchTerm = searchTerm,
            HostId = hostId,
            VisitPurposeId = visitPurposeId,
            Department = department,
            CheckedInOnly = checkedInOnly,
            CheckedOutOnly = checkedOutOnly,
            OverdueOnly = overdueOnly,
            PageIndex = 0,
            PageSize = 10000, // Large page size for export
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var report = await _mediator.Send(query);
        var csvBytes = GenerateComprehensiveCsv(report);

        var fileName = $"visitor-report_{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

        _logger.LogInformation("Comprehensive report exported: {Count} records", report.Visitors.Count);

        return File(csvBytes, "text/csv", fileName);
    }

    /// <summary>
    /// Gets visitor statistics and analytics for a given time period.
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Policy = "Permissions.Any.Report.Generate.All")]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? locationId = null,
        [FromQuery] string groupBy = "daily")
    {
        var query = new GetVisitorStatisticsQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            LocationId = locationId,
            GroupBy = groupBy
        };

        var statistics = await _mediator.Send(query);

        _logger.LogInformation(
            "Statistics report generated: {TotalVisitors} visitors from {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}",
            statistics.TotalVisitors, statistics.StartDate, statistics.EndDate);

        return SuccessResponse(statistics);
    }

    /// <summary>
    /// Exports visitor statistics report as CSV.
    /// </summary>
    [HttpGet("statistics/export")]
    [Authorize(Policy = "Permissions.Any.Report.Export")]
    public async Task<IActionResult> ExportStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? locationId = null,
        [FromQuery] string groupBy = "daily",
        [FromQuery] string format = "csv")
    {
        if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequestResponse("Only CSV export is currently supported. Use format=csv.");
        }

        var query = new GetVisitorStatisticsQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            LocationId = locationId,
            GroupBy = groupBy
        };

        var statistics = await _mediator.Send(query);
        var csvBytes = GenerateStatisticsCsv(statistics);

        var fileName = $"visitor-statistics_{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

        _logger.LogInformation("Statistics report exported");

        return File(csvBytes, "text/csv", fileName);
    }

    private static byte[] GenerateComprehensiveCsv(ComprehensiveVisitorReportDto report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Invitation ID,Visitor Name,Company,Email,Phone,Host Name,Host Department,Host Email,Host Phone,Location,Visit Purpose,Scheduled Start,Scheduled End,Checked In,Checked Out,Minutes On Site,Status,Overdue,Notes");

        foreach (var visitor in report.Visitors)
        {
            builder.AppendLine(string.Join(",",
                visitor.InvitationId.ToString(CultureInfo.InvariantCulture),
                Escape(visitor.VisitorName),
                Escape(visitor.Company),
                Escape(visitor.VisitorEmail),
                Escape(visitor.VisitorPhone),
                Escape(visitor.HostName),
                Escape(visitor.HostDepartment),
                Escape(visitor.HostEmail),
                Escape(visitor.HostPhone),
                Escape(visitor.LocationName),
                Escape(visitor.VisitPurpose),
                Escape(FormatDate(visitor.ScheduledStartTime)),
                Escape(FormatDate(visitor.ScheduledEndTime)),
                Escape(FormatDate(visitor.CheckedInAt)),
                Escape(FormatDate(visitor.CheckedOutAt)),
                visitor.MinutesOnSite.ToString(CultureInfo.InvariantCulture),
                Escape(visitor.Status),
                visitor.IsOverdue ? "Yes" : "No",
                Escape(visitor.Notes)));
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static byte[] GenerateStatisticsCsv(VisitorStatisticsReportDto statistics)
    {
        var builder = new StringBuilder();

        // Summary section
        builder.AppendLine("VISITOR STATISTICS SUMMARY");
        builder.AppendLine($"Generated,{FormatDate(statistics.GeneratedAt)}");
        builder.AppendLine($"Period,{statistics.StartDate:yyyy-MM-dd} to {statistics.EndDate:yyyy-MM-dd}");
        builder.AppendLine($"Total Visitors,{statistics.TotalVisitors}");
        builder.AppendLine($"Total Checked In,{statistics.TotalCheckedIn}");
        builder.AppendLine($"Total Checked Out,{statistics.TotalCheckedOut}");
        builder.AppendLine($"Total No-Show,{statistics.TotalNoShow}");
        builder.AppendLine($"Total Cancelled,{statistics.TotalCancelled}");
        builder.AppendLine($"Average Duration (minutes),{statistics.AverageDurationMinutes}");
        builder.AppendLine($"Check-In Rate (%),{statistics.CheckInRate:F2}");
        builder.AppendLine();

        // By Location
        builder.AppendLine("BY LOCATION");
        builder.AppendLine("Location,Visitor Count,Checked In Count,Percentage");
        foreach (var loc in statistics.ByLocation)
        {
            builder.AppendLine($"{Escape(loc.LocationName)},{loc.VisitorCount},{loc.CheckedInCount},{loc.Percentage:F2}");
        }
        builder.AppendLine();

        // By Department
        builder.AppendLine("BY DEPARTMENT (Top 10)");
        builder.AppendLine("Department,Visitor Count,Checked In Count,Percentage");
        foreach (var dept in statistics.ByDepartment)
        {
            builder.AppendLine($"{Escape(dept.Department)},{dept.VisitorCount},{dept.CheckedInCount},{dept.Percentage:F2}");
        }
        builder.AppendLine();

        // By Purpose
        builder.AppendLine("BY VISIT PURPOSE");
        builder.AppendLine("Purpose,Visitor Count,Checked In Count,Percentage");
        foreach (var purpose in statistics.ByPurpose)
        {
            builder.AppendLine($"{Escape(purpose.VisitPurposeName)},{purpose.VisitorCount},{purpose.CheckedInCount},{purpose.Percentage:F2}");
        }
        builder.AppendLine();

        // Top Hosts
        builder.AppendLine("TOP HOSTS");
        builder.AppendLine("Host Name,Visitor Count,Checked In Count");
        foreach (var host in statistics.TopHosts)
        {
            builder.AppendLine($"{Escape(host.HostName)},{host.VisitorCount},{host.CheckedInCount}");
        }
        builder.AppendLine();

        // Time Series
        builder.AppendLine("TIME SERIES");
        builder.AppendLine("Date,Label,Total Visitors,Checked In,Checked Out");
        foreach (var point in statistics.TimeSeries)
        {
            builder.AppendLine($"{point.Date:yyyy-MM-dd},{Escape(point.Label)},{point.TotalVisitors},{point.CheckedIn},{point.CheckedOut}");
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }
}
