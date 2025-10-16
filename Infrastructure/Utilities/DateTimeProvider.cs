using VisitorManagementSystem.Api.Domain.Interfaces.Services;

namespace VisitorManagementSystem.Api.Infrastructure.Utilities;

/// <summary>
/// Default implementation of date/time provider for production use
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Now => DateTime.Now;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
    public DateOnly UtcToday => DateOnly.FromDateTime(DateTime.UtcNow);
    public TimeOnly TimeNow => TimeOnly.FromDateTime(DateTime.Now);
    public TimeOnly UtcTimeNow => TimeOnly.FromDateTime(DateTime.UtcNow);

    public DateTime ConvertToUtc(DateTime dateTime, TimeZoneInfo timeZone)
    {
        if (dateTime.Kind == DateTimeKind.Utc)
            return dateTime;

        return TimeZoneInfo.ConvertTimeToUtc(dateTime, timeZone);
    }

    public DateTime ConvertFromUtc(DateTime utcDateTime, TimeZoneInfo timeZone)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
            throw new ArgumentException("DateTime must be in UTC", nameof(utcDateTime));

        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
    }

    public DateTime ConvertToUserTimeZone(DateTime utcDateTime, string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return utcDateTime;

        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return ConvertFromUtc(utcDateTime, timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return utcDateTime;
        }
    }

    public DateTime AddBusinessDays(DateTime startDate, int businessDays)
    {
        if (businessDays == 0)
            return startDate;

        var direction = businessDays > 0 ? 1 : -1;
        var remainingDays = Math.Abs(businessDays);
        var currentDate = startDate;

        while (remainingDays > 0)
        {
            currentDate = currentDate.AddDays(direction);

            if (currentDate.DayOfWeek != DayOfWeek.Saturday &&
                currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                remainingDays--;
            }
        }

        return currentDate;
    }

    public bool IsBusinessDay(DateTime date)
    {
        return date.DayOfWeek != DayOfWeek.Saturday &&
               date.DayOfWeek != DayOfWeek.Sunday;
    }

    public int GetBusinessDaysBetween(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            (startDate, endDate) = (endDate, startDate);

        var businessDays = 0;
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            if (IsBusinessDay(currentDate))
                businessDays++;

            currentDate = currentDate.AddDays(1);
        }

        return businessDays;
    }

    public DateTime StartOfDay(DateTime dateTime)
    {
        return dateTime.Date;
    }

    public DateTime EndOfDay(DateTime dateTime)
    {
        return dateTime.Date.AddDays(1).AddTicks(-1);
    }

    public DateTime StartOfWeek(DateTime dateTime, DayOfWeek startOfWeek = DayOfWeek.Monday)
    {
        var diff = (7 + (dateTime.DayOfWeek - startOfWeek)) % 7;
        return dateTime.AddDays(-diff).Date;
    }

    public DateTime EndOfWeek(DateTime dateTime, DayOfWeek startOfWeek = DayOfWeek.Monday)
    {
        return StartOfWeek(dateTime, startOfWeek).AddDays(7).AddTicks(-1);
    }

    public DateTime StartOfMonth(DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1);
    }

    public DateTime EndOfMonth(DateTime dateTime)
    {
        return StartOfMonth(dateTime).AddMonths(1).AddTicks(-1);
    }

    public DateTime StartOfYear(DateTime dateTime)
    {
        return new DateTime(dateTime.Year, 1, 1);
    }

    public DateTime EndOfYear(DateTime dateTime)
    {
        return StartOfYear(dateTime).AddYears(1).AddTicks(-1);
    }

    public bool IsWeekend(DateTime date)
    {
        return date.DayOfWeek == DayOfWeek.Saturday ||
               date.DayOfWeek == DayOfWeek.Sunday;
    }

    public DateTime GetNextWeekday(DateTime date)
    {
        var nextDay = date.AddDays(1);
        while (IsWeekend(nextDay))
        {
            nextDay = nextDay.AddDays(1);
        }
        return nextDay;
    }

    public DateTime GetPreviousWeekday(DateTime date)
    {
        var prevDay = date.AddDays(-1);
        while (IsWeekend(prevDay))
        {
            prevDay = prevDay.AddDays(-1);
        }
        return prevDay;
    }

    public int GetAge(DateTime birthDate, DateTime? asOfDate = null)
    {
        var referenceDate = asOfDate ?? DateTime.Today;
        var age = referenceDate.Year - birthDate.Year;

        if (referenceDate.Month < birthDate.Month ||
            (referenceDate.Month == birthDate.Month && referenceDate.Day < birthDate.Day))
        {
            age--;
        }

        return age;
    }

    public string GetRelativeTime(DateTime dateTime, DateTime? referenceTime = null)
    {
        var reference = referenceTime ?? UtcNow;
        var timeSpan = reference - dateTime;

        if (timeSpan.TotalSeconds < 60)
            return "just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minute{(timeSpan.TotalMinutes == 1 ? "" : "s")} ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hour{(timeSpan.TotalHours == 1 ? "" : "s")} ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)timeSpan.TotalDays} day{(timeSpan.TotalDays == 1 ? "" : "s")} ago";
        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)} month{((int)(timeSpan.TotalDays / 30) == 1 ? "" : "s")} ago";

        return $"{(int)(timeSpan.TotalDays / 365)} year{((int)(timeSpan.TotalDays / 365) == 1 ? "" : "s")} ago";
    }

    public bool IsValidTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return false;

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
    }

    public IEnumerable<TimeZoneInfo> GetSystemTimeZones()
    {
        return TimeZoneInfo.GetSystemTimeZones();
    }

    public DateTime RoundToNearestMinute(DateTime dateTime, int minutes = 1)
    {
        var ticksInMinute = TimeSpan.TicksPerMinute * minutes;
        var roundedTicks = (long)(Math.Round((double)dateTime.Ticks / ticksInMinute) * ticksInMinute);
        return new DateTime(roundedTicks);
    }

    public TimeSpan GetTimeUntil(DateTime targetDateTime, DateTime? fromDateTime = null)
    {
        var from = fromDateTime ?? UtcNow;
        return targetDateTime - from;
    }

    public bool IsInDateRange(DateTime dateTime, DateTime startDate, DateTime endDate)
    {
        return dateTime >= startDate && dateTime <= endDate;
    }

    public DateTime Truncate(DateTime dateTime, TimeSpan timeSpan)
    {
        if (timeSpan == TimeSpan.Zero) return dateTime;
        return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
    }

    public IEnumerable<DateTime> GetDateRange(DateTime startDate, DateTime endDate, int dayIncrement = 1)
    {
        for (var date = startDate; date <= endDate; date = date.AddDays(dayIncrement))
        {
            yield return date;
        }
    }

    public DateTime ParseDateSafely(string dateString, DateTime defaultValue)
    {
        return DateTime.TryParse(dateString, out var result) ? result : defaultValue;
    }

    public DateTime? ParseDateSafely(string dateString)
    {
        return DateTime.TryParse(dateString, out var result) ? result : null;
    }
}