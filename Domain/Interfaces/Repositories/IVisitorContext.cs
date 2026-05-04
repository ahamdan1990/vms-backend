namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Domain-scoped context for the visitor aggregate.
/// Handlers that only operate on visitors and their associated data should
/// depend on this rather than the full IUnitOfWork.
/// </summary>
public interface IVisitorContext : ITransactionalContext
{
    IVisitorRepository Visitors { get; }
    IVisitorAccessRepository VisitorAccess { get; }
    IVisitorDocumentRepository VisitorDocuments { get; }
    IVisitorNoteRepository VisitorNotes { get; }
    IEmergencyContactRepository EmergencyContacts { get; }
}
