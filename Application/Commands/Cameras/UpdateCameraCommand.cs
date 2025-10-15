using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Cameras;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Commands.Cameras;

/// <summary>
/// Command for updating an existing camera configuration and settings
/// Supports partial updates while maintaining data integrity and security
/// </summary>
public class UpdateCameraCommand : IRequest<CameraDto>
{
    /// <summary>
    /// Camera ID to update (required)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Updated camera display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Updated camera description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Updated camera type
    /// </summary>
    public CameraType CameraType { get; set; }

    /// <summary>
    /// Updated connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Updated authentication username
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Updated authentication password (will be encrypted)
    /// Null value means keep existing password, empty string means remove password
    /// </summary>
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
    public int Priority { get; set; } = 5;

    /// <summary>
    /// Updated manufacturer information
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Updated model information
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Updated firmware version
    /// </summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// Updated serial number
    /// </summary>
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Updated active status
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Updated metadata
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// ID of the user performing the update (for audit trail)
    /// </summary>
    public int ModifiedBy { get; set; }

    /// <summary>
    /// Whether to test the connection after updating configuration
    /// </summary>
    public bool TestConnection { get; set; } = false;
}