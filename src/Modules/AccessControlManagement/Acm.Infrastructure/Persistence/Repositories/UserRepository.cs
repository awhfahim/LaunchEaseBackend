using System.Data.Common;
using Acm.Application.Repositories;
using Acm.Domain.Entities;
using Common.Application.Data;
using Dapper;

namespace Acm.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, tenant_id, email, first_name, last_name, password_hash, security_stamp,
                   is_email_confirmed, is_locked, lockout_end, access_failed_count, 
                   last_login_at, phone_number, is_phone_number_confirmed, created_at, updated_at
            FROM users 
            WHERE id = @Id";

        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<User?> GetByEmailAsync(string email, Guid tenantId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, tenant_id, email, first_name, last_name, password_hash, security_stamp,
                   is_email_confirmed, is_locked, lockout_end, access_failed_count, 
                   last_login_at, phone_number, is_phone_number_confirmed, created_at, updated_at
            FROM users 
            WHERE email = @Email AND tenant_id = @TenantId";

        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email, TenantId = tenantId });
    }

    public async Task<IEnumerable<User>> GetByTenantIdAsync(Guid tenantId, int page, int limit,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, tenant_id, email, first_name, last_name, password_hash, security_stamp,
                   is_email_confirmed, is_locked, lockout_end, access_failed_count, 
                   last_login_at, phone_number, is_phone_number_confirmed, created_at, updated_at
            FROM users 
            WHERE tenant_id = @TenantId
            ORDER BY created_at DESC
            OFFSET @Page LIMIT @Limit";

        return await connection.QueryAsync<User>(sql, new { TenantId = tenantId, Page = page, Limit = limit });
    }

    public async Task<Guid> CreateAsync(User user, DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            INSERT INTO users (id, tenant_id, email, first_name, last_name, password_hash, security_stamp,
                              is_email_confirmed, is_locked, lockout_end, access_failed_count, 
                              last_login_at, phone_number, is_phone_number_confirmed, created_at, updated_at)
            VALUES (@Id, @TenantId, @Email, @FirstName, @LastName, @PasswordHash, @SecurityStamp,
                    @IsEmailConfirmed, @IsLocked, @LockoutEnd, @AccessFailedCount, 
                    @LastLoginAt, @PhoneNumber, @IsPhoneNumberConfirmed, @CreatedAt, @UpdatedAt)";

        await connection.ExecuteAsync(sql, user, transaction: transaction);
        return user.Id;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            UPDATE users 
            SET email = @Email, first_name = @FirstName, last_name = @LastName, 
                password_hash = @PasswordHash, security_stamp = @SecurityStamp,
                is_email_confirmed = @IsEmailConfirmed, is_locked = @IsLocked, 
                lockout_end = @LockoutEnd, access_failed_count = @AccessFailedCount, 
                last_login_at = @LastLoginAt, phone_number = @PhoneNumber, 
                is_phone_number_confirmed = @IsPhoneNumberConfirmed, updated_at = @UpdatedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, user);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = "DELETE FROM users WHERE id = @Id";

        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = "SELECT 1 FROM users WHERE id = @Id";

        return await connection.QueryFirstOrDefaultAsync<int?>(sql, new { Id = id }) is not null;
    }

    public async Task<bool> EmailExistsAsync(string email, Guid tenantId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = "SELECT 1 FROM users WHERE email = @Email AND tenant_id = @TenantId";

        return await connection.QueryFirstOrDefaultAsync<int?>(sql, new { Email = email, TenantId = tenantId }) is not
            null;
    }

    public async Task<int> GetAccessFailedCountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = "SELECT access_failed_count FROM users WHERE id = @Id";

        return await connection.QueryFirstOrDefaultAsync<int>(sql, new { Id = id });
    }

    public async Task SetAccessFailedCountAsync(Guid id, int count, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            UPDATE users 
            SET access_failed_count = @Count, updated_at = @UpdatedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, new { Id = id, Count = count, UpdatedAt = DateTime.UtcNow });
    }

    public async Task SetLockoutEndAsync(Guid id, DateTimeOffset? lockoutEnd,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            UPDATE users 
            SET lockout_end = @LockoutEnd, is_locked = @IsLocked, updated_at = @UpdatedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            LockoutEnd = lockoutEnd?.DateTime,
            IsLocked = lockoutEnd.HasValue && lockoutEnd > DateTimeOffset.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task<DateTimeOffset?> GetLockoutEndAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = "SELECT lockout_end FROM users WHERE id = @Id";

        var result = await connection.QueryFirstOrDefaultAsync<DateTime?>(sql, new { Id = id });
        return result.HasValue ? new DateTimeOffset(result.Value, TimeSpan.Zero) : null;
    }

    public async Task SetPasswordHashAsync(Guid id, string passwordHash, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            UPDATE users 
            SET password_hash = @PasswordHash, updated_at = @UpdatedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, new { Id = id, PasswordHash = passwordHash, UpdatedAt = DateTime.UtcNow });
    }

    public async Task SetSecurityStampAsync(Guid id, string securityStamp,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            UPDATE users 
            SET security_stamp = @SecurityStamp, updated_at = @UpdatedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, new { Id = id, SecurityStamp = securityStamp, UpdatedAt = DateTime.UtcNow });
    }
}