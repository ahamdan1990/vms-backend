using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.DTOs.Cameras;

/// <summary>
/// Data transfer object for creating a new camera
/// Contains all required and optional fields for camera initialization
/// </summary>
public class CreateCameraDto
{
    /// <summary>
    /// Camera display name
    /// </summary>
    [Required(ErrorMessage = "Camera name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Camera name must be between 1 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional camera description for administrative purposes
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Type of camera being added
    /// </summary>
    [Required(ErrorMessage = "Camera type is required")]
    public CameraType CameraType { get; set; }

    /// <summary>
    /// Camera connection string (RTSP URL, USB device path, IP address, etc.)
    /// </summary>
    [Required(ErrorMessage = "Connection string is required")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Connection string must be between 1 and 500 characters")]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Optional authentication username for network cameras
    /// </summary>
    [StringLength(100, ErrorMessage = "Username cannot exceed 100 characters")]
    public string? Username { get; set; }

    /// <summary>
    /// Optional authentication password for network cameras
    /// </summary>
    [StringLength(200, ErrorMessage = "Password cannot exceed 200 characters")]
    public string? Password { get; set; }

    /// <summary>
    /// Physical location where the camera will be installed
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Initial camera configuration settings
    /// If not provided, default configuration will be applied
    /// </summary>
    public CameraConfigurationDto? Configuration { get; set; }

    /// <summary>
    /// Whether the camera should be enabled for facial recognition processing
    /// </summary>
    public bool EnableFacialRecognition { get; set; } = true;

    /// <summary>
    /// Processing priority level (1 = highest priority, 10 = lowest)
    /// </summary>
    [Range(1, 10, ErrorMessage = "Priority must be between 1 and 10")]
    public int Priority { get; set; } = 5;

    /// <summary>
    /// Camera manufacturer/brand information
    /// </summary>
    [StringLength(100, ErrorMessage = "Manufacturer name cannot exceed 100 characters")]
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Camera model information
    /// </summary>
    [StringLength(100, ErrorMessage = "Model name cannot exceed 100 characters")]
    public string? Model { get; set; }

    /// <summary>
    /// Camera firmware version
    /// </summary>
    [StringLength(50, ErrorMessage = "Firmware version cannot exceed 50 characters")]
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// Camera serial number or unique identifier
    /// </summary>
    [StringLength(100, ErrorMessage = "Serial number cannot exceed 100 characters")]
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Whether the camera should be active upon creation
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Additional metadata as JSON string
    /// </summary>
    public string? Metadata { get; set; }
}