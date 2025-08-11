using MediatR;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Invitations;

/// <summary>
/// Handler for getting invitation statistics
/// </summary>
public class GetInvitationStatisticsQueryHandler : IRequestHandler<GetInvitationStatisticsQuery, InvitationStatistics>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetInvitationStatisticsQueryHandler> _logger;

    public GetInvitationStatisticsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetInvitationStatisticsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<InvitationStatistics> Handle(GetInvitationStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing get invitation statistics query");

            // Get basic statistics from repository
            var statistics = await _unitOfWork.Invitations.GetStatisticsAsync(cancellationToken);

            // Apply additional filters if specified
            if (request.StartDate.HasValue || request.EndDate.HasValue || request.HostId.HasValue)
            {
                statistics = await GetFilteredStatisticsAsync(request, cancellationToken);
            }

            _logger.LogDebug("Retrieved invitation statistics: Total={TotalInvitations}, Pending={PendingApprovals}",
                statistics.TotalInvitations, statistics.PendingApprovals);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invitation statistics");
            throw;
        }
    }

    private async Task<InvitationStatistics> GetFilteredStatisticsAsync(GetInvitationStatisticsQuery request, CancellationToken cancellationToken)
    {
        var startDate = request.StartDate ?? DateTime.MinValue;
        var endDate = request.EndDate ?? DateTime.MaxValue;

        // Get invitations within date range
        var invitations = await _unitOfWork.Invitations.GetByDateRangeAsync(startDate, endDate, cancellationToken);

        // Apply host filter if specified
        if (request.HostId.HasValue)
        {
            invitations = invitations.Where(i => i.HostId == request.HostId.Value).ToList();
        }

        // Calculate filtered statistics
        var filteredStats = new InvitationStatistics
        {
            TotalInvitations = invitations.Count,
            PendingApprovals = invitations.Count(i => i.Status == Domain.Enums.InvitationStatus.Submitted || 
                                                    i.Status == Domain.Enums.InvitationStatus.UnderReview),
            ApprovedInvitations = invitations.Count(i => i.Status == Domain.Enums.InvitationStatus.Approved),
            ActiveVisitors = invitations.Count(i => i.Status == Domain.Enums.InvitationStatus.Active),
            CompletedVisits = invitations.Count(i => i.Status == Domain.Enums.InvitationStatus.Completed),
            CancelledInvitations = invitations.Count(i => i.Status == Domain.Enums.InvitationStatus.Cancelled),
            ExpiredInvitations = invitations.Count(i => i.Status == Domain.Enums.InvitationStatus.Expired),
            StatusBreakdown = invitations.GroupBy(i => i.Status).ToDictionary(g => g.Key, g => g.Count())
        };

        // Calculate average visit duration for completed visits
        var completedWithTimes = invitations
            .Where(i => i.Status == Domain.Enums.InvitationStatus.Completed && 
                       i.CheckedInAt.HasValue && 
                       i.CheckedOutAt.HasValue)
            .ToList();

        if (completedWithTimes.Any())
        {
            filteredStats.AverageVisitDuration = completedWithTimes
                .Average(i => (i.CheckedOutAt!.Value - i.CheckedInAt!.Value).TotalHours);
        }

        return filteredStats;
    }
}
