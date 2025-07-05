using System.Data;
using Acm.Application.Repositories;
using Acm.Domain.Entities;
using Common.Application.Data;
using Dapper;
using System.Security.Claims;

namespace Acm.Infrastructure.Persistence.Repositories;

public class UserClaimRepository : IUserClaimRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    public UserClaimRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<UserClaim>> GetByUserIdAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, user_id, tenant_id, claim_type, claim_value
            FROM user_claims
            INNER JOIN master_claims mc on mc.id = user_claims.master_claim_id 
            WHERE user_id = @UserId";

        return await connection.QueryAsync<UserClaim>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<UserClaim>> GetByUserIdAsync(Guid userId, Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, user_id, tenant_id, claim_type, claim_value
            FROM user_claims
            INNER JOIN master_claims mc on mc.id = user_claims.master_claim_id
            WHERE user_id = @UserId AND tenant_id = @TenantId";

        return await connection.QueryAsync<UserClaim>(sql, new { UserId = userId, TenantId = tenantId });
    }

    public async Task<UserClaim?> GetAsync(Guid userId, string claimType, string claimValue,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT u.id, user_id, claim_type, claim_value
            FROM user_claims u
                     INNER JOIN master_claims mc on mc.id = u.master_claim_id
            WHERE user_id = @UserId AND claim_type = @ClaimType AND claim_value = @ClaimValue";

        return await connection.QueryFirstOrDefaultAsync<UserClaim>(sql, new
        {
            UserId = userId,
            ClaimType = claimType,
            ClaimValue = claimValue
        });
    }

    public async Task<UserClaim?> GetAsync(Guid userId, string claimType, string claimValue, Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, user_id, tenant_id, claim_type, claim_value
            FROM user_claims 
            WHERE user_id = @UserId AND claim_type = @ClaimType AND claim_value = @ClaimValue AND tenant_id = @TenantId";

        return await connection.QueryFirstOrDefaultAsync<UserClaim>(sql, new
        {
            UserId = userId,
            ClaimType = claimType,
            ClaimValue = claimValue,
            TenantId = tenantId
        });
    }

    public async Task<Guid> CreateAsync(UserClaim userClaim, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            INSERT INTO user_claims (id, user_id, tenant_id, claim_type, claim_value)
            VALUES (@Id, @UserId, @TenantId, @ClaimType, @ClaimValue)";

        await connection.ExecuteAsync(sql, userClaim);
        return userClaim.Id;
    }

    public async Task UpdateAsync(UserClaim userClaim, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            UPDATE user_claims 
            SET tenant_id = @TenantId, claim_type = @ClaimType, claim_value = @ClaimValue
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, userClaim);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = "DELETE FROM user_claims WHERE id = @Id";

        await connection.ExecuteAsync(sql, new { Id = id });
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
            connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
            shouldDispose = true;
        }

        try
        {
            const string sql = "DELETE FROM user_claims WHERE user_id = @UserId and tenant_id = @TenantId";
            await connection.ExecuteAsync(sql, new { UserId = userId, TenantId = tenantId }, transaction);
        }
        finally
        {
            if (shouldDispose && connection is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
        }
    }

    public Task DeleteByUserIdAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
        => DeleteByUserIdInternalAsync(userId, tenantId, null, null, cancellationToken);

    public Task DeleteByUserIdAsync(Guid userId, Guid tenantId, IDbConnection connection,
        IDbTransaction transaction, CancellationToken cancellationToken = default)
        => DeleteByUserIdInternalAsync(userId, tenantId, connection, transaction, cancellationToken);

    public async Task<bool> ExistsAsync(Guid userId, string claimType, string claimValue,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT 1 FROM user_claims 
            WHERE user_id = @UserId AND claim_type = @ClaimType AND claim_value = @ClaimValue";

        return await connection.QueryFirstOrDefaultAsync<int?>(sql, new
        {
            UserId = userId,
            ClaimType = claimType,
            ClaimValue = claimValue
        }) is not null;
    }

    public async Task<bool> ExistsAsync(Guid userId, string claimType, string claimValue, Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql =
            "SELECT 1 FROM user_claims WHERE user_id = @UserId AND claim_type = @ClaimType AND claim_value = @ClaimValue AND tenant_id = @TenantId";

        return await connection.QueryFirstOrDefaultAsync<int?>(sql, new
        {
            UserId = userId,
            ClaimType = claimType,
            ClaimValue = claimValue,
            TenantId = tenantId
        }) is not null;
    }

    public async Task<IEnumerable<Claim>> GetClaimsForUserAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT claim_type, claim_value
            FROM user_claims
            inner join master_claims mc on mc.id = user_claims.master_claim_id
            WHERE user_id = @UserId";

        var results = await connection.QueryAsync<(string ClaimType, string ClaimValue)>(sql, new { UserId = userId });
        return results.Select(r => new Claim(r.ClaimType, r.ClaimValue));
    }

    public async Task<IEnumerable<Claim>> GetClaimsForUserAsync(Guid userId, Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT claim_type as Type, claim_value as Value
            FROM user_claims
            inner join master_claims mc on mc.id = user_claims.master_claim_id
            WHERE user_id = @UserId AND tenant_id = @TenantId";

        var claims = await connection.QueryAsync<dynamic>(sql, new { UserId = userId, TenantId = tenantId });
        return claims.Select(c => new Claim(c.Type, c.Value));
    }
}