using System.Data.Common;
using System.Text.Json;
using Acm.Application.Repositories;
using Acm.Domain.DTOs;
using Acm.Domain.Entities;
using Common.Application.Data;
using Dapper;

namespace Acm.Infrastructure.Persistence.Repositories;

public class RoleRepository : IRoleRepository
{
    private const string DeleteRoleSql = "DELETE FROM roles WHERE id = @Id";

    private readonly IDbConnectionFactory _connectionFactory;

    public RoleRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, tenant_id, name, description, created_at, updated_at
            FROM roles 
            WHERE id = @Id";

        return await connection.QueryFirstOrDefaultAsync<Role>(sql, new { Id = id });
    }

    public async Task<Role?> GetByNameAsync(string name, Guid tenantId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, tenant_id, name, description, created_at, updated_at
            FROM roles 
            WHERE name = @Name AND tenant_id = @TenantId";

        return await connection.QueryFirstOrDefaultAsync<Role>(sql, new { Name = name, TenantId = tenantId });
    }

    public async Task<IEnumerable<RoleResponse>> GetByTenantIdAsync(Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT r.id, tenant_id, name, r.description, r.created_at, r.updated_at,
                   json_agg(json_build_object('claimId', rc.master_claim_id, 'claim', mc.claim_value)) AS Claims
            FROM roles r
            LEFT JOIN role_claims rc ON r.id = rc.role_id
            LEFT JOIN master_claims mc ON rc.master_claim_id = mc.id
            WHERE tenant_id = @TenantId
            GROUP BY r.id";

        var result = await connection.QueryAsync<RoleResponse>(sql, new { TenantId = tenantId });

        var byTenantIdAsync = result as RoleResponse[] ?? result.ToArray();
        foreach (var roleResponse in byTenantIdAsync)
        {
            if (!string.IsNullOrEmpty(roleResponse.Claims))
            {
                roleResponse.Permissions =
                    JsonSerializer.Deserialize<List<RoleClaimResponse>>(roleResponse.Claims, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? 
                    [];

                roleResponse.Claims = string.Empty;
            }
        }
        
        return byTenantIdAsync;
    }

    public async Task<Guid> CreateAsync(Role role,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            INSERT INTO roles (id, tenant_id, name, description, created_at, updated_at)
            VALUES (@Id, @TenantId, @Name, @Description, @CreatedAt, @UpdatedAt)";

        await connection.ExecuteAsync(sql, role);
        return role.Id;
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            UPDATE roles 
            SET name = @Name, description = @Description, updated_at = @UpdatedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, role);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();
        await connection.ExecuteAsync(DeleteRoleSql, new { Id = id });
    }

    public async Task DeleteAsync(Guid id, DbConnection connection, DbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await connection.ExecuteAsync(DeleteRoleSql, new { Id = id }, transaction);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = "SELECT 1 FROM roles WHERE id = @Id";

        return await connection.QueryFirstOrDefaultAsync<int?>(sql, new { Id = id }) is not null;
    }

    public async Task<bool> NameExistsAsync(string name, Guid tenantId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = "SELECT 1 FROM roles WHERE name = @Name AND tenant_id = @TenantId";

        return await connection.QueryFirstOrDefaultAsync<int?>(sql, new { Name = name, TenantId = tenantId }) is not
            null;
    }
}