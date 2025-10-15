using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.DTOs.Cameras;

/// <summary>
/// Lightweight camera data transfer object for list views and basic operations
/// Optimized for paginated collections with minimal data transfer
/// </summary>
public class CameraListDto
{
    /// <summary>
    /// Camera unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Camera display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Camera type (USB, RTSP, IP, ONVIF)
    /// </summary>
    public CameraType CameraType { get; set; }

    /// <summary>
    /// Camera type as display string
    /// </summary>
    public string CameraTypeDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Current operational status
    /// </summary>
    public CameraStatus Status { get; set; }

    /// <summary>
    /// Status as display string with color coding hints
    /// </summary>
    public string StatusDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Associated location name (if any)
    /// </summary>
    public string? LocationName { get; set; }

    /// <summary>
    /// Whether camera is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether camera is operational (active, online, not deleted)
    /// </summary>
    public bool IsOperational { get; set; }

    /// <summary>
    /// Whether facial recognition is enabled
    /// </summary>
    public bool EnableFacialRecognition { get; set; }

    /// <summary>
    /// Processing priority level
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Number of consecutive failures
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Display name with location information
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Minutes since last health check (null if never checked)
    /// </summary>
    public int? MinutesSinceLastHealthCheck { get; set; }

    /// <summary>
    /// Health check status description
    /// </summary>
    public string HealthStatus { get; set; } = "Unknown";

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime? ModifiedOn { get; set; }
}