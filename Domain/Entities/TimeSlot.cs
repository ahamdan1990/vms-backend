using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents available time slots for visitor appointments with capacity management
/// </summary>
public class TimeSlot : SoftDeleteEntity
{
    /// <summary>
    /// Time slot name/identifier
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Start time of the slot
    /// </summary>
    [Required]
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// End time of the slot
    /// </summary>
    [Required]
    public TimeOnly EndTime { get; set; }

    /// <summary>
    /// Maximum visitors allowed in this time slot
    /// </summary>
    [Range(1, 10000)]
    public int MaxVisitors { get; set; } = 50;

    /// <summary>
    /// Days of week this slot is active (comma-separated: 1=Monday, 7=Sunday)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string ActiveDays { get; set; } = "1,2,3,4,5"; // Monday to Friday by default

    /// <summary>
    /// Location this time slot applies to (null = applies to all locations)
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Whether this time slot is currently active
    /// </summary>
    public new bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int DisplayOrder { get; set; } = 1;

    /// <summary>
    /// Buffer time between appointments in minutes
    /// </summary>
    [Range(0, 60)]
    public int BufferMinutes { get; set; } = 15;

    /// <summary>
    /// Whether to allow overlapping appointments
    /// </summary>
    public bool AllowOverlapping { get; set; } = true;

    /// <summary>
    /// Navigation property to location
    /// </summary>
    public virtual Location? Location { get; set; }

    /// <summary>
    /// Gets the duration of this time slot in minutes
    /// </summary>
    public int DurationMinutes => (int)(EndTime - StartTime).TotalMinutes;

    /// <summary>
    /// Checks if a given time falls within this slot
    /// </summary>
    /// <param name="dateTime">DateTime to check</param>
    /// <returns>True if the time falls within this slot</returns>
    public bool ContainsTime(DateTime dateTime)
    {
        var timeOnly = TimeOnly.FromDateTime(dateTime);
        return timeOnly >= StartTime && timeOnly <= EndTime;
    }

    /// <summary>
    /// Checks if this time slot is active on a specific day
    /// </summary>
    /// <param name="dayOfWeek">Day of week to check</param>
    /// <returns>True if active on this day</returns>
    public bool IsActiveOnDay(DayOfWeek dayOfWeek)
    {
        var dayNumber = ((int)dayOfWeek == 0) ? 7 : (int)dayOfWeek; // Convert Sunday from 0 to 7
        return ActiveDays.Split(',').Contains(dayNumber.ToString());
    }

    /// <summary>
    /// Checks if an appointment time conflicts with this slot considering buffer time
    /// </summary>
    /// <param name="appointmentStart">Appointment start time</param>
    /// <param name="appointmentEnd">Appointment end time</param>
    /// <returns>True if there's a conflict</returns>
    public bool HasTimeConflict(DateTime appointmentStart, DateTime appointmentEnd)
    {
        if (!IsActiveOnDay(appointmentStart.DayOfWeek))
            return false;

        var slotStart = appointmentStart.Date.Add(StartTime.ToTimeSpan());
        var slotEnd = appointmentStart.Date.Add(EndTime.ToTimeSpan());
        
        // Add buffer time
        var bufferedSlotStart = slotStart.AddMinutes(-BufferMinutes);
        var bufferedSlotEnd = slotEnd.AddMinutes(BufferMinutes);

        return appointmentStart < bufferedSlotEnd && appointmentEnd > bufferedSlotStart;
    }

    /// <summary>
    /// Validates the time slot configuration
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> ValidateTimeSlot()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Time slot name is required.");

        if (EndTime <= StartTime)
            errors.Add("End time must be after start time.");

        if (MaxVisitors < 1)
            errors.Add("Maximum visitors must be at least 1.");

        if (string.IsNullOrWhiteSpace(ActiveDays))
            errors.Add("Active days must be specified.");

        if (BufferMinutes < 0)
            errors.Add("Buffer minutes cannot be negative.");

        // Validate active days format
        var dayNumbers = ActiveDays.Split(',');
        foreach (var day in dayNumbers)
        {
            if (!int.TryParse(day.Trim(), out var dayNum) || dayNum < 1 || dayNum > 7)
            {
                errors.Add("Active days must be comma-separated numbers 1-7 (1=Monday, 7=Sunday).");
                break;
            }
        }

        return errors;
    }

    /// <summary>
    /// Gets formatted display string
    /// </summary>
    /// <returns>Formatted time slot display</returns>
    public string GetDisplayString()
    {
        return $"{Name} ({StartTime:HH:mm} - {EndTime:HH:mm}) - Max: {MaxVisitors}";
    }
}
