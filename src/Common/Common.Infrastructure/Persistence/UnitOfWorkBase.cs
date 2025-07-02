using System.Data.Common;
using Common.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Common.Infrastructure.Persistence;

public abstract class UnitOfWorkBase : IUnitOfWork
{
    private readonly DbContext _dbContext;
    private IDbContextTransaction? _currentTransaction;

    protected UnitOfWorkBase(DbContext dbContext) => _dbContext = dbContext;

    public virtual void Dispose() => _dbContext.Dispose();

    public virtual ValueTask DisposeAsync() => _dbContext.DisposeAsync();

    public virtual void Save() => _dbContext.SaveChanges();

    public virtual async Task SaveAsync() => await _dbContext.SaveChangesAsync();

    public async Task<DbTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        var trx = await _dbContext.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        return trx.GetDbTransaction();
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("Transaction already in progress");
        }

        _currentTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation();
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }
}