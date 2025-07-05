using System.Data;
using System.Data.Common;
using Common.Application.Data;
using Common.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Infrastructure.Persistence;

/// <summary>
/// Dapper-based Unit of Work implementation for managing database transactions
/// </summary>
public class DapperUnitOfWork : IDapperUnitOfWork
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IServiceProvider _serviceProvider;
    private DbConnection? _connection;
    private DbTransaction? _transaction;
    private bool _disposed;

    public DapperUnitOfWork(IDbConnectionFactory connectionFactory, IServiceProvider serviceProvider)
    {
        _connectionFactory = connectionFactory;
        _serviceProvider = serviceProvider;
    }

    public IDbConnection Connection
    {
        get
        {
            if (_connection == null)
            {
                throw new InvalidOperationException(
                    "Connection is not initialized. Call BeginTransactionAsync first or ensure a transaction is active.");
            }

            return _connection;
        }
    }

    public IDbTransaction? Transaction => _transaction;

    public bool HasActiveTransaction => _transaction != null;

    public async Task<IDbTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DapperUnitOfWork));

        if (_transaction != null)
            throw new InvalidOperationException("A transaction is already active.");

        _connection ??= await _connectionFactory.OpenConnectionAsync(cancellationToken);

        _transaction = await _connection.BeginTransactionAsync(isolationLevel);
        return _transaction;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DapperUnitOfWork));

        if (_transaction == null)
            throw new InvalidOperationException("No active transaction to commit.");

        try
        {
            await _transaction.CommitAsync();
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DapperUnitOfWork));

        if (_transaction == null)
            throw new InvalidOperationException("No active transaction to rollback.");

        try
        {
            await _transaction.RollbackAsync();
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> work,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DapperUnitOfWork));

        var wasTransactionActive = HasActiveTransaction;

        if (!wasTransactionActive)
        {
            await BeginTransactionAsync(isolationLevel, cancellationToken);
        }

        try
        {
            var result = await work(Connection, Transaction!);

            if (!wasTransactionActive)
            {
                await CommitAsync(cancellationToken);
            }

            return result;
        }
        catch
        {
            if (!wasTransactionActive && HasActiveTransaction)
            {
                await RollbackAsync(cancellationToken);
            }

            throw;
        }
    }

    public async Task ExecuteInTransactionAsync(Func<IDbConnection, IDbTransaction, Task> work,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            await work(connection, transaction);
            return 0; // Dummy return value
        }, isolationLevel, cancellationToken);
    }

    public TRepository GetRepository<TRepository>() where TRepository : class
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DapperUnitOfWork));

        var repository = _serviceProvider.GetService<TRepository>();
        if (repository == null)
        {
            throw new InvalidOperationException(
                $"Repository of type {typeof(TRepository).Name} is not registered in the service container.");
        }

        return repository;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _transaction?.Dispose();
        _connection?.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
        }

        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }

        _disposed = true;
        await Task.CompletedTask;
    }
}