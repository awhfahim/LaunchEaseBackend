using System.Data.Common;
using Acm.Application.Repositories;
using Acm.Domain.Entities;
using Common.Application.Data;
using Dapper;

namespace Acm.Infrastructure.Persistence.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TenantRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, name, slug, logo_url, contact_email, created_at, updated_at
            FROM tenants 
            WHERE id = @Id";

        return await connection.QueryFirstOrDefaultAsync<Tenant>(sql, new { Id = id });
    }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, name, slug, logo_url, contact_email, created_at, updated_at
            FROM tenants 
            WHERE slug = @Slug";

        return await connection.QueryFirstOrDefaultAsync<Tenant>(sql, new { Slug = slug });
    }

    public async Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, name, slug, logo_url, contact_email, created_at, updated_at
            FROM tenants 
            ORDER BY created_at DESC";

        return await connection.QueryAsync<Tenant>(sql);
    }

    public async Task<Guid> CreateAsync(Tenant tenant, DbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();
        
        const string sql = @"
            INSERT INTO tenants (id, name, slug, logo_url, contact_email, created_at, updated_at)
            VALUES (@Id, @Name, @Slug, @LogoUrl, @ContactEmail, @CreatedAt, @UpdatedAt)";

        await connection.ExecuteAsync(sql, tenant, transaction: transaction);
        return tenant.Id;
    }

    public async Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            UPDATE tenants 
            SET name = @Name, slug = @Slug, logo_url = @LogoUrl, 
                contact_email = @ContactEmail, updated_at = @UpdatedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, tenant);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = "DELETE FROM tenants WHERE id = @Id";

        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = "SELECT 1 FROM tenants WHERE id = @Id";

        return await connection.QueryFirstOrDefaultAsync<int?>(sql, new { Id = id }) is not null;
    }

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = "SELECT 1 FROM tenants WHERE slug = @Slug";

        return await connection.QueryFirstOrDefaultAsync<int?>(sql, new { Slug = slug }) is not null;
    }
}