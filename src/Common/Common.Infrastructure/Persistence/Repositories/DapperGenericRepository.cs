using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;
using System.Text;
using Common.Application.Data;
using Common.Domain.Interfaces;
using Dapper;

namespace Common.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base generic repository implementation using Dapper
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TKey">Primary key type</typeparam>
public abstract class DapperGenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey>
    where TEntity : class
{
    protected readonly IDbConnectionFactory ConnectionFactory;
    protected readonly string TableName;
    protected readonly string PrimaryKeyColumn;

    protected DapperGenericRepository(IDbConnectionFactory connectionFactory)
    {
        ConnectionFactory = connectionFactory;

        // Get table name from TableAttribute or use class name
        var tableAttribute = typeof(TEntity).GetCustomAttribute<TableAttribute>();
        TableName = tableAttribute?.Name ?? typeof(TEntity).Name.ToLowerInvariant() + "s";

        // Get primary key column name (assumes 'Id' property by default)
        PrimaryKeyColumn = GetPrimaryKeyColumn();
    }

    protected virtual string GetPrimaryKeyColumn()
    {
        // You can override this in derived classes for custom primary key columns
        return "id";
    }

    protected abstract string GetInsertSql();
    protected abstract string GetUpdateSql();
    protected abstract string GetSelectSql();
    protected virtual string GetSelectByIdSql() => $"{GetSelectSql()} WHERE {PrimaryKeyColumn} = @Id";
    protected virtual string GetDeleteSql() => $"DELETE FROM {TableName} WHERE {PrimaryKeyColumn} = @Id";

    protected virtual string GetExistsSql() =>
        $"SELECT EXISTS(SELECT 1 FROM {TableName} WHERE {PrimaryKeyColumn} = @Id)";

    protected virtual string GetCountSql() => $"SELECT COUNT(*) FROM {TableName}";
    protected virtual string GetSelectAllSql() => GetSelectSql();

    #region Public Methods (without transaction)

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await GetByIdAsync(id, connection, null, cancellationToken);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(int? limit = null, int? offset = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await GetAllAsync(connection, null, limit, offset, cancellationToken);
    }

    public virtual async Task<TKey> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await InsertAsync(entity, connection, null, cancellationToken);
    }

    public virtual async Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await UpdateAsync(entity, connection, null, cancellationToken);
    }

    public virtual async Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await DeleteAsync(id, connection, null, cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await ExistsAsync(id, connection, null, cancellationToken);
    }

    public virtual async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await GetCountAsync(connection, null, cancellationToken);
    }

    #endregion

    #region Public Methods (with transaction support)

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, IDbConnection connection,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var sql = GetSelectByIdSql();
        return await connection.QueryFirstOrDefaultAsync<TEntity>(sql, new { Id = id }, transaction);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(IDbConnection connection,
        IDbTransaction? transaction = null, int? limit = null, int? offset = null,
        CancellationToken cancellationToken = default)
    {
        var sql = GetSelectAllSql();

        if (limit.HasValue)
        {
            sql += $" LIMIT {limit.Value}";
            if (offset.HasValue)
            {
                sql += $" OFFSET {offset.Value}";
            }
        }

        return await connection.QueryAsync<TEntity>(sql, transaction: transaction);
    }

    public virtual async Task<TKey> InsertAsync(TEntity entity, IDbConnection connection,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var sql = GetInsertSql();
        return await connection.QuerySingleAsync<TKey>(sql, entity, transaction);
    }

    public virtual async Task<bool> UpdateAsync(TEntity entity, IDbConnection connection,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var sql = GetUpdateSql();
        var rowsAffected = await connection.ExecuteAsync(sql, entity, transaction);
        return rowsAffected > 0;
    }

    public virtual async Task<bool> DeleteAsync(TKey id, IDbConnection connection, IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var sql = GetDeleteSql();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id }, transaction);
        return rowsAffected > 0;
    }

    public virtual async Task<bool> ExistsAsync(TKey id, IDbConnection? connection = null, IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        bool shouldDispose = false;
        if (connection == null)
        {
            connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);
            shouldDispose = true;
        }
        
        try
        {
            var sql = GetExistsSql();
            return await connection.QuerySingleAsync<bool>(sql, new { Id = id }, transaction);
        }
        finally
        {
            if (shouldDispose && connection is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (connection is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public virtual async Task<int> GetCountAsync(IDbConnection connection, IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var sql = GetCountSql();
        return await connection.QuerySingleAsync<int>(sql, transaction: transaction);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Build INSERT SQL with RETURNING clause for PostgreSQL
    /// </summary>
    protected virtual string BuildInsertSql(IEnumerable<string> columns, string returningColumn = "id")
    {
        var enumerable = columns as string[] ?? columns.ToArray();
        var columnList = string.Join(", ", enumerable);
        var parameterList = string.Join(", ", enumerable.Select(c => "@" + ToPascalCase(c)));

        return $"INSERT INTO {TableName} ({columnList}) VALUES ({parameterList}) RETURNING {returningColumn}";
    }

    /// <summary>
    /// Build UPDATE SQL
    /// </summary>
    protected virtual string BuildUpdateSql(IEnumerable<string> columns, string whereClause = null)
    {
        var setClause = string.Join(", ", columns.Select(c => $"{c} = @{ToPascalCase(c)}"));
        var sql = $"UPDATE {TableName} SET {setClause}";

        if (!string.IsNullOrEmpty(whereClause))
        {
            sql += $" WHERE {whereClause}";
        }
        else
        {
            sql += $" WHERE {PrimaryKeyColumn} = @{ToPascalCase(PrimaryKeyColumn)}";
        }

        return sql;
    }

    /// <summary>
    /// Build SELECT SQL
    /// </summary>
    protected virtual string BuildSelectSql(IEnumerable<string> columns)
    {
        var columnList = string.Join(", ", columns);
        return $"SELECT {columnList} FROM {TableName}";
    }

    /// <summary>
    /// Convert snake_case to PascalCase for parameter mapping
    /// </summary>
    private static string ToPascalCase(string snakeCase)
    {
        if (string.IsNullOrEmpty(snakeCase))
            return snakeCase;

        var parts = snakeCase.Split('_');
        var result = new StringBuilder();

        foreach (var part in parts)
        {
            if (part.Length <= 0) continue;
            result.Append(char.ToUpper(part[0]));
            if (part.Length > 1)
            {
                result.Append(part[1..].ToLower());
            }
        }

        return result.ToString();
    }

    #endregion
}