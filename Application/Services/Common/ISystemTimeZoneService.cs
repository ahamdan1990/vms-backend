namespace VisitorManagementSystem.Api.Application.Services.Common;

/// <summary>
/// Resolves the system/site timezone and normalizes date-only filters into UTC ranges.
/// </summary>
public interface ISystemTimeZoneService
{
    Task<TimeZoneInfo> GetSystemTimeZoneAsync(CancellationToken cancellationToken = default);
    Task<string> GetSystemTimeZoneIdAsync(CancellationToken cancellationToken = default);
    Task<TimeZoneInfo> ResolveTimeZoneAsync(string? preferredTimeZoneId, CancellationToken cancellationToken = default);
    Task<UtcDateRange> GetUtcDateRangeAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
    Task<UtcDateRange> GetCurrentDayUtcRangeAsync(CancellationToken cancellationToken = default);
    DateTime ConvertUtcToSystemTime(DateTime utcDateTime, TimeZoneInfo timeZone);
    DateTime? ConvertUtcToSystemTime(DateTime? utcDateTime, TimeZoneInfo timeZone);
}

/// <summary>
/// UTC date range with an exclusive end boundary.
/// </summary>
public sealed class UtcDateRange
{
    public DateTime? StartUtc { get; init; }
    public DateTime? EndUtcExclusive { get; init; }
}
