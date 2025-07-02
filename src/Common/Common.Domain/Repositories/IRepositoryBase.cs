using System.Linq.Expressions;
using Common.Domain.CoreProviders;
using Common.Domain.DataTransferObjects.Request;
using Common.Domain.DataTransferObjects.Response;

namespace Common.Domain.Repositories;

public interface IRepositoryBase<TEntity>

{
    Task CreateAsync(TEntity entity);
    Task CreateManyAsync(ICollection<TEntity> entity);

    Task<TEntity?> GetOneAsync(Expression<Func<TEntity, bool>> condition,
        CancellationToken cancellationToken = default);

    Task<TEntity?> GetOneAsync(Expression<Func<TEntity, bool>> condition, bool enableTracking,
        CancellationToken cancellationToken = default);

    Task<TResult?> GetOneAsync<TResult>(Expression<Func<TEntity, bool>> condition,
        Expression<Func<TEntity, TResult>> subsetSelector,
        bool enableTracking, CancellationToken cancellationToken = default);

    Task<ICollection<TEntity>> GetAllAsync<TSorter>(int page, int limit,
        (Expression<Func<TEntity, TSorter>> orderBy, bool desc) sorter,
        bool enableTracking,
        Expression<Func<TEntity, bool>>? condition = null,
        CancellationToken cancellationToken = default)
        where TSorter : IComparable<TSorter>;

    Task<ICollection<TEntity>> GetAllAsync<TSorter>(
        (Expression<Func<TEntity, TSorter>> orderBy, bool desc) sorter,
        bool enableTracking,
        Expression<Func<TEntity, bool>>? condition = null,
        CancellationToken cancellationToken = default)
        where TSorter : IComparable<TSorter>;

    Task<ICollection<TEntity>> GetAllAsync(
        bool enableTracking,
        Expression<Func<TEntity, bool>>? condition = null,
        CancellationToken cancellationToken = default);

    Task<ICollection<TResult>> GetAllAsync<TResult>(bool enableTracking,
        Expression<Func<TEntity, TResult>> subsetSelector,
        Expression<Func<TEntity, bool>>? condition = null,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(TEntity entityToUpdate);
    Task UpdateManyAsync(ICollection<TEntity> entitiesToUpdate);
    Task RemoveAsync(TEntity entityToDelete);
    Task RemoveManyAsync(ICollection<TEntity> entitiesToRemove);

    Task<int> RemoveManyWithoutTrackingAsync(Expression<Func<TEntity, bool>> condition);

    Task<int> UpdateManyWithoutTrackingAsync<TProperty>(
        Expression<Func<TEntity, bool>> condition,
        Func<TEntity, TProperty> propertyExpression,
        Func<TEntity, TProperty> valueExpression);

    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> condition,
        CancellationToken cancellationToken = default);

    Task<long> GetCountAsync(Expression<Func<TEntity, bool>>? condition = null,
        CancellationToken cancellationToken = default);

    Task ModifyEntityStateToAddedAsync<T>(T entity);
    Task ModifyEntityStateToAddedAsync(TEntity entity);
    Task TrackEntityAsync<T>(T entity) where T : class;
    Task TrackEntityAsync(TEntity entity);

    Task<PagedData<TEntity>> GetPagedDataForDynamicQueryAsync<TSorter>(DynamicQueryDto dto,
        (Expression<Func<TEntity, TSorter>> orderBy, bool desc) sorter,
        IReflectionCacheProvider reflectionCacheProvider,
        CancellationToken cancellationToken = default)
        where TSorter : IComparable<TSorter>;
}
