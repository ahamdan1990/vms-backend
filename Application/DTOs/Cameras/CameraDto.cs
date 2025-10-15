using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.DTOs.Cameras;

/// <summary>
/// Camera data transfer object for API responses
/// Contains complete camera information including status and configuration
/// </summary>
public class CameraDto
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
    /// Camera description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Camera type (USB, RTSP, IP, ONVIF)
    /// </summary>
    public CameraType CameraType { get; set; }

    /// <summary>
    /// Camera type as display string
    /// </summary>
    public string CameraTypeDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Safe connection string (credentials masked for security)
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Authentication username (if applicable)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Current operational status
    /// </summary>
    public CameraStatus Status { get; set; }

    /// <summary>
    /// Status as display string
    /// </summary>
    public string StatusDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Associated location ID
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Associated location name
    /// </summary>
    public string? LocationName { get; set; }

    /// <summary>
    /// Camera configuration settings
    /// </summary>
    public CameraConfigurationDto? Configuration { get; set; }

    /// <summary>
    /// Timestamp of last health check
    /// </summary>
    public DateTime? LastHealthCheck { get; set; }

    /// <summary>
    /// Timestamp when camera was last online
    /// </summary>
    public DateTime? LastOnlineTime { get; set; }

    /// <summary>
    /// Last error message
    /// </summary>
    public string? LastErrorMessage { get; set; }

    /// <summary>
    /// Number of consecutive failures
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Whether facial recognition is enabled
    /// </summary>
    public bool EnableFacialRecognition { get; set; }

    /// <summary>
    /// Processing priority level
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Camera manufacturer
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Camera model
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Firmware version
    /// </summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// Serial number
    /// </summary>
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Whether camera is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether camera is deleted
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Whether camera is currently operational
    /// </summary>
    public bool IsOperational { get; set; }

    /// <summary>
    /// Whether camera is available for streaming
    /// </summary>
    public bool IsAvailableForStreaming { get; set; }

    /// <summary>
    /// Display name with location information
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Time since last health check (in minutes)
    /// </summary>
    public int? MinutesSinceLastHealthCheck { get; set; }

    /// <summary>
    /// Time since last online (in minutes)
    /// </summary>
    public int? MinutesSinceLastOnline { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>
    /// Name of user who created the camera
    /// </summary>
    public string? CreatedByName { get; set; }

    /// <summary>
    /// Name of user who last modified the camera
    /// </summary>
    public string? ModifiedByName { get; set; }
}