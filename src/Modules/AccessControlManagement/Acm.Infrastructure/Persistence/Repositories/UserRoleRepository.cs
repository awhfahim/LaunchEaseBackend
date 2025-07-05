using System.Data;
using System.Data.Common;
using Acm.Application.Repositories;
using Acm.Domain.Entities;
using Common.Application.Data;
using Dapper;

namespace Acm.Infrastructure.Persistence.Repositories;

public class UserRoleRepository : IUserRoleRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRoleRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<UserRole>> GetByUserIdAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, user_id, role_id, tenant_id
            FROM user_roles 
            WHERE user_id = @UserId";

        return await connection.QueryAsync<UserRole>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<UserRole>> GetByRoleIdAsync(Guid roleId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, user_id, role_id, tenant_id
            FROM user_roles 
            WHERE role_id = @RoleId";

        return await connection.QueryAsync<UserRole>(sql, new { RoleId = roleId });
    }

    public async Task<UserRole?> GetAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, user_id, role_id
            FROM user_roles 
            WHERE user_id = @UserId AND role_id = @RoleId";

        return await connection.QueryFirstOrDefaultAsync<UserRole>(sql, new { UserId = userId, RoleId = roleId });
    }

    private async Task<Guid> CreateAsyncInternal(UserRole userRole,
        IDbConnection? connection = null, IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        bool shouldDispose = false;
        if (connection == null)
        {
            connection = await _connectionFactory.OpenConnectionAsync();
            shouldDispose = true;
        }

        try
        {
            const string sql = @"
                INSERT INTO user_roles (id, user_id, role_id, tenant_id)
                VALUES (@Id, @UserId, @RoleId, @TenantId)";
            
            await connection.ExecuteAsync(sql, userRole, transaction);
            return userRole.Id;
        }
        finally
        {
            if (shouldDispose && connection is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else if (connection is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public Task<Guid> CreateAsync(UserRole userRole,
        CancellationToken cancellationToken = default)
        => CreateAsyncInternal(userRole, null, null, cancellationToken);

    public Task<Guid> CreateAsync(UserRole userRole, IDbConnection connection, IDbTransaction transaction)
        => CreateAsyncInternal(userRole, connection, transaction);
    
    public async Task CreateRangeAsync(IEnumerable<UserRole> userRoles,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            INSERT INTO user_roles (id, user_id, role_id, tenant_id)
            VALUES (@Id, @UserId, @RoleId, @TenantId)";

        await connection.ExecuteAsync(sql, userRoles);
    }

    public async Task DeleteAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = "DELETE FROM user_roles WHERE user_id = @UserId AND role_id = @RoleId";

        await connection.ExecuteAsync(sql, new { UserId = userId, RoleId = roleId });
    }

    public async Task DeleteRangeAsync(Guid userId, Guid tenantId, ICollection<Guid> roleIds)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            DELETE FROM user_roles 
            WHERE user_id = @UserId AND tenant_id = @TenantId AND role_id IN @RoleIds";

        await connection.ExecuteAsync(sql, new { UserId = userId, TenantId = tenantId, RoleIds = roleIds });
    }

    private async Task DeleteByUserIdInternalAsync(
        Guid userId,
        Guid tenantId,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        bool shouldDispose = false;
        if (connection == null)
        {
            connection = await _connectionFactory.OpenConnectionAsync();
            shouldDispose = true;
        }

        try
        {
            const string sql =
                "DELETE FROM user_roles WHERE user_id = @UserId and tenant_id = @TenantId";

            await connection.ExecuteAsync(sql, new { UserId = userId, TenantId = tenantId },
                transaction);
        }
        finally
        {
            if (shouldDispose && connection is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else if (connection is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public Task DeleteByUserIdAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
        => DeleteByUserIdInternalAsync(userId, tenantId, null, null, cancellationToken);

    public Task DeleteByUserIdAsync(Guid userId, Guid tenantId, IDbConnection connection, IDbTransaction transaction,
        CancellationToken cancellationToken = default)
        => DeleteByUserIdInternalAsync(userId, tenantId, connection, transaction, cancellationToken);

    private async Task DeleteByRoleIdInternalAsync(
        Guid roleId,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        bool shouldDispose = false;
        if (connection == null)
        {
            connection = await _connectionFactory.OpenConnectionAsync();
            shouldDispose = true;
        }

        try
        {
            const string sql = "DELETE FROM user_roles WHERE role_id = @RoleId";
            await connection.ExecuteAsync(sql, new { RoleId = roleId }, transaction);
        }
        finally
        {
            if (shouldDispose && connection is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else if (connection is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public Task DeleteByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
        => DeleteByRoleIdInternalAsync(roleId, null, null, cancellationToken);

    public Task DeleteByRoleIdAsync(Guid roleId, DbConnection connection, DbTransaction transaction,
        CancellationToken cancellationToken = default)
        => DeleteByRoleIdInternalAsync(roleId, connection, transaction, cancellationToken);

    public async Task<bool> ExistsAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = "SELECT 1 FROM user_roles WHERE user_id = @UserId AND role_id = @RoleId";

        return await connection.QueryFirstOrDefaultAsync<int?>(sql, new { UserId = userId, RoleId = roleId }) is not
            null;
    }

    public async Task<IEnumerable<string>> GetRoleNamesForUserAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT r.name
            FROM user_roles ur
            INNER JOIN roles r ON ur.role_id = r.id
            WHERE ur.user_id = @UserId";

        return await connection.QueryAsync<string>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<UserRole>> GetByUserIdAsync(Guid userId, Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, user_id, role_id, tenant_id
            FROM user_roles 
            WHERE user_id = @UserId AND tenant_id = @TenantId";

        return await connection.QueryAsync<UserRole>(sql, new { UserId = userId, TenantId = tenantId });
    }

    public async Task<UserRole?> GetAsync(Guid userId, Guid roleId, Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, user_id, role_id, tenant_id
            FROM user_roles 
            WHERE user_id = @UserId AND role_id = @RoleId AND tenant_id = @TenantId";

        return await connection.QueryFirstOrDefaultAsync<UserRole>(sql,
            new { UserId = userId, RoleId = roleId, TenantId = tenantId });
    }

    public async Task DeleteAsync(Guid userId, Guid roleId, Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql =
            "DELETE FROM user_roles WHERE user_id = @UserId AND role_id = @RoleId AND tenant_id = @TenantId";

        await connection.ExecuteAsync(sql, new { UserId = userId, RoleId = roleId, TenantId = tenantId });
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid roleId, Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql =
            "SELECT 1 FROM user_roles WHERE user_id = @UserId AND role_id = @RoleId AND tenant_id = @TenantId";

        return await connection.QueryFirstOrDefaultAsync<int?>(sql,
            new { UserId = userId, RoleId = roleId, TenantId = tenantId }) is not null;
    }

    public async Task<IEnumerable<string>> GetRoleNamesForUserAsync(Guid userId, Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT r.name
            FROM roles r
            INNER JOIN user_roles ur ON r.id = ur.role_id
            WHERE ur.user_id = @UserId AND ur.tenant_id = @TenantId";

        return await connection.QueryAsync<string>(sql, new { UserId = userId, TenantId = tenantId });
    }
}