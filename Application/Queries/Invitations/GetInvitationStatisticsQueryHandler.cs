using MediatR;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using DomainPermissions = VisitorManagementSystem.Api.Domain.Constants.Permissions;

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
            _logger.LogDebug("Processing get invitation statistics query for user {UserId} with permissions: {Permissions}",
                request.UserId, string.Join(", ", request.UserPermissions));

            // Check if user has ReadAll permission for INVITATIONS (not visitors)
            // Staff users with Invitation.Read.Own should see only their own invitations (where they are the host)
            bool hasInvitationReadAll = request.UserPermissions.Contains(DomainPermissions.Invitation.ReadAll);
            bool hasInvitationReadOwn = request.UserPermissions.Contains(DomainPermissions.Invitation.ReadOwn);

            _logger.LogDebug("User {UserId} hasInvitationReadAll={HasReadAll}, hasInvitationReadOwn={HasReadOwn}",
                request.UserId, hasInvitationReadAll, hasInvitationReadOwn);

            InvitationStatistics statistics;

            // Check if we need custom date filtering
            bool needsCustomFiltering = request.StartDate.HasValue || request.EndDate.HasValue;

            // Determine HostId filter:
            // - If HostId is explicitly provided in request, use it
            // - Otherwise, if user has ReadOwn (but not ReadAll), filter by current user's ID as host
            int? effectiveHostId = request.HostId;
            if (!effectiveHostId.HasValue && hasInvitationReadOwn && !hasInvitationReadAll && request.UserId.HasValue)
            {
                effectiveHostId = request.UserId.Value;
                _logger.LogDebug("Staff user {UserId} with ReadOwn - filtering by HostId={HostId}",
                    request.UserId.Value, effectiveHostId);
            }

            // Always use filtered statistics method if we have any filters (date or host)
            if (needsCustomFiltering || effectiveHostId.HasValue)
            {
                _logger.LogDebug("Getting filtered invitation statistics - HostId={HostId}", effectiveHostId);
                statistics = await GetFilteredStatisticsAsync(request, effectiveHostId, cancellationToken);
            }
            else
            {
                // Admin users or users with ReadAll - get all statistics
                _logger.LogDebug("Getting all invitation statistics (admin or ReadAll permission)");
                statistics = await _unitOfWork.Invitations.GetStatisticsAsync(cancellationToken);
            }

            _logger.LogDebug("Retrieved invitation statistics for user {UserId}: Total={TotalInvitations}, Pending={PendingApprovals}",
                request.UserId, statistics.TotalInvitations, statistics.PendingApprovals);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invitation statistics");
            throw;
        }
    }

    private async Task<InvitationStatistics> GetFilteredStatisticsAsync(GetInvitationStatisticsQuery request, int? hostId, CancellationToken cancellationToken)
    {
        // Use more reasonable defaults for date range
        var startDate = request.StartDate ?? DateTime.UtcNow.AddYears(-10);
        var endDate = request.EndDate ?? DateTime.UtcNow.AddYears(1);

        _logger.LogDebug("GetFilteredStatisticsAsync: startDate={StartDate}, endDate={EndDate}, hostId={HostId}",
            startDate, endDate, hostId);

        // Get invitations within date range
        var invitations = await _unitOfWork.Invitations.GetByDateRangeAsync(startDate, endDate, cancellationToken);
        _logger.LogDebug("Retrieved {Count} invitations from date range", invitations.Count);

        // Apply host filter if specified (for staff users with ReadOwn permission)
        if (hostId.HasValue)
        {
            var beforeHostFilterCount = invitations.Count;
            invitations = invitations.Where(i => i.HostId == hostId.Value).ToList();
            _logger.LogDebug("After HostId filter: {Before} -> {After} invitations (HostId={HostId})",
                beforeHostFilterCount, invitations.Count, hostId.Value);
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
