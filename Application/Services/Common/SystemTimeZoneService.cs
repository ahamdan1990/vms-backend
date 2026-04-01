using VisitorManagementSystem.Api.Application.Services.Configuration;

namespace VisitorManagementSystem.Api.Application.Services.Common;

/// <summary>
/// Resolves the configured system timezone and converts report/dashboard date filters into UTC.
/// </summary>
public class SystemTimeZoneService : ISystemTimeZoneService
{
    private readonly IDynamicConfigurationService _dynamicConfigurationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SystemTimeZoneService> _logger;

    public SystemTimeZoneService(
        IDynamicConfigurationService dynamicConfigurationService,
        IConfiguration configuration,
        ILogger<SystemTimeZoneService> logger)
    {
        _dynamicConfigurationService = dynamicConfigurationService ?? throw new ArgumentNullException(nameof(dynamicConfigurationService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TimeZoneInfo> GetSystemTimeZoneAsync(CancellationToken cancellationToken = default)
    {
        var timeZoneId = await GetSystemTimeZoneIdAsync(cancellationToken);

        if (TryResolveTimeZone(timeZoneId, out var resolvedTimeZone))
        {
            return resolvedTimeZone;
        }

        _logger.LogWarning("Configured system timezone '{TimeZoneId}' could not be resolved on this host. Falling back to UTC.", timeZoneId);

        return TimeZoneInfo.Utc;
    }

    public async Task<string> GetSystemTimeZoneIdAsync(CancellationToken cancellationToken = default)
    {
        var fallback = _configuration["SystemSettings:DefaultTimeZone"];
        var configuredTimeZone = await _dynamicConfigurationService.GetConfigurationValueAsync(
            "SystemSettings",
            "DefaultTimeZone",
            fallback,
            cancellationToken);

        return string.IsNullOrWhiteSpace(configuredTimeZone) ? "UTC" : configuredTimeZone.Trim();
    }

    public async Task<TimeZoneInfo> ResolveTimeZoneAsync(string? preferredTimeZoneId, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(preferredTimeZoneId) &&
            TryResolveTimeZone(preferredTimeZoneId.Trim(), out var resolvedTimeZone))
        {
            return resolvedTimeZone;
        }

        if (!string.IsNullOrWhiteSpace(preferredTimeZoneId))
        {
            _logger.LogWarning(
                "Requested timezone '{TimeZoneId}' could not be resolved. Falling back to configured system timezone.",
                preferredTimeZoneId);
        }

        return await GetSystemTimeZoneAsync(cancellationToken);
    }

    public async Task<UtcDateRange> GetUtcDateRangeAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        var timeZone = await GetSystemTimeZoneAsync(cancellationToken);

        return new UtcDateRange
        {
            StartUtc = startDate.HasValue ? ConvertLocalDateToUtcStart(startDate.Value, timeZone) : null,
            EndUtcExclusive = endDate.HasValue ? ConvertLocalDateToUtcEndExclusive(endDate.Value, timeZone) : null
        };
    }

    public async Task<UtcDateRange> GetCurrentDayUtcRangeAsync(CancellationToken cancellationToken = default)
    {
        var timeZone = await GetSystemTimeZoneAsync(cancellationToken);
        var systemNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        var localStart = DateTime.SpecifyKind(systemNow.Date, DateTimeKind.Unspecified);

        return new UtcDateRange
        {
            StartUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone),
            EndUtcExclusive = TimeZoneInfo.ConvertTimeToUtc(localStart.AddDays(1), timeZone)
        };
    }

    public DateTime ConvertUtcToSystemTime(DateTime utcDateTime, TimeZoneInfo timeZone)
    {
        var normalizedUtc = EnsureUtc(utcDateTime);
        return TimeZoneInfo.ConvertTimeFromUtc(normalizedUtc, timeZone);
    }

    public DateTime? ConvertUtcToSystemTime(DateTime? utcDateTime, TimeZoneInfo timeZone)
    {
        if (!utcDateTime.HasValue)
        {
            return null;
        }

        return ConvertUtcToSystemTime(utcDateTime.Value, timeZone);
    }

    private static DateTime ConvertLocalDateToUtcStart(DateTime rawDate, TimeZoneInfo timeZone)
    {
        var localDate = NormalizeIncomingDate(rawDate, timeZone).Date;
        var unspecifiedLocalStart = DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecifiedLocalStart, timeZone);
    }

    private static DateTime ConvertLocalDateToUtcEndExclusive(DateTime rawDate, TimeZoneInfo timeZone)
    {
        var localDate = NormalizeIncomingDate(rawDate, timeZone).Date.AddDays(1);
        var unspecifiedLocalEnd = DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecifiedLocalEnd, timeZone);
    }

    private static DateTime NormalizeIncomingDate(DateTime rawDate, TimeZoneInfo timeZone)
    {
        return rawDate.Kind switch
        {
            DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(rawDate, timeZone),
            DateTimeKind.Local => TimeZoneInfo.ConvertTimeFromUtc(rawDate.ToUniversalTime(), timeZone),
            _ => rawDate
        };
    }

    private static DateTime EnsureUtc(DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };
    }

    private bool TryResolveTimeZone(string timeZoneId, out TimeZoneInfo timeZone)
    {
        if (TryFindTimeZone(timeZoneId, out timeZone))
        {
            return true;
        }

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsTimeZoneId) &&
            TryFindTimeZone(windowsTimeZoneId, out timeZone))
        {
            _logger.LogInformation(
                "Resolved configured IANA timezone '{ConfiguredTimeZoneId}' to Windows timezone '{ResolvedTimeZoneId}'.",
                timeZoneId,
                windowsTimeZoneId);

            return true;
        }

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZoneId, out var ianaTimeZoneId) &&
            TryFindTimeZone(ianaTimeZoneId, out timeZone))
        {
            _logger.LogInformation(
                "Resolved configured Windows timezone '{ConfiguredTimeZoneId}' to IANA timezone '{ResolvedTimeZoneId}'.",
                timeZoneId,
                ianaTimeZoneId);

            return true;
        }

        timeZone = TimeZoneInfo.Utc;
        return false;
    }

    private static bool TryFindTimeZone(string timeZoneId, out TimeZoneInfo timeZone)
    {
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
        }
        catch (InvalidTimeZoneException)
        {
        }

        timeZone = TimeZoneInfo.Utc;
        return false;
    }
}
