using Microsoft.EntityFrameworkCore;
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
        public InvitationRepository(ApplicationDbContext context) : base(context)
        {
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
                            i.Status != InvitationStatus.Completed &&
                            i.Status != InvitationStatus.Cancelled &&
                            i.Status != InvitationStatus.Expired)
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
            var invitations = await ApplyIncludes(_dbSet).ToListAsync(cancellationToken);

            var stats = new InvitationStatistics
            {
                TotalInvitations = invitations.Count,
                PendingApprovals = invitations.Count(i => i.Status == InvitationStatus.Submitted || i.Status == InvitationStatus.UnderReview),
                ApprovedInvitations = invitations.Count(i => i.Status == InvitationStatus.Approved),
                ActiveVisitors = invitations.Count(i => i.Status == InvitationStatus.Active),
                CompletedVisits = invitations.Count(i => i.Status == InvitationStatus.Completed),
                CancelledInvitations = invitations.Count(i => i.Status == InvitationStatus.Cancelled),
                ExpiredInvitations = invitations.Count(i => i.Status == InvitationStatus.Expired),
                StatusBreakdown = invitations.GroupBy(i => i.Status).ToDictionary(g => g.Key, g => g.Count())
            };

            var completedWithTimes = invitations
                .Where(i => i.Status == InvitationStatus.Completed && i.CheckedInAt.HasValue && i.CheckedOutAt.HasValue)
                .ToList();

            if (completedWithTimes.Any())
            {
                stats.AverageVisitDuration = completedWithTimes
                    .Average(i => (i.CheckedOutAt!.Value - i.CheckedInAt!.Value).TotalHours);
            }

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
