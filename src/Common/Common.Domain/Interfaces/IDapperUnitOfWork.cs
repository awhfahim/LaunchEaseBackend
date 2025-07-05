using System.Data;

namespace Common.Domain.Interfaces;

/// <summary>
/// Unit of Work interface for managing database transactions using Dapper
/// </summary>
public interface IDapperUnitOfWork : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Gets the current database connection
    /// </summary>
    IDbConnection Connection { get; }

    /// <summary>
    /// Gets the current transaction (if active)
    /// </summary>
    IDbTransaction? Transaction { get; }

    /// <summary>
    /// Indicates whether a transaction is currently active
    /// </summary>
    bool HasActiveTransaction { get; }

    /// <summary>
    /// Begin a new database transaction
    /// </summary>
    Task<IDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Commit the current transaction
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback the current transaction
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute work within a transaction scope
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> work,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute work within a transaction scope (void return)
    /// </summary>
    Task ExecuteInTransactionAsync(Func<IDbConnection, IDbTransaction, Task> work,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get repository instance
    /// </summary>
    TRepository GetRepository<TRepository>() where TRepository : class;
}