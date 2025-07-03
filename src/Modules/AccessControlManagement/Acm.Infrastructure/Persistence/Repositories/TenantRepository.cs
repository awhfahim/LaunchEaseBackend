using System.Data;
using System.Data.Common;
using Acm.Application.Repositories;
using Acm.Domain.Entities;
using Common.Application.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Acm.Infrastructure.Persistence.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<TenantRepository> _logger;

    public TenantRepository(IDbConnectionFactory connectionFactory, ILogger<TenantRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
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

    private async Task CreateDefaultAdminClaimsAsync(IDbConnection connection, IDbTransaction transaction, Guid roleId)
    {
        // Define default admin permissions for tenant
        var defaultPermissions = new[]
        {
            "users.view", "users.create", "users.edit", "users.delete",
            "roles.view", "roles.create", "roles.edit", "roles.delete",
            "tenant.settings.view", "tenant.settings.edit",
            "dashboard.view",
            "authentication.view", "authentication.edit",
            "authorization.view", "authorization.edit"
        };

        const string insertClaimSql = @"
        INSERT INTO role_claims (id, role_id, claim_type, claim_value)
        VALUES (@Id, @RoleId, @ClaimType, @ClaimValue)";

        var claimInserts = defaultPermissions.Select(permission => new
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            ClaimType = "permission",
            ClaimValue = permission
        });

        await connection.ExecuteAsync(insertClaimSql, claimInserts, transaction: transaction);
    }
    public async Task<Guid> CreateAsync(Tenant tenant, Role adminRole, User adminUser, UserRole userRole, 
        Guid userTenantId)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            const string sql = @"
            WITH tenant_insert AS (
                INSERT INTO tenants (id, name, slug, logo_url, contact_email, created_at, updated_at)
                    VALUES (@TenantId, @TenantName, @TenantSlug, @TenantLogoUrl, @TenantContactEmail, @TenantCreatedAt, @TenantUpdatedAt)
                    RETURNING id
            ),
                 role_insert AS (
                     INSERT INTO roles (id, tenant_id, name, description, created_at, updated_at)
                         SELECT @RoleId, id, @RoleName, @RoleDescription, @RoleCreatedAt, @RoleUpdatedAt
                         FROM tenant_insert
                         RETURNING id
                 ),
                 user_insert AS (
                     INSERT INTO users (id, email, first_name, last_name, password_hash, security_stamp,
                                        is_email_confirmed, is_globally_locked, global_lockout_end, global_access_failed_count,
                                        last_login_at, phone_number, is_phone_number_confirmed, created_at, updated_at)
                         VALUES (@UserId, @UserEmail, @UserFirstName, @UserLastName, @UserPasswordHash, @UserSecurityStamp,
                                 @UserIsEmailConfirmed, @UserIsGloballyLocked, @UserGlobalLockoutEnd, @UserGlobalAccessFailedCount,
                                 @UserLastLoginAt, @UserPhoneNumber, @UserIsPhoneNumberConfirmed, @UserCreatedAt, @UserUpdatedAt)
                         RETURNING id
                 ),
                 user_tenant_insert AS (
                     INSERT INTO user_tenants (id, user_id, tenant_id, is_active, joined_at, left_at, invited_by)
                         SELECT @UserTenantId, u.id, t.id, true, NOW(), NULL, NULL
                         FROM user_insert u, tenant_insert t
                         RETURNING id
                 )
            INSERT INTO user_roles (id, user_id, role_id, tenant_id)
            SELECT @UserRoleId, u.id, r.id, t.id
            FROM user_insert u, role_insert r, tenant_insert t";

            var parameters = new
            {
                UserTenantId = userTenantId,
                // Tenant parameters
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                TenantSlug = tenant.Slug,
                TenantLogoUrl = tenant.LogoUrl,
                TenantContactEmail = tenant.ContactEmail,
                TenantCreatedAt = tenant.CreatedAt,
                TenantUpdatedAt = tenant.UpdatedAt,

                // Role parameters
                RoleId = adminRole.Id,
                RoleTenantId = adminRole.TenantId,
                RoleName = adminRole.Name,
                RoleDescription = adminRole.Description,
                RoleCreatedAt = adminRole.CreatedAt,
                RoleUpdatedAt = adminRole.UpdatedAt,

                // User parameters
                UserId = adminUser.Id,
                UserEmail = adminUser.Email,
                UserFirstName = adminUser.FirstName,
                UserLastName = adminUser.LastName,
                UserPasswordHash = adminUser.PasswordHash,
                UserSecurityStamp = adminUser.SecurityStamp,
                UserIsEmailConfirmed = adminUser.IsEmailConfirmed,
                UserIsGloballyLocked = adminUser.IsGloballyLocked,
                UserGlobalLockoutEnd = adminUser.GlobalLockoutEnd,
                UserGlobalAccessFailedCount = adminUser.GlobalAccessFailedCount,
                UserLastLoginAt = adminUser.LastLoginAt,
                UserPhoneNumber = adminUser.PhoneNumber,
                UserIsPhoneNumberConfirmed = adminUser.IsPhoneNumberConfirmed,
                UserCreatedAt = adminUser.CreatedAt,
                UserUpdatedAt = adminUser.UpdatedAt,

                // UserRole parameters
                UserRoleId = userRole.Id
            };

            await connection.ExecuteAsync(sql, parameters, transaction: transaction);

            // Still need to add permissions separately
            await CreateDefaultAdminClaimsAsync(connection, transaction, adminRole.Id);

            await transaction.CommitAsync();

            _logger.LogInformation("Successfully created tenant {TenantName} with admin user {AdminEmail}",
                tenant.Name, adminUser.Email);

            return tenant.Id;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex,
                "Failed to create tenant {TenantName} with admin user {AdminEmail}. Transaction rolled back.",
                tenant.Name, adminUser.Email);
            throw;
        }
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