using Acm.Application.Repositories;
using Acm.Domain.Entities;
using Common.Application.Data;
using Dapper;
using System.Security.Claims;

namespace Acm.Infrastructure.Persistence.Repositories;

public class RoleClaimRepository : IRoleClaimRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RoleClaimRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<RoleClaim>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.OpenConnectionAsync();
        
        const string sql = @"
            SELECT id, role_id, claim_type, claim_value
            FROM role_claims 
            WHERE role_id = @RoleId";
        
        return await connection.QueryAsync<RoleClaim>(sql, new { RoleId = roleId });
    }

    public async Task<RoleClaim?> GetAsync(Guid roleId, string claimType, string claimValue, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.OpenConnectionAsync();
        
        const string sql = @"
            SELECT id, role_id, claim_type, claim_value
            FROM role_claims 
            WHERE role_id = @RoleId AND claim_type = @ClaimType AND claim_value = @ClaimValue";
        
        return await connection.QueryFirstOrDefaultAsync<RoleClaim>(sql, new { 
            RoleId = roleId, 
            ClaimType = claimType, 
            ClaimValue = claimValue 
        });
    }

    public async Task<Guid> CreateAsync(RoleClaim roleClaim, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.OpenConnectionAsync();
        
        const string sql = @"
            INSERT INTO role_claims (id, role_id, claim_type, claim_value)
            VALUES (@Id, @RoleId, @ClaimType, @ClaimValue)";
        
        await connection.ExecuteAsync(sql, roleClaim);
        return roleClaim.Id;
    }

    public async Task UpdateAsync(RoleClaim roleClaim, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.OpenConnectionAsync();
        
        const string sql = @"
            UPDATE role_claims 
            SET claim_type = @ClaimType, claim_value = @ClaimValue
            WHERE id = @Id";
        
        await connection.ExecuteAsync(sql, roleClaim);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.OpenConnectionAsync();
        
        const string sql = "DELETE FROM role_claims WHERE id = @Id";
        
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task DeleteAsync(Guid roleId, string claimType, string claimValue, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.OpenConnectionAsync();
        
        const string sql = @"
            DELETE FROM role_claims 
            WHERE role_id = @RoleId AND claim_type = @ClaimType AND claim_value = @ClaimValue";
        
        await connection.ExecuteAsync(sql, new { 
            RoleId = roleId, 
            ClaimType = claimType, 
            ClaimValue = claimValue 
        });
    }

    public async Task DeleteByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.OpenConnectionAsync();
        
        const string sql = "DELETE FROM role_claims WHERE role_id = @RoleId";
        
        await connection.ExecuteAsync(sql, new { RoleId = roleId });
    }

    public async Task<bool> ExistsAsync(Guid roleId, string claimType, string claimValue, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.OpenConnectionAsync();
        
        const string sql = @"
            SELECT 1 FROM role_claims 
            WHERE role_id = @RoleId AND claim_type = @ClaimType AND claim_value = @ClaimValue";
        
        return await connection.QueryFirstOrDefaultAsync<int?>(sql, new { 
            RoleId = roleId, 
            ClaimType = claimType, 
            ClaimValue = claimValue 
        }) is not null;
    }

    public async Task<IEnumerable<Claim>> GetClaimsForRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.OpenConnectionAsync();
        
        const string sql = @"
            SELECT claim_type, claim_value
            FROM role_claims 
            WHERE role_id = @RoleId";
        
        var results = await connection.QueryAsync<(string ClaimType, string ClaimValue)>(sql, new { RoleId = roleId });
        return results.Select(r => new Claim(r.ClaimType, r.ClaimValue));
    }

    public async Task<IEnumerable<Claim>> GetClaimsForUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.OpenConnectionAsync();
        
        const string sql = @"
            SELECT DISTINCT rc.claim_type, rc.claim_value
            FROM role_claims rc
            INNER JOIN user_roles ur ON rc.role_id = ur.role_id
            WHERE ur.user_id = @UserId";
        
        var results = await connection.QueryAsync<(string ClaimType, string ClaimValue)>(sql, new { UserId = userId });
        return results.Select(r => new Claim(r.ClaimType, r.ClaimValue));
    }
}
