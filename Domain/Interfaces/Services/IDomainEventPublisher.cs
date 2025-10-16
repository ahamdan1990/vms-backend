using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Services;

/// <summary>
/// Interface for publishing domain events
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Publishes a domain event
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    /// <param name="domainEvent">Domain event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;

    /// <summary>
    /// Publishes multiple domain events
    /// </summary>
    /// <param name="domainEvents">List of domain events</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a domain event and waits for completion
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    /// <param name="domainEvent">Domain event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task PublishAndWaitAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}

/// <summary>
/// Base interface for domain events
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Event ID
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// When the event occurred
    /// </summary>
    DateTime OccurredOn { get; }

    /// <summary>
    /// Event type name
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Event data as JSON
    /// </summary>
    string EventData { get; }

    /// <summary>
    /// Correlation ID for tracking related events
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// User who triggered the event
    /// </summary>
    int? TriggeredBy { get; }
}

/// <summary>
/// Base class for domain events
/// </summary>
public abstract class BaseDomainEvent : IDomainEvent
{
    protected BaseDomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
        EventType = GetType().Name;
    }

    public Guid EventId { get; protected set; }
    public DateTime OccurredOn { get; protected set; }
    public string EventType { get; protected set; }
    public abstract string EventData { get; }
    public string? CorrelationId { get; set; }
    public int? TriggeredBy { get; set; }
}