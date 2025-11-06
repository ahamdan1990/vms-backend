namespace VisitorManagementSystem.Api.Domain.Enums;

/// <summary>
/// Status of a time slot booking
/// </summary>
public enum BookingStatus
{
    /// <summary>
    /// Booking is pending confirmation
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Booking is confirmed
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Booking has been cancelled
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// Booking has been completed (visitor checked out)
    /// </summary>
    Completed = 3
}
