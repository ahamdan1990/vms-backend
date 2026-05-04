namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Domain-scoped context for the invitation aggregate.
/// Handlers that only operate on invitations and visit purposes should
/// depend on this rather than the full IUnitOfWork.
/// </summary>
public interface IInvitationContext : ITransactionalContext
{
    IInvitationRepository Invitations { get; }
    IVisitPurposeRepository VisitPurposes { get; }
}
