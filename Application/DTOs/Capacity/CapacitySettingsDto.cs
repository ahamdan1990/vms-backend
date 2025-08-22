namespace VisitorManagementSystem.Api.Application.DTOs.Capacity;

/// <summary>
/// DTO for capacity settings
/// </summary>
public class CapacitySettingsDto
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public string? LocationName { get; set; }
    public int MaxCapacity { get; set; }
    public int WarningThreshold { get; set; }
    public double BufferPercentage { get; set; }
    public bool AllowOverbooking { get; set; }
    public double MaxOverbookingPercentage { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// DTO for creating capacity settings
/// </summary>
public class CreateCapacitySettingsDto
{
    public int LocationId { get; set; }
    public int MaxCapacity { get; set; }
    public int WarningThreshold { get; set; }
    public double BufferPercentage { get; set; } = 0.1; // 10% default
    public bool AllowOverbooking { get; set; } = false;
    public double MaxOverbookingPercentage { get; set; } = 0.0;
}

/// <summary>
/// DTO for updating capacity settings
/// </summary>
public class UpdateCapacitySettingsDto
{
    public int MaxCapacity { get; set; }
    public int WarningThreshold { get; set; }
    public double BufferPercentage { get; set; }
    public bool AllowOverbooking { get; set; }
    public double MaxOverbookingPercentage { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for occupancy information that matches frontend OccupancyCard expectations
/// </summary>
public class OccupancyDto
{
    public DateTime DateTime { get; set; }
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public int? TimeSlotId { get; set; }
    public string? TimeSlotName { get; set; }
    public int CurrentOccupancy { get; set; }
    public int MaxCapacity { get; set; }
    public int AvailableSlots { get; set; }
    public decimal OccupancyPercentage { get; set; }
    public bool IsWarningLevel { get; set; }
    public bool IsAtCapacity { get; set; }
    public DateTime LastUpdated { get; set; }
}