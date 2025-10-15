using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents a camera device in the visitor management system
/// Supports RTSP, USB, IP, and ONVIF camera types for visitor monitoring and facial recognition
/// </summary>
public class Camera : SoftDeleteEntity
{
    /// <summary>
    /// Camera display name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Camera description for administrative purposes
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Type of camera (USB, RTSP, IP, ONVIF)
    /// </summary>
    [Required]
    public CameraType CameraType { get; set; }

    /// <summary>
    /// Camera connection string (RTSP URL, USB device path, IP address, etc.)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Optional authentication username for network cameras
    /// </summary>
    [MaxLength(100)]
    public string? Username { get; set; }

    /// <summary>
    /// Optional authentication password for network cameras (encrypted)
    /// </summary>
    [MaxLength(500)]
    public string? Password { get; set; }

    /// <summary>
    /// Current operational status of the camera
    /// </summary>
    [Required]
    public CameraStatus Status { get; set; } = CameraStatus.Inactive;

    /// <summary>
    /// Physical location where the camera is installed
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Navigation property to the associated location
    /// </summary>
    public virtual Location? Location { get; set; }

    /// <summary>
    /// Camera configuration settings (resolution, frame rate, etc.)
    /// Stored as JSON for flexibility
    /// </summary>
    public string? ConfigurationJson { get; set; }

    /// <summary>
    /// Timestamp of the last successful health check
    /// </summary>
    public DateTime? LastHealthCheck { get; set; }

    /// <summary>
    /// Timestamp when the camera was last seen online
    /// </summary>
    public DateTime? LastOnlineTime { get; set; }

    /// <summary>
    /// Error message from the last failed connection attempt
    /// </summary>
    [MaxLength(1000)]
    public string? LastErrorMessage { get; set; }

    /// <summary>
    /// Number of consecutive connection failures
    /// </summary>
    public int FailureCount { get; set; } = 0;

    /// <summary>
    /// Whether the camera should be enabled for facial recognition processing
    /// </summary>
    public bool EnableFacialRecognition { get; set; } = true;

    /// <summary>
    /// Priority level for camera processing (1 = highest priority)
    /// </summary>
    [Range(1, 10)]
    public int Priority { get; set; } = 5;

    /// <summary>
    /// Camera manufacturer/brand information
    /// </summary>
    [MaxLength(100)]
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Camera model information
    /// </summary>
    [MaxLength(100)]
    public string? Model { get; set; }

    /// <summary>
    /// Camera firmware version
    /// </summary>
    [MaxLength(50)]
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// Camera serial number or unique identifier
    /// </summary>
    [MaxLength(100)]
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Additional metadata stored as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets the camera configuration as a strongly-typed object
    /// </summary>
    public CameraConfiguration GetConfiguration()
    {
        return CameraConfiguration.FromJsonString(ConfigurationJson) ?? CameraConfiguration.Default;
    }

    /// <summary>
    /// Sets the camera configuration from a strongly-typed object
    /// </summary>
    public void SetConfiguration(CameraConfiguration configuration)
    {
        ConfigurationJson = configuration.ToJsonString();
        UpdateModifiedOn();
    }

    /// <summary>
    /// Updates the camera status and related timestamps
    /// </summary>
    public void UpdateStatus(CameraStatus newStatus, string? errorMessage = null, int? userId = null)
    {
        var previousStatus = Status;
        Status = newStatus;

        switch (newStatus)
        {
            case CameraStatus.Active:
                LastOnlineTime = DateTime.UtcNow;
                FailureCount = 0;
                LastErrorMessage = null;
                break;

            case CameraStatus.Error:
                FailureCount++;
                LastErrorMessage = errorMessage;
                break;

            case CameraStatus.Connecting:
                LastErrorMessage = null;
                break;

            case CameraStatus.Maintenance:
                LastErrorMessage = "Camera under maintenance";
                break;
        }

        LastHealthCheck = DateTime.UtcNow;

        if (userId.HasValue)
            UpdateModifiedBy(userId.Value);
        else
            UpdateModifiedOn();
    }

    /// <summary>
    /// Checks if the camera is currently operational
    /// </summary>
    public bool IsOperational()
    {
        return Status == CameraStatus.Active && IsActive && !IsDeleted;
    }

    /// <summary>
    /// Checks if the camera is available for streaming
    /// </summary>
    public bool IsAvailableForStreaming()
    {
        return IsOperational() && 
               (CameraType == CameraType.RTSP || CameraType == CameraType.IP || CameraType == CameraType.ONVIF);
    }

    /// <summary>
    /// Gets the display name with location information
    /// </summary>
    public string GetDisplayName()
    {
        if (Location != null)
            return $"{Name} ({Location.Name})";
        return Name;
    }

    /// <summary>
    /// Gets the connection string without sensitive information for logging
    /// </summary>
    public string GetSafeConnectionString()
    {
        if (string.IsNullOrEmpty(ConnectionString))
            return string.Empty;

        // For RTSP URLs, mask credentials
        if (CameraType == CameraType.RTSP && ConnectionString.Contains("://"))
        {
            try
            {
                var uri = new Uri(ConnectionString);
                var safeUri = new UriBuilder(uri)
                {
                    UserName = string.IsNullOrEmpty(uri.UserInfo) ? "" : "****",
                    Password = ""
                };
                return safeUri.ToString();
            }
            catch
            {
                return "****";
            }
        }

        return ConnectionString;
    }

    /// <summary>
    /// Validates the camera configuration
    /// </summary>
    public bool IsConfigurationValid(out List<string> validationErrors)
    {
        validationErrors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            validationErrors.Add("Camera name is required");

        if (string.IsNullOrWhiteSpace(ConnectionString))
            validationErrors.Add("Connection string is required");

        // Validate RTSP URL format
        if (CameraType == CameraType.RTSP)
        {
            if (!ConnectionString.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase))
                validationErrors.Add("RTSP cameras must have a valid RTSP URL starting with 'rtsp://'");
        }

        // Validate IP camera format
        if (CameraType == CameraType.IP)
        {
            if (!ConnectionString.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !ConnectionString.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                validationErrors.Add("IP cameras must have a valid HTTP/HTTPS URL");
        }

        // Validate camera configuration
        var config = GetConfiguration();
        if (!config.IsValid(out var configErrors))
            validationErrors.AddRange(configErrors);

        return validationErrors.Count == 0;
    }

    /// <summary>
    /// Resets failure count and error state
    /// </summary>
    public void ResetErrorState(int? userId = null)
    {
        FailureCount = 0;
        LastErrorMessage = null;
        
        if (userId.HasValue)
            UpdateModifiedBy(userId.Value);
        else
            UpdateModifiedOn();
    }

    /// <summary>
    /// Gets the time since last health check
    /// </summary>
    public TimeSpan? TimeSinceLastHealthCheck()
    {
        return LastHealthCheck.HasValue ? DateTime.UtcNow - LastHealthCheck.Value : null;
    }

    /// <summary>
    /// Gets the time since camera was last online
    /// </summary>
    public TimeSpan? TimeSinceLastOnline()
    {
        return LastOnlineTime.HasValue ? DateTime.UtcNow - LastOnlineTime.Value : null;
    }
}