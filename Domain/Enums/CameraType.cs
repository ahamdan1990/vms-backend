namespace VisitorManagementSystem.Api.Domain.Enums;

/// <summary>
/// Enumeration of supported camera types for the visitor management system
/// </summary>
public enum CameraType
{
    /// <summary>
    /// USB-connected camera device
    /// </summary>
    USB = 1,

    /// <summary>
    /// RTSP network stream camera
    /// </summary>
    RTSP = 2,

    /// <summary>
    /// IP camera with direct HTTP/HTTPS access
    /// </summary>
    IP = 3,

    /// <summary>
    /// ONVIF-compliant network camera
    /// </summary>
    ONVIF = 4
}