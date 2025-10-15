using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Cameras;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Commands.Cameras;

/// <summary>
/// Command for creating a new camera in the visitor management system
/// Includes comprehensive validation and configuration setup
/// </summary>
public class CreateCameraCommand : IRequest<CameraDto>
{
    /// <summary>
    /// Camera display name (required)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional camera description for administrative purposes
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of camera being added (USB, RTSP, IP, ONVIF)
    /// </summary>
    public CameraType CameraType { get; set; }

    /// <summary>
    /// Camera connection string (RTSP URL, USB device path, IP address, etc.)
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Optional authentication username for network cameras
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Optional authentication password for network cameras
    /// Will be encrypted before storage
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Physical location where the camera will be installed
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Camera configuration settings
    /// If null, default configuration will be applied
    /// </summary>
    public CameraConfigurationDto? Configuration { get; set; }

    /// <summary>
    /// Whether the camera should be enabled for facial recognition processing
    /// </summary>
    public bool EnableFacialRecognition { get; set; } = true;

    /// <summary>
    /// Processing priority level (1 = highest priority, 10 = lowest)
    /// </summary>
    public int Priority { get; set; } = 5;

    /// <summary>
    /// Camera manufacturer/brand information
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Camera model information
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Camera firmware version
    /// </summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// Camera serial number or unique identifier
    /// </summary>
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Whether the camera should be active upon creation
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Additional metadata as JSON string
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// ID of the user creating the camera (for audit trail)
    /// </summary>
    public int CreatedBy { get; set; }
}