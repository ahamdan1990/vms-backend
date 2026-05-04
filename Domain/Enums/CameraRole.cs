namespace VisitorManagementSystem.Api.Domain.Enums;

/// <summary>
/// Physical role of a camera at a door or access point.
/// Used by the Civil Defense module to route face-recognition results
/// to the correct check-in or check-out flow.
/// </summary>
public enum CameraRole
{
    None     = 0,
    Entry    = 1,
    Exit     = 2
}
