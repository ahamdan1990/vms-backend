namespace VisitorManagementSystem.Api.Domain.Enums;

/// <summary>
/// How a staff attendance record was created.
/// </summary>
public enum AttendanceMethod
{
    Manual  = 0,
    Camera  = 1,
    ApiKey  = 2
}
