namespace VisitorManagementSystem.Api.Domain.Interfaces.Services
{
    /// <summary>
    /// Provides an abstraction for date and time operations to support testability.
    /// </summary>
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
        DateTime Now { get; }
        DateOnly Today { get; }
        DateOnly UtcToday { get; }
        TimeOnly TimeNow { get; }
        TimeOnly UtcTimeNow { get; }

        DateTime ConvertToUtc(DateTime dateTime, TimeZoneInfo timeZone);
        DateTime ConvertFromUtc(DateTime utcDateTime, TimeZoneInfo timeZone);
        DateTime ConvertToUserTimeZone(DateTime utcDateTime, string timeZoneId);
        DateTime AddBusinessDays(DateTime startDate, int businessDays);
        bool IsBusinessDay(DateTime date);
        int GetBusinessDaysBetween(DateTime startDate, DateTime endDate);
        DateTime StartOfDay(DateTime dateTime);
        DateTime EndOfDay(DateTime dateTime);
        DateTime StartOfWeek(DateTime dateTime, DayOfWeek startOfWeek = DayOfWeek.Monday);
        DateTime EndOfWeek(DateTime dateTime, DayOfWeek startOfWeek = DayOfWeek.Monday);
        DateTime StartOfMonth(DateTime dateTime);
        DateTime EndOfMonth(DateTime dateTime);
        DateTime StartOfYear(DateTime dateTime);
        DateTime EndOfYear(DateTime dateTime);
        bool IsWeekend(DateTime date);
        DateTime GetNextWeekday(DateTime date);
        DateTime GetPreviousWeekday(DateTime date);
        int GetAge(DateTime birthDate, DateTime? asOfDate = null);
        string GetRelativeTime(DateTime dateTime, DateTime? referenceTime = null);
        bool IsValidTimeZone(string timeZoneId);
        IEnumerable<TimeZoneInfo> GetSystemTimeZones();
        DateTime RoundToNearestMinute(DateTime dateTime, int minutes = 1);
        TimeSpan GetTimeUntil(DateTime targetDateTime, DateTime? fromDateTime = null);
        bool IsInDateRange(DateTime dateTime, DateTime startDate, DateTime endDate);
        DateTime Truncate(DateTime dateTime, TimeSpan timeSpan);
        IEnumerable<DateTime> GetDateRange(DateTime startDate, DateTime endDate, int dayIncrement = 1);
        DateTime ParseDateSafely(string dateString, DateTime defaultValue);
        DateTime? ParseDateSafely(string dateString);
    }
}