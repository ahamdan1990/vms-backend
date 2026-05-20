using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

public interface ICameraFaceEventRepository : IGenericRepository<CameraFaceEvent>
{
    Task<(List<CameraFaceEvent> Events, int Total)> GetPagedAsync(
        int? cameraId = null,
        bool? isKnown = null,
        FaceEventReviewStatus? reviewStatus = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 0,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<CameraFaceEvent?> GetByEventIdAsync(
        string eventId,
        CancellationToken cancellationToken = default);

    Task<CameraFaceEvent?> GetPendingKnownEventAsync(
        int cameraId,
        string personType,
        int personId,
        CancellationToken cancellationToken = default);

    Task<List<CameraFaceEvent>> GetAllPendingKnownEventsForPersonAsync(
        string personType,
        int personId,
        CancellationToken cancellationToken = default);

    Task<CameraFaceEvent?> GetPendingUnknownEventBySubjectAsync(
        int cameraId,
        string subjectId,
        CancellationToken cancellationToken = default);

    Task<List<CameraFaceEvent>> GetPendingUnknownEventsAsync(
        int cameraId,
        CancellationToken cancellationToken = default);

    Task<int> GetKnownCandidateCountAsync(
        int personId,
        string personType,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the oldest known-candidate snapshot for a person (for FIFO eviction).</summary>
    Task<CameraFaceEvent?> GetOldestKnownCandidateAsync(
        int personId,
        string personType,
        CancellationToken cancellationToken = default);

    /// <summary>Returns up to <paramref name="limit"/> high-quality candidate snapshots for a person, ordered by similarity descending.</summary>
    Task<List<CameraFaceEvent>> GetCandidateSnapshotsAsync(
        string personType,
        int personId,
        int limit = 5,
        CancellationToken cancellationToken = default);

    Task<List<CameraFaceEvent>> GetExpiredEventsAsync(
        DateTime cutoff,
        int batchSize = 200,
        CancellationToken cancellationToken = default);

    Task HardDeleteBatchAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hard-deletes all events matching the optional filters in a single statement.
    /// Returns the number of rows deleted.
    /// </summary>
    Task<int> BulkClearAsync(
        int? cameraId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);
}
