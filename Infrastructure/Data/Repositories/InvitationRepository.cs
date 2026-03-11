using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Repository implementation for invitation operations
    /// </summary>
    public class InvitationRepository : BaseRepository<Invitation>, IInvitationRepository
    {
        private readonly ILogger<InvitationRepository> _logger;

        public InvitationRepository(ApplicationDbContext context, ILogger<InvitationRepository> logger) : base(context)
        {
            _logger = logger;
        }

        public override async Task<Invitation?> GetByIdAsync(
       int id,
       CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(i => i.Visitor)
                .Include(i => i.Host)
                .Include(i => i.VisitPurpose)
                .Include(i => i.Location)
                .Include(i => i.ApprovedByUser)
                .Include(i => i.RejectedByUser)
                .Include(i => i.Approvals)
                .Include(i => i.Events)
                .AsSplitQuery()
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        }

        // Centralized includes
        protected IQueryable<Invitation> ApplyIncludes(IQueryable<Invitation> query)
        {
            return query
                .Include(i => i.Visitor)
                .Include(i => i.Host)
                .Include(i => i.VisitPurpose)
                .Include(i => i.Location);
        }

        public async Task<List<Invitation>> GetByVisitorIdAsync(int visitorId, CancellationToken cancellationToken = default)
        {
            return await ApplyIncludes(_dbSet)
                .Where(i => i.VisitorId == visitorId)
                .OrderByDescending(i => i.ScheduledStartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Invitation>> GetByVisitorIdAsync(int visitorId, int? hostId, CancellationToken cancellationToken = default)
        {
            var query = ApplyIncludes(_dbSet).Where(i => i.VisitorId == visitorId);
            if (hostId.HasValue)
                query = query.Where(i => i.HostId == hostId.Value);
            return await query.OrderByDescending(i => i.ScheduledStartTime).ToListAsync(cancellationToken);
        }

        public async Task<List<Invitation>> GetByHostIdAsync(int hostId, CancellationToken cancellationToken = default)
        {
            return await ApplyIncludes(_dbSet)
                .Where(i => i.HostId == hostId)
                .OrderByDescending(i => i.ScheduledStartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Invitation>> GetByStatusAsync(InvitationStatus status, CancellationToken cancellationToken = default)
        {
            return await ApplyIncludes(_dbSet)
                .Where(i => i.Status == status)
                .OrderByDescending(i => i.ScheduledStartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Invitation>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default)
        {
            return await ApplyIncludes(_dbSet)
                .Where(i => i.Status == InvitationStatus.Submitted || i.Status == InvitationStatus.UnderReview)
                .OrderBy(i => i.CreatedOn)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Invitation>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await ApplyIncludes(_dbSet)
                .Where(i => i.ScheduledStartTime >= startDate && i.ScheduledStartTime <= endDate)
                .OrderBy(i => i.ScheduledStartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Invitation>> GetActiveInvitationsAsync(CancellationToken cancellationToken = default)
        {
            return await ApplyIncludes(_dbSet)
                .Where(i => i.Status == InvitationStatus.Active)
                .OrderBy(i => i.CheckedInAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Invitation>> GetExpiredInvitationsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await ApplyIncludes(_dbSet)
                .Where(i => i.ScheduledEndTime < now &&
                            i.Status == InvitationStatus.Approved)
                .OrderBy(i => i.ScheduledEndTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> InvitationNumberExistsAsync(string invitationNumber, int? excludeId = null, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(i => i.InvitationNumber == invitationNumber && (excludeId == null || i.Id != excludeId))
                .AnyAsync(cancellationToken);
        }

        public async Task<InvitationStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            // Use projected GROUP BY queries to avoid loading all rows into memory
            var statusCounts = await _dbSet
                .GroupBy(i => i.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var countByStatus = statusCounts.ToDictionary(x => x.Status, x => x.Count);

            var stats = new InvitationStatistics
            {
                TotalInvitations = statusCounts.Sum(x => x.Count),
                PendingApprovals = countByStatus.GetValueOrDefault(InvitationStatus.Submitted) +
                                   countByStatus.GetValueOrDefault(InvitationStatus.UnderReview),
                ApprovedInvitations = countByStatus.GetValueOrDefault(InvitationStatus.Approved),
                ActiveVisitors = countByStatus.GetValueOrDefault(InvitationStatus.Active),
                CompletedVisits = countByStatus.GetValueOrDefault(InvitationStatus.Completed),
                CancelledInvitations = countByStatus.GetValueOrDefault(InvitationStatus.Cancelled),
                ExpiredInvitations = countByStatus.GetValueOrDefault(InvitationStatus.Expired),
                StatusBreakdown = countByStatus
            };

            // Average visit duration only requires completed invitations with timestamps
            var avgDuration = await _dbSet
                .Where(i => i.Status == InvitationStatus.Completed && i.CheckedInAt != null && i.CheckedOutAt != null)
                .Select(i => EF.Functions.DateDiffMinute(i.CheckedInAt!.Value, i.CheckedOutAt!.Value))
                .AverageAsync(m => (double?)m, cancellationToken);

            if (avgDuration.HasValue)
                stats.AverageVisitDuration = avgDuration.Value / 60.0; // convert minutes to hours

            return stats;
        }

        public async Task<InvitationStatistics> GetStatisticsByUserAccessAsync(int userId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("GetStatisticsByUserAccessAsync called for userId={UserId}", userId);

            // Get accessible visitor IDs for this user
            var accessibleVisitorIds = await _context.Set<VisitorAccess>()
                .IgnoreQueryFilters()
                .Where(va => va.UserId == userId && va.IsActive)
                .Select(va => va.VisitorId)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("User {UserId} has access to {Count} visitors", userId, accessibleVisitorIds.Count);

            // Use GROUP BY projection — no full table scan
            var baseQuery = _dbSet.Where(i => accessibleVisitorIds.Contains(i.VisitorId));

            var statusCounts = await baseQuery
                .GroupBy(i => i.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var countByStatus = statusCounts.ToDictionary(x => x.Status, x => x.Count);

            var stats = new InvitationStatistics
            {
                TotalInvitations = statusCounts.Sum(x => x.Count),
                PendingApprovals = countByStatus.GetValueOrDefault(InvitationStatus.Submitted) +
                                   countByStatus.GetValueOrDefault(InvitationStatus.UnderReview),
                ApprovedInvitations = countByStatus.GetValueOrDefault(InvitationStatus.Approved),
                ActiveVisitors = countByStatus.GetValueOrDefault(InvitationStatus.Active),
                CompletedVisits = countByStatus.GetValueOrDefault(InvitationStatus.Completed),
                CancelledInvitations = countByStatus.GetValueOrDefault(InvitationStatus.Cancelled),
                ExpiredInvitations = countByStatus.GetValueOrDefault(InvitationStatus.Expired),
                StatusBreakdown = countByStatus
            };

            var avgDuration = await baseQuery
                .Where(i => i.Status == InvitationStatus.Completed && i.CheckedInAt != null && i.CheckedOutAt != null)
                .Select(i => EF.Functions.DateDiffMinute(i.CheckedInAt!.Value, i.CheckedOutAt!.Value))
                .AverageAsync(m => (double?)m, cancellationToken);

            if (avgDuration.HasValue)
                stats.AverageVisitDuration = avgDuration.Value / 60.0;

            _logger.LogDebug("Returning stats: Total={Total}, Pending={Pending}, Active={Active}",
                stats.TotalInvitations, stats.PendingApprovals, stats.ActiveVisitors);

            return stats;
        }

        public async Task<Invitation?> GetByInvitationNumberAsync(string invitationNumber, CancellationToken cancellationToken = default)
        {
            return await ApplyIncludes(_dbSet)
                .FirstOrDefaultAsync(i => i.InvitationNumber == invitationNumber, cancellationToken);
        }

        public async Task<Invitation?> GetByQrCodeAsync(string qrCode, CancellationToken cancellationToken = default)
        {
            return await ApplyIncludes(_dbSet)
                .FirstOrDefaultAsync(i => i.QrCode == qrCode, cancellationToken);
        }

        public async Task<List<Invitation>> GetByVisitorIdAndStatusAsync(int visitorId, InvitationStatus status, CancellationToken cancellationToken = default)
        {
            return await ApplyIncludes(_dbSet)
                .Where(i => i.VisitorId == visitorId && i.Status == status)
                .ToListAsync(cancellationToken);
        }

        // Methods moved from RepositoryExtensions
        public async Task<IEnumerable<Invitation>> GetTodaysInvitationsForHostAsync(
            int hostId, 
            DateTime startDate, 
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            return await ApplyIncludes(_dbSet)
                .Where(inv => inv.HostId == hostId && 
                           inv.ScheduledStartTime >= startDate && 
                           inv.ScheduledStartTime < endDate &&
                           inv.IsActive)
                .OrderBy(inv => inv.ScheduledStartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<Invitation?> GetTodaysInvitationForVisitorAsync(
            int visitorId,
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            return await ApplyIncludes(_dbSet)
                .FirstOrDefaultAsync(
                    inv => inv.VisitorId == visitorId &&
                           inv.ScheduledStartTime >= startDate &&
                           inv.ScheduledStartTime < endDate &&
                           inv.IsActive,
                    cancellationToken);
        }

        public async Task<int> GetPendingApprovalsCountAsync(
            CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(
                inv => inv.Status == InvitationStatus.Submitted && inv.IsActive,
                cancellationToken);
        }

        public async Task<int> GetTodaysInvitationsCountAsync(
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            return await _dbSet.CountAsync(
                inv => inv.ScheduledStartTime >= startDate &&
                       inv.ScheduledStartTime < endDate &&
                       inv.IsActive,
                cancellationToken);
        }

        public async Task<IEnumerable<Invitation>> GetOverstayedInvitationsAsync(
            DateTime threshold,
            CancellationToken cancellationToken = default)
        {
            return await ApplyIncludes(_dbSet)
                .Where(inv => inv.ScheduledEndTime < threshold &&
                           inv.Status == InvitationStatus.Active &&
                           inv.IsActive)
                .OrderBy(inv => inv.ScheduledEndTime)
                .ToListAsync(cancellationToken);
        }
    }
}
