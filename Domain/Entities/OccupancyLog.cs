using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Tracks real-time facility occupancy for capacity management
/// </summary>
public class OccupancyLog : BaseEntity
{
    /// <summary>
    /// Date of the occupancy record
    /// </summary>
    [Required]
    public DateTime Date { get; set; }

    /// <summary>
    /// Time slot this record applies to
    /// </summary>
    public int? TimeSlotId { get; set; }

    /// <summary>
    /// Location this record applies to
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Current visitor count for this time/location
    /// </summary>
    [Range(0, 10000)]
    public int CurrentCount { get; set; } = 0;

    /// <summary>
    /// Maximum capacity for this time/location
    /// </summary>
    [Range(1, 10000)]
    public int MaxCapacity { get; set; }

    /// <summary>
    /// Reserved capacity (pre-approved invitations)
    /// </summary>
    [Range(0, 10000)]
    public int ReservedCount { get; set; } = 0;

    /// <summary>
    /// Available capacity (calculated)
    /// </summary>
    public int AvailableCapacity { get; private set; }

    /// <summary>
    /// Occupancy percentage (0-100)
    /// </summary>
    public decimal OccupancyPercentage { get; private set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    [Required]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to time slot
    /// </summary>
    public virtual TimeSlot? TimeSlot { get; set; }

    /// <summary>
    /// Navigation property to location
    /// </summary>
    public virtual Location? Location { get; set; }

    /// <summary>
    /// Checks if capacity is exceeded
    /// </summary>
    /// <returns>True if over capacity</returns>
    public bool IsOverCapacity()
    {
        return (CurrentCount + ReservedCount) > MaxCapacity;
    }

    /// <summary>
    /// Checks if capacity is at warning level (>80%)
    /// </summary>
    /// <returns>True if at warning level</returns>
    public bool IsAtWarningLevel()
    {
        return GetCalculatedOccupancyPercentage() >= 80;
    }

    /// <summary>
    /// Checks if there's available capacity for additional visitors
    /// </summary>
    /// <param name="additionalVisitors">Number of additional visitors</param>
    /// <returns>True if capacity is available</returns>
    public bool HasAvailableCapacity(int additionalVisitors)
    {
        return (CurrentCount + ReservedCount + additionalVisitors) <= MaxCapacity;
    }

    /// <summary>
    /// Calculates available capacity manually (for when computed column isn't available)
    /// </summary>
    /// <returns>Available capacity</returns>
    public int GetCalculatedAvailableCapacity()
    {
        return MaxCapacity - CurrentCount - ReservedCount;
    }

    /// <summary>
    /// Calculates occupancy percentage manually (for when computed column isn't available)
    /// </summary>
    /// <returns>Occupancy percentage</returns>
    public decimal GetCalculatedOccupancyPercentage()
    {
        return MaxCapacity > 0 ? 
            Math.Round((decimal)(CurrentCount + ReservedCount) / MaxCapacity * 100, 2) : 0;
    }

    /// <summary>
    /// Updates the occupancy counts
    /// </summary>
    /// <param name="currentCount">New current count</param>
    /// <param name="reservedCount">New reserved count</param>
    public void UpdateCounts(int currentCount, int reservedCount)
    {
        CurrentCount = Math.Max(0, currentCount);
        ReservedCount = Math.Max(0, reservedCount);
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates the occupancy log data
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> ValidateOccupancyLog()
    {
        var errors = new List<string>();

        if (Date == default)
            errors.Add("Date is required.");

        if (MaxCapacity < 1)
            errors.Add("Maximum capacity must be greater than zero.");

        if (CurrentCount < 0)
            errors.Add("Current count cannot be negative.");

        if (ReservedCount < 0)
            errors.Add("Reserved count cannot be negative.");

        if (Date > DateTime.UtcNow.AddDays(365))
            errors.Add("Date cannot be more than 1 year in the future.");

        return errors;
    }
}
