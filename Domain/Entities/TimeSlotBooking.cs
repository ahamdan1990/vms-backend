using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents a booking reservation for a specific time slot on a specific date
/// </summary>
public class TimeSlotBooking : SoftDeleteEntity
{
    /// <summary>
    /// The time slot being booked
    /// </summary>
    [Required]
    public int TimeSlotId { get; set; }

    /// <summary>
    /// The date for which this slot is booked (date only, time is from TimeSlot)
    /// </summary>
    [Required]
    public DateTime BookingDate { get; set; }

    /// <summary>
    /// The invitation associated with this booking (optional for walk-ins)
    /// </summary>
    public int? InvitationId { get; set; }

    /// <summary>
    /// Number of visitors for this booking
    /// </summary>
    [Range(1, 1000)]
    public int VisitorCount { get; set; } = 1;

    /// <summary>
    /// Current booking status
    /// </summary>
    [Required]
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

    /// <summary>
    /// Additional notes or comments for this booking
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// User who created this booking
    /// </summary>
    [Required]
    public int BookedBy { get; set; }

    /// <summary>
    /// When the booking was made
    /// </summary>
    [Required]
    public DateTime BookedOn { get; set; }

    /// <summary>
    /// When the booking was cancelled (if applicable)
    /// </summary>
    public DateTime? CancelledOn { get; set; }

    /// <summary>
    /// User who cancelled the booking
    /// </summary>
    public int? CancelledBy { get; set; }

    /// <summary>
    /// Reason for cancellation
    /// </summary>
    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    // Navigation Properties
    /// <summary>
    /// Navigation property for the time slot
    /// </summary>
    public virtual TimeSlot TimeSlot { get; set; } = null!;

    /// <summary>
    /// Navigation property for the invitation
    /// </summary>
    public virtual Invitation? Invitation { get; set; }

    /// <summary>
    /// Navigation property for the user who booked
    /// </summary>
    public virtual User BookedByUser { get; set; } = null!;

    /// <summary>
    /// Navigation property for the user who cancelled
    /// </summary>
    public virtual User? CancelledByUser { get; set; }

    // Business Methods
    /// <summary>
    /// Checks if the booking is active (confirmed or pending)
    /// </summary>
    public bool IsActiveBooking => Status == BookingStatus.Confirmed || Status == BookingStatus.Pending;

    /// <summary>
    /// Checks if the booking can be cancelled
    /// </summary>
    public bool CanBeCancelled => Status != BookingStatus.Cancelled && Status != BookingStatus.Completed;

    /// <summary>
    /// Cancels the booking
    /// </summary>
    /// <param name="cancelledBy">User cancelling the booking</param>
    /// <param name="reason">Reason for cancellation</param>
    public void Cancel(int cancelledBy, string? reason = null)
    {
        if (!CanBeCancelled)
            throw new InvalidOperationException("This booking cannot be cancelled.");

        Status = BookingStatus.Cancelled;
        CancelledOn = DateTime.UtcNow;
        CancelledBy = cancelledBy;
        CancellationReason = reason?.Trim();
        UpdateModifiedBy(cancelledBy);
    }

    /// <summary>
    /// Confirms a pending booking
    /// </summary>
    /// <param name="confirmedBy">User confirming the booking</param>
    public void Confirm(int confirmedBy)
    {
        if (Status != BookingStatus.Pending)
            throw new InvalidOperationException("Only pending bookings can be confirmed.");

        Status = BookingStatus.Confirmed;
        UpdateModifiedBy(confirmedBy);
    }

    /// <summary>
    /// Marks the booking as completed
    /// </summary>
    /// <param name="completedBy">User marking the booking as completed</param>
    public void Complete(int completedBy)
    {
        if (Status != BookingStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed bookings can be completed.");

        Status = BookingStatus.Completed;
        UpdateModifiedBy(completedBy);
    }

    /// <summary>
    /// Validates the booking data
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> ValidateBooking()
    {
        var errors = new List<string>();

        if (BookingDate.Date < DateTime.UtcNow.Date)
            errors.Add("Booking date cannot be in the past.");

        if (VisitorCount <= 0)
            errors.Add("Visitor count must be greater than 0.");

        // Check if time slot is provided (will be validated when loaded)
        if (TimeSlot != null)
        {
            if (!TimeSlot.IsActive)
                errors.Add("The selected time slot is not active.");

            if (VisitorCount > TimeSlot.MaxVisitors)
                errors.Add($"Visitor count ({VisitorCount}) exceeds time slot capacity ({TimeSlot.MaxVisitors}).");

            // Check day of week
            var dayOfWeek = (int)BookingDate.DayOfWeek == 0 ? 7 : (int)BookingDate.DayOfWeek;
            var activeDays = TimeSlot.ActiveDays?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => int.TryParse(d.Trim(), out var day) ? day : 0)
                .Where(d => d > 0)
                .ToList() ?? new List<int>();

            if (activeDays.Any() && !activeDays.Contains(dayOfWeek))
                errors.Add($"The selected time slot is not active on {BookingDate.DayOfWeek}.");
        }

        return errors;
    }

    /// <summary>
    /// Gets a summary of the booking
    /// </summary>
    /// <returns>Booking summary</returns>
    public string GetSummary()
    {
        return $"{TimeSlot?.Name ?? "Time Slot"} on {BookingDate:yyyy-MM-dd} for {VisitorCount} visitor(s) - {Status}";
    }
}
