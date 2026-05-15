using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Repositories;

public class CameraFaceEventRepository : BaseRepository<CameraFaceEvent>, ICameraFaceEventRepository
{
    private readonly ApplicationDbContext _ctx;

    public CameraFaceEventRepository(ApplicationDbContext context) : base(context)
    {
        _ctx = context;
    }

    public async Task<(List<CameraFaceEvent> Events, int Total)> GetPagedAsync(
        int? cameraId = null,
        bool? isKnown = null,
        FaceEventReviewStatus? reviewStatus = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 0,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _ctx.CameraFaceEvents.AsQueryable();

        if (cameraId.HasValue)
            query = query.Where(e => e.CameraId == cameraId.Value);

        if (isKnown.HasValue)
            query = query.Where(e => e.IsKnown == isKnown.Value);

        if (reviewStatus.HasValue)
            query = query.Where(e => e.ReviewStatus == reviewStatus.Value);

        if (from.HasValue)
            query = query.Where(e => e.CapturedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.CapturedAt <= to.Value);

        var total = await query.CountAsync(cancellationToken);
        var events = await query
            .OrderByDescending(e => e.CapturedAt)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (events, total);
    }

    public async Task<CameraFaceEvent?> GetByEventIdAsync(
        string eventId,
        CancellationToken cancellationToken = default)
    {
        return await _ctx.CameraFaceEvents
            .FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
    }

    public async Task<int> GetKnownCandidateCountAsync(
        int personId,
        string personType,
        CancellationToken cancellationToken = default)
    {
        return await _ctx.CameraFaceEvents
            .CountAsync(e => e.PersonId == personId &&
                             e.PersonType == personType &&
                             e.IsKnownCandidate,
                        cancellationToken);
    }

    public async Task<CameraFaceEvent?> GetOldestKnownCandidateAsync(
        int personId,
        string personType,
        CancellationToken cancellationToken = default)
    {
        return await _ctx.CameraFaceEvents
            .Where(e => e.PersonId == personId &&
                        e.PersonType == personType &&
                        e.IsKnownCandidate)
            .OrderBy(e => e.CapturedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<CameraFaceEvent>> GetExpiredEventsAsync(
        DateTime cutoff,
        int batchSize = 200,
        CancellationToken cancellationToken = default)
    {
        return await _ctx.CameraFaceEvents
            .Where(e => e.ExpiresAt.HasValue && e.ExpiresAt.Value <= cutoff)
            .OrderBy(e => e.ExpiresAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task HardDeleteBatchAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
            return;

        await _ctx.CameraFaceEvents
            .Where(e => idList.Contains(e.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> BulkClearAsync(
        int? cameraId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _ctx.CameraFaceEvents.AsQueryable();

        if (cameraId.HasValue)
            query = query.Where(e => e.CameraId == cameraId.Value);

        if (from.HasValue)
            query = query.Where(e => e.CapturedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.CapturedAt <= to.Value);

        return await query.ExecuteDeleteAsync(cancellationToken);
    }
}
