using System.Data;
using Acm.Application.Repositories;
using Acm.Domain.Entities;
using Common.Application.Data;
using Dapper;

namespace Acm.Infrastructure.Persistence.Repositories;

public class UserTenantRepository : IUserTenantRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserTenantRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<UserTenant>> GetUserTenantsAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, user_id, tenant_id, is_active, joined_at, left_at, invited_by
            FROM user_tenants 
            WHERE user_id = @UserId AND is_active = true
            ORDER BY joined_at DESC";

        return await connection.QueryAsync<UserTenant>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<UserTenant>> GetTenantUsersAsync(Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, user_id, tenant_id, is_active, joined_at, left_at, invited_by
            FROM user_tenants 
            WHERE tenant_id = @TenantId AND is_active = true
            ORDER BY joined_at DESC";

        return await connection.QueryAsync<UserTenant>(sql, new { TenantId = tenantId });
    }

    public async Task<UserTenant?> GetUserTenantAsync(Guid userId, Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, user_id, tenant_id, is_active, joined_at, left_at, invited_by
            FROM user_tenants 
            WHERE user_id = @UserId AND tenant_id = @TenantId";

        return await connection.QueryFirstOrDefaultAsync<UserTenant>(sql, new { UserId = userId, TenantId = tenantId });
    }

    public async Task<Guid> AddUserToTenantAsync(UserTenant userTenant, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            INSERT INTO user_tenants (id, user_id, tenant_id, is_active, joined_at, left_at, invited_by)
            VALUES (@Id, @UserId, @TenantId, @IsActive, @JoinedAt, @LeftAt, @InvitedBy)";

        await connection.ExecuteAsync(sql, userTenant);
        return userTenant.Id;
    }

    public async Task<Guid> AddUserToTenantAsync(UserTenant userTenant, IDbConnection connection,
        IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO user_tenants (id, user_id, tenant_id, is_active, joined_at, left_at, invited_by)
            VALUES (@Id, @UserId, @TenantId, @IsActive, @JoinedAt, @LeftAt, @InvitedBy)";

        await connection.ExecuteAsync(sql, userTenant, transaction);
        return userTenant.Id;
    }
    
    private async Task RemoveUserFromTenantAsyncInternal(
        Guid userId,
        Guid tenantId,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        bool shouldDispose = false;
        if (connection == null)
        {
            connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
            shouldDispose = true;
        }

        try
        {
            const string sql = @"
                UPDATE user_tenants 
                SET is_active = false, left_at = current_timestamp
                WHERE user_id = @UserId AND tenant_id = @TenantId";

            await connection.ExecuteAsync(sql, new { UserId = userId, TenantId = tenantId }, transaction);
        }
        finally
        {
            if (shouldDispose && connection is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
        }
    }

    public Task RemoveUserFromTenantAsync(Guid userId, Guid tenantId,
        CancellationToken cancellationToken = default)
        => RemoveUserFromTenantAsyncInternal(userId, tenantId, null, null, cancellationToken);
    
    public Task RemoveUserFromTenantAsync(Guid userId, Guid tenantId, IDbConnection connection,
        IDbTransaction transaction, CancellationToken cancellationToken = default)
        => RemoveUserFromTenantAsyncInternal(userId, tenantId, connection, transaction, cancellationToken);

    public async Task<bool> IsUserMemberOfTenantAsync(Guid userId, Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT 1 
            FROM user_tenants 
            WHERE user_id = @UserId AND tenant_id = @TenantId AND is_active = true";

        return await connection.QueryFirstOrDefaultAsync<int?>(sql, new { UserId = userId, TenantId = tenantId }) is not
            null;
    }

    public async Task<IEnumerable<Tenant>> GetUserAccessibleTenantsAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT t.id, t.name, t.slug, t.logo_url, t.contact_email, t.created_at, t.updated_at
            FROM tenants t
            INNER JOIN user_tenants ut ON t.id = ut.tenant_id
            WHERE ut.user_id = @UserId AND ut.is_active = true
            ORDER BY ut.joined_at DESC";

        return await connection.QueryAsync<Tenant>(sql, new { UserId = userId });
    }
}