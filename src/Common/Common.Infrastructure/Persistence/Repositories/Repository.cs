using System.Linq.Expressions;
using Common.Domain.CoreProviders;
using Common.Domain.DataTransferObjects.Request;
using Common.Domain.DataTransferObjects.Response;
using Common.Domain.Repositories;
using Common.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Common.Infrastructure.Persistence.Repositories;

public abstract class Repository<TEntity> : IRepositoryBase<TEntity>
    where TEntity : class
{
    private readonly DbContext _databaseContext;
    protected readonly DbSet<TEntity> EntityDbSet;

    protected Repository(DbContext context)
    {
        _databaseContext = context;
        EntityDbSet = _databaseContext.Set<TEntity>();
    }

    public virtual async Task CreateAsync(TEntity entity)
    {
        await EntityDbSet.AddAsync(entity).ConfigureAwait(false);
    }

    public async Task CreateManyAsync(ICollection<TEntity> entity)
    {
        await EntityDbSet.AddRangeAsync(entity).ConfigureAwait(false);
    }

    public virtual Task RemoveAsync(TEntity entityToDelete)
    {
        return Task.Run(() =>
        {
            if (_databaseContext.Entry(entityToDelete).State is EntityState.Detached)
            {
                EntityDbSet.Attach(entityToDelete);
            }

            EntityDbSet.Remove(entityToDelete);
        });
    }

    public Task<TEntity?> GetOneAsync(Expression<Func<TEntity, bool>> condition,
        CancellationToken cancellationToken = default)
    {
        return EntityDbSet.Where(condition).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
    }

    public Task<TEntity?> GetOneAsync(Expression<Func<TEntity, bool>> condition, bool enableTracking,
        CancellationToken cancellationToken = default)
    {
        var query = EntityDbSet.Where(condition);
        if (enableTracking is false)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TResult?> GetOneAsync<TResult>(Expression<Func<TEntity, bool>> condition,
        Expression<Func<TEntity, TResult>> subsetSelector, bool enableTracking,
        CancellationToken cancellationToken = default)
    {
        var query = EntityDbSet.Where(condition);
        if (enableTracking is false)
        {
            query = query.AsNoTracking();
        }

        return await query.Select(subsetSelector)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ICollection<TEntity>> GetAllAsync<TSorter>(int page, int limit,
        (Expression<Func<TEntity, TSorter>> orderBy, bool desc) sorter, bool enableTracking,
        Expression<Func<TEntity, bool>>? condition = null,
        CancellationToken cancellationToken = default) where TSorter : IComparable<TSorter>
    {
        var query = EntityDbSet.AsQueryable();
        if (condition is not null)
        {
            query = EntityDbSet.Where(condition);
        }

        query = sorter.desc
            ? query.OrderByDescending(sorter.orderBy)
            : query.OrderBy(sorter.orderBy);

        if (enableTracking is false)
        {
            query = query.AsNoTracking();
        }

        var safePageLimit = AvoidNegativeOrZeroPagination(page, limit);

        return await query
            .Skip((safePageLimit.page - 1) * safePageLimit.limit).Take(safePageLimit.limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<ICollection<TEntity>> GetAllAsync<TSorter>(
        (Expression<Func<TEntity, TSorter>> orderBy, bool desc) sorter, bool enableTracking,
        Expression<Func<TEntity, bool>>? condition = null,
        CancellationToken cancellationToken = default) where TSorter : IComparable<TSorter>
    {
        var query = EntityDbSet.AsQueryable();
        if (condition is not null)
        {
            query = EntityDbSet.Where(condition);
        }

        query = sorter.desc
            ? query.OrderByDescending(sorter.orderBy)
            : query.OrderBy(sorter.orderBy);

        if (enableTracking is false)
        {
            query = query.AsNoTracking();
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<ICollection<TEntity>> GetAllAsync(bool enableTracking,
        Expression<Func<TEntity, bool>>? condition = null,
        CancellationToken cancellationToken = default)
    {
        var query = EntityDbSet.AsQueryable();
        if (condition is not null)
        {
            query = EntityDbSet.Where(condition);
        }

        if (enableTracking is false)
        {
            query = query.AsNoTracking();
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<ICollection<TResult>> GetAllAsync<TResult>(bool enableTracking,
        Expression<Func<TEntity, TResult>> subsetSelector,
        Expression<Func<TEntity, bool>>? condition = null,
        CancellationToken cancellationToken = default)
    {
        var query = EntityDbSet.AsQueryable();
        if (condition is not null)
        {
            query = EntityDbSet.Where(condition);
        }

        if (enableTracking is false)
        {
            query = query.AsNoTracking();
        }

        return await query.Select(subsetSelector)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> condition,
        CancellationToken cancellationToken = default)
    {
        return EntityDbSet.AnyAsync(condition, cancellationToken);
    }

    public virtual Task UpdateManyAsync(ICollection<TEntity> entitiesToUpdate)
    {
        return Task.Run(() => EntityDbSet.UpdateRange(entitiesToUpdate));
    }

    public virtual Task RemoveManyAsync(ICollection<TEntity> entitiesToRemove)
    {
        return Task.Run(() => EntityDbSet.RemoveRange(entitiesToRemove));
    }

    public virtual Task<int> UpdateManyWithoutTrackingAsync<TProperty>(
        Expression<Func<TEntity, bool>> condition,
        Func<TEntity, TProperty> propertyExpression,
        Func<TEntity, TProperty> valueExpression)
    {
        return EntityDbSet
            .Where(condition)
            .ExecuteUpdateAsync(updates => updates.SetProperty(propertyExpression, valueExpression));
    }


    public virtual Task<int> RemoveManyWithoutTrackingAsync(Expression<Func<TEntity, bool>> condition)
    {
        return EntityDbSet.Where(condition).ExecuteDeleteAsync();
    }

    public virtual Task UpdateAsync(TEntity entityToUpdate)
    {
        return Task.Run(() =>
        {
            EntityDbSet.Attach(entityToUpdate);
            _databaseContext.Entry(entityToUpdate).State = EntityState.Modified;
        });
    }

    public virtual Task<long> GetCountAsync(Expression<Func<TEntity, bool>>? condition = null,
        CancellationToken cancellationToken = default)
    {
        return condition is not null
            ? EntityDbSet.LongCountAsync(condition, cancellationToken)
            : EntityDbSet.LongCountAsync(cancellationToken);
    }

    public Task TrackEntityAsync<T>(T entity) where T : class
    {
        return Task.Run(() => _databaseContext.Set<T>().Attach(entity));
    }

    public Task TrackEntityAsync(TEntity entity)
    {
        return Task.Run(() => _databaseContext.Set<TEntity>().Attach(entity));
    }

    public virtual async Task<PagedData<TEntity>> GetPagedDataForDynamicQueryAsync<TSorter>(
        DynamicQueryDto dto,
        (Expression<Func<TEntity, TSorter>> orderBy, bool desc) sorter,
        IReflectionCacheProvider reflectionCacheProvider,
        CancellationToken cancellationToken = default) where TSorter : IComparable<TSorter>
    {
        var data = EntityDbSet.WhereDynamic(dto.Filters, reflectionCacheProvider);

        data = data.OrderByDynamic(dto.Sorters, sorter, reflectionCacheProvider);

        var count = await data.LongCountAsync(cancellationToken);
        data = data.PaginateQueryable(dto.Page, dto.Size);
        return new PagedData<TEntity>(await data.ToListAsync(cancellationToken), count);
    }

    public Task ModifyEntityStateToAddedAsync(TEntity entity)
    {
        return Task.Run(() =>
        {
            if (_databaseContext.Entry(entity).State is not EntityState.Added)
            {
                _databaseContext.Entry(entity).State = EntityState.Added;
            }
        });
    }

    public Task ModifyEntityStateToAddedAsync<T>(T entity)
    {
        return Task.Run(() =>
        {
            if (entity is null)
            {
                return;
            }

            if (_databaseContext.Entry(entity).State is not EntityState.Added)
            {
                _databaseContext.Entry(entity).State = EntityState.Added;
            }
        });
    }

    private static (int page, int limit) AvoidNegativeOrZeroPagination(int page, int limit)
    {
        var pagination = (page, limit);
        if (page <= 0)
        {
            pagination.page = 1;
        }

        if (limit <= 0)
        {
            pagination.limit = 1;
        }

        return pagination;
    }
}
