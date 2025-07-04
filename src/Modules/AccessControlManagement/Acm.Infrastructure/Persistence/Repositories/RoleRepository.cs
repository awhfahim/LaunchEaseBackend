using System.Data.Common;
using Acm.Application.Repositories;
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

    public async Task<IEnumerable<Role>> GetByTenantIdAsync(Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, tenant_id, name, description, created_at, updated_at
            FROM roles 
            WHERE tenant_id = @TenantId
            ORDER BY name";

        return await connection.QueryAsync<Role>(sql, new { TenantId = tenantId });
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