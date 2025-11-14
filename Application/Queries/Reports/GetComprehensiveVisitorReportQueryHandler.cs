using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Reports;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using System.Linq.Expressions;
using System.Linq;

namespace VisitorManagementSystem.Api.Application.Queries.Reports;

/// <summary>
/// Handler for comprehensive visitor report with advanced filtering and pagination.
/// </summary>
public class GetComprehensiveVisitorReportQueryHandler : IRequestHandler<GetComprehensiveVisitorReportQuery, ComprehensiveVisitorReportDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetComprehensiveVisitorReportQueryHandler> _logger;

    public GetComprehensiveVisitorReportQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetComprehensiveVisitorReportQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ComprehensiveVisitorReportDto> Handle(GetComprehensiveVisitorReportQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        try
        {
            // Validate and cap page size
            var pageSize = Math.Min(Math.Max(request.PageSize, 1), 500);
            var pageIndex = Math.Max(request.PageIndex, 0);

            // Build base query
            var query = _unitOfWork.Invitations
                .GetQueryable()
                .Include(i => i.Visitor)
                    .ThenInclude(v => v.CompanyEntity)
                .Include(i => i.Host)
                .Include(i => i.Location)
                .Include(i => i.VisitPurpose)
                .Where(i => !i.IsDeleted);

            // Apply filters
            query = ApplyFilters(query, request, now);

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting
            query = ApplySorting(query, request.SortBy, request.SortDirection);

            // Apply pagination
            var invitations = await query
                .AsNoTracking()
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            // Map to DTOs
            var visitors = invitations.Select(invitation => MapToDto(invitation, now)).ToList();

            // Calculate summary statistics
            var summary = new ReportSummaryDto
            {
                TotalRecords = totalCount,
                TotalCheckedIn = await CountByConditionAsync(query, i => i.CheckedInAt != null && i.CheckedOutAt == null, cancellationToken),
                TotalCheckedOut = await CountByConditionAsync(query, i => i.CheckedOutAt != null, cancellationToken),
                TotalOverdue = await CountByConditionAsync(query, i => i.CheckedInAt != null && i.CheckedOutAt == null && i.ScheduledEndTime < now, cancellationToken),
                TotalPending = await CountByConditionAsync(query, i => i.Status == InvitationStatus.Submitted, cancellationToken),
                TotalActive = await CountByConditionAsync(query, i => i.Status == InvitationStatus.Active, cancellationToken)
            };

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            _logger.LogInformation(
                "Generated comprehensive visitor report: {TotalRecords} records, Page {Page}/{TotalPages}",
                totalCount, pageIndex + 1, totalPages);

            return new ComprehensiveVisitorReportDto
            {
                GeneratedAt = now,
                Summary = summary,
                Visitors = visitors,
                Filters = MapFilters(request),
                Pagination = new PaginationDto
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    TotalRecords = totalCount,
                    TotalPages = totalPages,
                    HasPreviousPage = pageIndex > 0,
                    HasNextPage = pageIndex < totalPages - 1
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate comprehensive visitor report");
            throw;
        }
    }

    private IQueryable<Domain.Entities.Invitation> ApplyFilters(
        IQueryable<Domain.Entities.Invitation> query,
        GetComprehensiveVisitorReportQuery request,
        DateTime now)
    {
        // Location filter
        if (request.LocationId.HasValue)
        {
            query = query.Where(i => i.LocationId == request.LocationId.Value);
        }

        // Date range filter
        if (request.StartDate.HasValue)
        {
            query = query.Where(i => i.ScheduledStartTime >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            var endOfDay = request.EndDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(i => i.ScheduledStartTime <= endOfDay);
        }

        // Status filter
        if (request.Status.HasValue)
        {
            query = query.Where(i => i.Status == request.Status.Value);
        }

        // Host filter
        if (request.HostId.HasValue)
        {
            query = query.Where(i => i.HostId == request.HostId.Value);
        }

        // Visit purpose filter
        if (request.VisitPurposeId.HasValue)
        {
            query = query.Where(i => i.VisitPurposeId == request.VisitPurposeId.Value);
        }

        // Department filter
        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            query = query.Where(i => i.Host.Department != null && i.Host.Department.Contains(request.Department));
        }

        // Search term (visitor name, company, email)
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            query = query.Where(i =>
                (i.Visitor.FirstName != null && i.Visitor.FirstName.ToLower().Contains(searchLower)) ||
                (i.Visitor.LastName != null && i.Visitor.LastName.ToLower().Contains(searchLower)) ||
                (i.Visitor.Email != null && i.Visitor.Email.Value.ToLower().Contains(searchLower)) ||
                (i.Visitor.Company != null && i.Visitor.Company.ToLower().Contains(searchLower)) ||
                (i.Visitor.CompanyEntity != null && i.Visitor.CompanyEntity.Name.ToLower().Contains(searchLower)));
        }

        // Checked-in only
        if (request.CheckedInOnly == true)
        {
            query = query.Where(i => i.CheckedInAt != null && i.CheckedOutAt == null);
        }

        // Checked-out only
        if (request.CheckedOutOnly == true)
        {
            query = query.Where(i => i.CheckedOutAt != null);
        }

        // Overdue only
        if (request.OverdueOnly == true)
        {
            query = query.Where(i => i.CheckedInAt != null && i.CheckedOutAt == null && i.ScheduledEndTime < now);
        }

        return query;
    }

    private IQueryable<Domain.Entities.Invitation> ApplySorting(
        IQueryable<Domain.Entities.Invitation> query,
        string sortBy,
        string sortDirection)
    {
        var isDescending = sortDirection?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "visitorname" => isDescending
                ? query.OrderByDescending(i => i.Visitor.LastName).ThenByDescending(i => i.Visitor.FirstName)
                : query.OrderBy(i => i.Visitor.LastName).ThenBy(i => i.Visitor.FirstName),
            "company" => isDescending
                ? query.OrderByDescending(i => i.Visitor.Company ?? i.Visitor.CompanyEntity!.Name)
                : query.OrderBy(i => i.Visitor.Company ?? i.Visitor.CompanyEntity!.Name),
            "hostname" => isDescending
                ? query.OrderByDescending(i => i.Host.LastName).ThenByDescending(i => i.Host.FirstName)
                : query.OrderBy(i => i.Host.LastName).ThenBy(i => i.Host.FirstName),
            "location" => isDescending
                ? query.OrderByDescending(i => i.Location!.Name)
                : query.OrderBy(i => i.Location!.Name),
            "scheduledstart" => isDescending
                ? query.OrderByDescending(i => i.ScheduledStartTime)
                : query.OrderBy(i => i.ScheduledStartTime),
            "checkedinat" => isDescending
                ? query.OrderByDescending(i => i.CheckedInAt)
                : query.OrderBy(i => i.CheckedInAt),
            "checkedoutat" => isDescending
                ? query.OrderByDescending(i => i.CheckedOutAt)
                : query.OrderBy(i => i.CheckedOutAt),
            "status" => isDescending
                ? query.OrderByDescending(i => i.Status)
                : query.OrderBy(i => i.Status),
            _ => query.OrderByDescending(i => i.CheckedInAt ?? i.ScheduledStartTime)
        };
    }

    private VisitorReportItemDto MapToDto(Domain.Entities.Invitation invitation, DateTime now)
    {
        var minutesOnSite = invitation.CheckedInAt.HasValue
            ? invitation.CheckedOutAt.HasValue
                ? (int)(invitation.CheckedOutAt.Value - invitation.CheckedInAt.Value).TotalMinutes
                : (int)(now - invitation.CheckedInAt.Value).TotalMinutes
            : 0;

        var isOverdue = invitation.CheckedInAt.HasValue &&
                       !invitation.CheckedOutAt.HasValue &&
                       invitation.ScheduledEndTime < now;

        return new VisitorReportItemDto
        {
            InvitationId = invitation.Id,
            VisitorId = invitation.VisitorId,
            VisitorName = $"{invitation.Visitor.FirstName} {invitation.Visitor.LastName}".Trim(),
            Company = invitation.Visitor.Company ?? invitation.Visitor.CompanyEntity?.Name,
            VisitorEmail = invitation.Visitor.Email?.Value,
            VisitorPhone = invitation.Visitor.PhoneNumber?.Value,
            HostId = invitation.HostId,
            HostName = invitation.Host.FullName,
            HostDepartment = invitation.Host.Department,
            HostEmail = invitation.Host.Email?.Value,
            HostPhone = invitation.Host.PhoneNumber?.Value,
            LocationId = invitation.LocationId,
            LocationName = invitation.Location?.Name,
            VisitPurpose = invitation.VisitPurpose?.Name,
            ScheduledStartTime = invitation.ScheduledStartTime,
            ScheduledEndTime = invitation.ScheduledEndTime,
            CheckedInAt = invitation.CheckedInAt,
            CheckedOutAt = invitation.CheckedOutAt,
            MinutesOnSite = minutesOnSite,
            Status = invitation.Status.ToString(),
            IsOverdue = isOverdue,
            CreatedAt = invitation.CreatedOn,
            Notes = invitation.Message
        };
    }

    private ReportFiltersDto MapFilters(GetComprehensiveVisitorReportQuery request)
    {
        return new ReportFiltersDto
        {
            LocationId = request.LocationId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status?.ToString(),
            SearchTerm = request.SearchTerm,
            HostId = request.HostId,
            VisitPurposeId = request.VisitPurposeId,
            Department = request.Department,
            CheckedInOnly = request.CheckedInOnly,
            CheckedOutOnly = request.CheckedOutOnly,
            OverdueOnly = request.OverdueOnly
        };
    }

    private async Task<int> CountByConditionAsync(
        IQueryable<Domain.Entities.Invitation> query,
        Expression<Func<Domain.Entities.Invitation, bool>> condition,
        CancellationToken cancellationToken)
    {
        return await query.Where(condition).CountAsync(cancellationToken);
    }
}
