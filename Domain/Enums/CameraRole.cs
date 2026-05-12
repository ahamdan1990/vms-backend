namespace VisitorManagementSystem.Api.Domain.Enums;

/// <summary>
/// Role a camera plays in visitor/staff movement workflows.
/// </summary>
public enum CameraRole
{
    /// <summary>
    /// General purpose camera with no entry/exit workflow implication.
    /// </summary>
    General = 0,

    /// <summary>
    /// Camera used to detect people entering the facility.
    /// </summary>
    Entry = 1,

    /// <summary>
    /// Camera used to detect people leaving the facility.
    /// </summary>
    Exit = 2,

    /// <summary>
    /// Monitoring-only camera that should not trigger check-in/check-out.
    /// </summary>
    Monitoring = 3
}
