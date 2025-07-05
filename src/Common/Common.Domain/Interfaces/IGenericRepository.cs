using System.Data;

namespace Common.Domain.Interfaces;

/// <summary>
/// Generic repository interface for basic CRUD operations using Dapper
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TKey">Primary key type</typeparam>
public interface IGenericRepository<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// Get entity by ID
    /// </summary>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get entity by ID with transaction support
    /// </summary>
    Task<TEntity?> GetByIdAsync(TKey id, IDbConnection connection, IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all entities with optional pagination
    /// </summary>
    Task<IEnumerable<TEntity>> GetAllAsync(int? limit = null, int? offset = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all entities with transaction support
    /// </summary>
    Task<IEnumerable<TEntity>> GetAllAsync(IDbConnection connection, IDbTransaction? transaction = null,
        int? limit = null, int? offset = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Insert new entity
    /// </summary>
    Task<TKey> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Insert new entity with transaction support
    /// </summary>
    Task<TKey> InsertAsync(TEntity entity, IDbConnection connection, IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing entity
    /// </summary>
    Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing entity with transaction support
    /// </summary>
    Task<bool> UpdateAsync(TEntity entity, IDbConnection connection, IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete entity by ID
    /// </summary>
    Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete entity by ID with transaction support
    /// </summary>
    Task<bool> DeleteAsync(TKey id, IDbConnection connection, IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if entity exists by ID
    /// </summary>
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if entity exists by ID with transaction support
    /// </summary>
    Task<bool> ExistsAsync(TKey id, IDbConnection connection, IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get count of entities
    /// </summary>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get count of entities with transaction support
    /// </summary>
    Task<int> GetCountAsync(IDbConnection connection, IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);
}