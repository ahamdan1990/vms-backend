namespace VisitorManagementSystem.Api.Domain.Enums;

/// <summary>
/// Enumeration of camera operational status states
/// </summary>
public enum CameraStatus
{
    /// <summary>
    /// Camera is offline or inactive
    /// </summary>
    Inactive = 0,

    /// <summary>
    /// Camera is active and operational
    /// </summary>
    Active = 1,

    /// <summary>
    /// Camera is attempting to establish connection
    /// </summary>
    Connecting = 2,

    /// <summary>
    /// Camera connection failed or error state
    /// </summary>
    Error = 3,

    /// <summary>
    /// Camera is temporarily disconnected
    /// </summary>
    Disconnected = 4,

    /// <summary>
    /// Camera is undergoing maintenance
    /// </summary>
    Maintenance = 5
}