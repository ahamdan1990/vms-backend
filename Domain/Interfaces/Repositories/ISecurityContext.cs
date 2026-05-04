namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Domain-scoped context for the security and monitoring aggregate.
/// Handlers dealing with operator sessions, alerts, cameras, and locations should
/// depend on this rather than the full IUnitOfWork.
/// </summary>
public interface ISecurityContext : ITransactionalContext
{
    IOperatorSessionRepository OperatorSessions { get; }
    IAlertEscalationRepository AlertEscalations { get; }
    INotificationAlertRepository NotificationAlerts { get; }
    ICameraRepository Cameras { get; }
    ILocationRepository Locations { get; }
}
