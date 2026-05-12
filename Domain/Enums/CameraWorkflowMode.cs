namespace VisitorManagementSystem.Api.Domain.Enums;

/// <summary>
/// Controls whether camera recognition only informs operators or also drives workflows.
/// </summary>
public enum CameraWorkflowMode
{
    /// <summary>
    /// Recognition events are informational only.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Receptionist must approve suggested check-in/check-out actions.
    /// </summary>
    Manual = 1,

    /// <summary>
    /// System performs allowed check-in/check-out actions and still notifies the receptionist.
    /// </summary>
    Automatic = 2
}
