using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using Acm.Application.Repositories;
using Acm.Domain.Entities;
using Common.Application.Data;
using Common.Infrastructure.Persistence.Repositories;
using Dapper;
using MailKit.Search;

namespace Acm.Infrastructure.Persistence.Repositories;

[Table("users")]
public class EnhancedUserRepository : DapperGenericRepository<User, Guid>, IUserRepository
{
    public EnhancedUserRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }

    #region Generic Repository Implementation

    protected override string GetInsertSql()
    {
        return BuildInsertSql([
            "id", "email", "first_name", "last_name", "password_hash", "security_stamp",
            "is_email_confirmed", "is_globally_locked", "global_lockout_end",
            "global_access_failed_count", "last_login_at", "phone_number",
            "is_phone_number_confirmed", "created_at", "updated_at"
        ]);
    }

    protected override string GetUpdateSql()
    {
        return BuildUpdateSql([
            "email", "first_name", "last_name", "password_hash", "security_stamp",
            "is_email_confirmed", "is_globally_locked", "global_lockout_end",
            "global_access_failed_count", "last_login_at", "phone_number",
            "is_phone_number_confirmed", "updated_at"
        ]);
    }

    protected override string GetSelectSql()
    {
        return BuildSelectSql([
            "id", "email", "first_name", "last_name", "password_hash", "security_stamp",
            "is_email_confirmed", "is_globally_locked", "global_lockout_end",
            "global_access_failed_count", "last_login_at", "phone_number",
            "is_phone_number_confirmed", "created_at", "updated_at"
        ]);
    }

    #endregion

    #region IUserRepository Implementation (Custom Methods)

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await GetByEmailAsync(email, connection, null, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, IDbConnection connection, IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, email, first_name, last_name, password_hash, security_stamp,
                   is_email_confirmed, is_globally_locked, global_lockout_end, 
                   global_access_failed_count, last_login_at, phone_number, 
                   is_phone_number_confirmed, created_at, updated_at
            FROM users 
            WHERE email = @Email";

        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email }, transaction);
    }

    public async Task<(int, IEnumerable<User>)> GetByTenantIdAsync(Guid tenantId, int page, int limit,
        string? searchString = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await GetByTenantIdAsync(tenantId, page, limit, searchString, connection, null, cancellationToken);
    }

    public async Task<(int, IEnumerable<User>)> GetByTenantIdAsync(Guid tenantId, int page, int limit,
        string? searchString, IDbConnection connection, IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var offset = (page - 1) * limit;
        const string sql = @"
            SELECT COUNT(1) AS TotalCount
            FROM users u
            INNER JOIN user_tenants ut ON u.id = ut.user_id
            WHERE ut.tenant_id = @TenantId
              AND ut.is_active
              AND (
                @SearchQuery IS NULL
                OR u.email ILIKE ('%' || @SearchQuery || '%')
                OR (u.first_name || ' ' || u.last_name) ILIKE ('%' || @SearchQuery || '%')
              );
            
            SELECT u.id, u.email, u.first_name, u.last_name, u.password_hash, u.security_stamp,
                   u.is_email_confirmed, u.is_globally_locked, u.global_lockout_end, 
                   u.global_access_failed_count, u.last_login_at, u.phone_number, 
                   u.is_phone_number_confirmed, u.created_at, u.updated_at
            FROM users u
            INNER JOIN user_tenants ut ON u.id = ut.user_id
            WHERE ut.tenant_id = @TenantId
              AND ut.is_active
              AND (
                @SearchQuery IS NULL
                OR u.email ILIKE ('%' || @SearchQuery || '%')
                OR (u.first_name || ' ' || u.last_name) ILIKE ('%' || @SearchQuery || '%')
              )
            ORDER BY u.created_at DESC
            LIMIT @Limit OFFSET @Offset;";

        var result = await connection.QueryMultipleAsync(sql,
            new { TenantId = tenantId, Limit = limit, Offset = offset, SearchQuery = searchString },
            transaction);

        var totalCount = await result.ReadSingleAsync<int>();
        var users = await result.ReadAsync<User>();
        return (totalCount, users);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await EmailExistsAsync(email, connection, null, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, IDbConnection connection, IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM users WHERE email = @Email)";
        return await connection.QuerySingleAsync<bool>(sql, new { Email = email }, transaction);
    }

    public async Task<int> GetAccessFailedCountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await GetAccessFailedCountAsync(id, connection, null, cancellationToken);
    }

    public async Task<int> GetAccessFailedCountAsync(Guid id, IDbConnection connection,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT global_access_failed_count FROM users WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<int>(sql, new { Id = id }, transaction);
    }

    public async Task SetAccessFailedCountAsync(Guid id, int count, CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);
        await SetAccessFailedCountAsync(id, count, connection, null, cancellationToken);
    }

    public async Task SetAccessFailedCountAsync(Guid id, int count, IDbConnection connection,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE users 
            SET global_access_failed_count = @Count, updated_at = @UpdatedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, new { Id = id, Count = count, UpdatedAt = DateTime.UtcNow }, transaction);
    }

    public async Task SetLockoutEndAsync(Guid id, DateTimeOffset? lockoutEnd,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);
        await SetLockoutEndAsync(id, lockoutEnd, connection, null, cancellationToken);
    }

    public async Task SetLockoutEndAsync(Guid id, DateTimeOffset? lockoutEnd, IDbConnection connection,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE users 
            SET global_lockout_end = @LockoutEnd, updated_at = @UpdatedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql,
            new { Id = id, LockoutEnd = lockoutEnd?.DateTime, UpdatedAt = DateTime.UtcNow }, transaction);
    }

    public async Task<DateTimeOffset?> GetLockoutEndAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await GetLockoutEndAsync(id, connection, null, cancellationToken);
    }

    public async Task<DateTimeOffset?> GetLockoutEndAsync(Guid id, IDbConnection connection,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT global_lockout_end FROM users WHERE id = @Id";
        var result = await connection.QueryFirstOrDefaultAsync<DateTime?>(sql, new { Id = id }, transaction);
        return result?.ToUniversalTime();
    }

    public async Task SetPasswordHashAsync(Guid id, string passwordHash, CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);
        await SetPasswordHashAsync(id, passwordHash, connection, null, cancellationToken);
    }

    public async Task SetPasswordHashAsync(Guid id, string passwordHash, IDbConnection connection,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE users 
            SET password_hash = @PasswordHash, updated_at = @UpdatedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, new { Id = id, PasswordHash = passwordHash, UpdatedAt = DateTime.UtcNow },
            transaction);
    }

    public async Task SetSecurityStampAsync(Guid id, string securityStamp,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);
        await SetSecurityStampAsync(id, securityStamp, connection, null, cancellationToken);
    }

    public async Task SetSecurityStampAsync(Guid id, string securityStamp, IDbConnection connection,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE users 
            SET security_stamp = @SecurityStamp, updated_at = @UpdatedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, new { Id = id, SecurityStamp = securityStamp, UpdatedAt = DateTime.UtcNow },
            transaction);
    }

    #endregion

    #region Enhanced Methods with Automatic Timestamp Updates

    /// <summary>
    /// Create user with automatic timestamp - overrides the base InsertAsync
    /// </summary>
    public override async Task<Guid> InsertAsync(User user, CancellationToken cancellationToken = default)
    {
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        return await base.InsertAsync(user, cancellationToken);
    }

    /// <summary>
    /// Create user with automatic timestamp and transaction support
    /// </summary>
    public override async Task<Guid> InsertAsync(User user, IDbConnection connection,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return await base.InsertAsync(user, connection, transaction, cancellationToken);
    }

    /// <summary>
    /// Update user with automatic timestamp - overrides the base UpdateAsync
    /// </summary>
    public override async Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        return await base.UpdateAsync(user, cancellationToken);
    }

    /// <summary>
    /// Update user with automatic timestamp and transaction support
    /// </summary>
    public override async Task<bool> UpdateAsync(User user, IDbConnection connection,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        user.UpdatedAt = DateTime.UtcNow;
        return await base.UpdateAsync(user, connection, transaction, cancellationToken);
    }

    #endregion
}