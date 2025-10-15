using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.DTOs.Cameras;

/// <summary>
/// Data transfer object for updating an existing camera
/// Contains all updatable fields with proper validation constraints
/// </summary>
public class UpdateCameraDto
{
    /// <summary>
    /// Updated camera display name
    /// </summary>
    [Required(ErrorMessage = "Camera name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Camera name must be between 1 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Updated camera description
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Updated camera type
    /// </summary>
    [Required(ErrorMessage = "Camera type is required")]
    public CameraType CameraType { get; set; }

    /// <summary>
    /// Updated connection string
    /// </summary>
    [Required(ErrorMessage = "Connection string is required")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Connection string must be between 1 and 500 characters")]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Updated authentication username
    /// </summary>
    [StringLength(100, ErrorMessage = "Username cannot exceed 100 characters")]
    public string? Username { get; set; }

    /// <summary>
    /// Updated authentication password
    /// </summary>
    [StringLength(200, ErrorMessage = "Password cannot exceed 200 characters")]
    public string? Password { get; set; }

    /// <summary>
    /// Updated location assignment
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Updated camera configuration settings
    /// </summary>
    public CameraConfigurationDto? Configuration { get; set; }

    /// <summary>
    /// Updated facial recognition enablement status
    /// </summary>
    public bool EnableFacialRecognition { get; set; } = true;

    /// <summary>
    /// Updated processing priority level
    /// </summary>
    [Range(1, 10, ErrorMessage = "Priority must be between 1 and 10")]
    public int Priority { get; set; } = 5;

    /// <summary>
    /// Updated manufacturer information
    /// </summary>
    [StringLength(100, ErrorMessage = "Manufacturer name cannot exceed 100 characters")]
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Updated model information
    /// </summary>
    [StringLength(100, ErrorMessage = "Model name cannot exceed 100 characters")]
    public string? Model { get; set; }

    /// <summary>
    /// Updated firmware version
    /// </summary>
    [StringLength(50, ErrorMessage = "Firmware version cannot exceed 50 characters")]
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// Updated serial number
    /// </summary>
    [StringLength(100, ErrorMessage = "Serial number cannot exceed 100 characters")]
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Updated active status
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Updated metadata
    /// </summary>
    public string? Metadata { get; set; }
}