namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Domain-scoped context for the identity and authentication aggregate.
/// Handlers that only touch users or refresh tokens should depend on this
/// rather than the full IUnitOfWork.
/// </summary>
public interface IUserContext : ITransactionalContext
{
    IUserRepository Users { get; }
    IRefreshTokenRepository RefreshTokens { get; }
}
