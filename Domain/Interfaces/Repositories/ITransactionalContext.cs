using Microsoft.EntityFrameworkCore.Storage;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Minimal persistence contract shared by all domain-scoped context interfaces.
/// Provides save and transaction primitives without exposing the full IUnitOfWork surface.
/// </summary>
public interface ITransactionalContext
{
    /// <summary>Current ambient transaction, if any.</summary>
    IDbContextTransaction? CurrentTransaction { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    IDbContextTransaction BeginTransaction();

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    void CommitTransaction();

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    void RollbackTransaction();

    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> func,
        CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(Func<Task> action,
        CancellationToken cancellationToken = default);
}
