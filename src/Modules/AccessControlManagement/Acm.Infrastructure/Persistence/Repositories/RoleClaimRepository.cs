using System.Data.Common;
using Acm.Application.Repositories;
using Acm.Domain.Entities;
using Common.Application.Data;
using Dapper;
using System.Security.Claims;

namespace Acm.Infrastructure.Persistence.Repositories;

public class RoleClaimRepository : IRoleClaimRepository
{
    private const string DeleteByRoleIdSql = "DELETE FROM role_claims WHERE role_id = @RoleId";

    private readonly IDbConnectionFactory _connectionFactory;

    public RoleClaimRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<RoleClaim>> GetByRoleIdAsync(Guid roleId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT r.id, role_id, claim_type, claim_value
            FROM role_claims r
            inner join master_claims mc on mc.id = r.master_claim_id
            WHERE role_id = @RoleId";

        return await connection.QueryAsync<RoleClaim>(sql, new { RoleId = roleId });
    }

    public async Task<RoleClaim?> GetAsync(Guid roleId, string claimType, string claimValue,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT r.id, role_id, claim_type, claim_value
            FROM role_claims r
                     inner join master_claims mc on mc.id = r.master_claim_id
            WHERE role_id = @RoleId AND claim_type = @ClaimType AND claim_value = @ClaimValue";

        return await connection.QueryFirstOrDefaultAsync<RoleClaim>(sql, new
        {
            RoleId = roleId,
            ClaimType = claimType,
            ClaimValue = claimValue
        });
    }

    public async Task<Guid> CreateAsync(RoleClaim roleClaim, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            INSERT INTO role_claims (id, role_id, claim_type, claim_value)
            VALUES (@Id, @RoleId, @ClaimType, @ClaimValue)";

        await connection.ExecuteAsync(sql, roleClaim);
        return roleClaim.Id;
    }

    public async Task UpdateAsync(RoleClaim roleClaim, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            UPDATE role_claims 
            SET claim_type = @ClaimType, claim_value = @ClaimValue
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, roleClaim);
    }

    public async Task DeleteAsync(Guid id, DbConnection connection, DbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM role_claims WHERE role_id = @RoleId";

        await connection.ExecuteAsync(sql, new { Id = id }, transaction);
    }

    public async Task DeleteAsync(Guid roleId, string claimType, string claimValue,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            DELETE FROM role_claims 
            INNER JOIN master_claims mc on mc.id = rc.master_claim_id
            WHERE role_id = @RoleId AND claim_type = @ClaimType AND claim_value = @ClaimValue";

        await connection.ExecuteAsync(sql, new
        {
            RoleId = roleId,
            ClaimType = claimType,
            ClaimValue = claimValue
        });
    }

    public async Task DeleteByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(DeleteByRoleIdSql, new { RoleId = roleId });
    }

    public async Task DeleteByRoleIdAsync(Guid roleId, DbConnection connection, DbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await connection.ExecuteAsync(DeleteByRoleIdSql, new { RoleId = roleId }, transaction);
    }

    public Task AddRangeAsync(List<RoleClaim> claims, DbConnection connection, DbTransaction transaction)
    {
        const string sql = @"
            INSERT INTO role_claims (id, role_id, master_claim_id)
            VALUES (@Id, @RoleId, @MasterClaimId)";

        return connection.ExecuteAsync(sql, claims, transaction);
    }

    public async Task<bool> ExistsAsync(Guid roleId, string claimType, string claimValue,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT 1 FROM role_claims 
            INNER JOIN master_claims mc on mc.id = rc.master_claim_id
            WHERE role_id = @RoleId AND claim_type = @ClaimType AND claim_value = @ClaimValue";

        return await connection.QueryFirstOrDefaultAsync<int?>(sql, new
        {
            RoleId = roleId,
            ClaimType = claimType,
            ClaimValue = claimValue
        }) is not null;
    }

    public async Task<IEnumerable<Claim>> GetClaimsForRoleAsync(Guid roleId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT claim_type, claim_value
            FROM role_claims rc
            INNER JOIN master_claims mc on mc.id = rc.master_claim_id
            WHERE role_id = @RoleId";

        var results = await connection.QueryAsync<(string ClaimType, string ClaimValue)>(sql, new { RoleId = roleId });
        return results.Select(r => new Claim(r.ClaimType, r.ClaimValue));
    }

    public async Task<IEnumerable<Claim>> GetClaimsForUserRolesAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT DISTINCT mc.claim_type, mc.claim_value
            FROM role_claims rc
            INNER JOIN master_claims mc on mc.id = rc.master_claim_id
            INNER JOIN user_roles ur ON rc.role_id = ur.role_id
            WHERE ur.user_id = @UserId";

        var results = await connection.QueryAsync<(string ClaimType, string ClaimValue)>(sql, new { UserId = userId });
        return results.Select(r => new Claim(r.ClaimType, r.ClaimValue));
    }

    public async Task<IEnumerable<Claim>> GetClaimsForUserRolesAsync(Guid userId, Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT DISTINCT mc.claim_type, mc.claim_value
            FROM role_claims rc
                INNER JOIN master_claims mc on mc.id = rc.master_claim_id
                INNER JOIN user_roles ur ON rc.role_id = ur.role_id
            WHERE ur.user_id = @UserId AND ur.tenant_id = @TenantId";

        var results =
            await connection.QueryAsync<(string ClaimType, string ClaimValue)>(sql,
                new { UserId = userId, TenantId = tenantId });
        return results.Select(r => new Claim(r.ClaimType, r.ClaimValue));
    }
}