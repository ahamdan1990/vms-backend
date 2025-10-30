namespace VisitorManagementSystem.Api.Application.DTOs.Capacity;

/// <summary>
/// DTO for frontend capacity overview selector
/// Matches the expected format for selectOverviewSummary selector
/// </summary>
public class FrontendCapacityOverviewDto
{
    /// <summary>
    /// Location ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Location ID (for compatibility)
    /// </summary>
    public int LocationId { get; set; }

    /// <summary>
    /// Location name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Location name (for compatibility)
    /// </summary>
    public string LocationName { get; set; } = string.Empty;

    /// <summary>
    /// Maximum capacity for the location
    /// </summary>
    public int MaxCapacity { get; set; }

    /// <summary>
    /// Current occupancy count
    /// </summary>
    public int CurrentOccupancy { get; set; }

    /// <summary>
    /// Available slots (calculated)
    /// </summary>
    public int AvailableSlots { get; set; }

    /// <summary>
    /// Occupancy percentage (calculated)
    /// </summary>
    public decimal OccupancyPercentage { get; set; }

    /// <summary>
    /// Whether the location is at full capacity
    /// </summary>
    public bool IsAtCapacity { get; set; }

    /// <summary>
    /// Whether the location is at warning level (>= 80% capacity)
    /// </summary>
    public bool IsWarningLevel { get; set; }
}
