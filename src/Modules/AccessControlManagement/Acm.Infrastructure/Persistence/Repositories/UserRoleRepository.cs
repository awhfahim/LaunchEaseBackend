using System.Data.Common;
using Acm.Application.Repositories;
using Acm.Domain.Entities;
using Common.Application.Data;
using Dapper;

namespace Acm.Infrastructure.Persistence.Repositories;

public class UserRoleRepository : IUserRoleRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRoleRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<UserRole>> GetByUserIdAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, user_id, role_id
            FROM user_roles 
            WHERE user_id = @UserId";

        return await connection.QueryAsync<UserRole>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<UserRole>> GetByRoleIdAsync(Guid roleId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, user_id, role_id
            FROM user_roles 
            WHERE role_id = @RoleId";

        return await connection.QueryAsync<UserRole>(sql, new { RoleId = roleId });
    }

    public async Task<UserRole?> GetAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT id, user_id, role_id
            FROM user_roles 
            WHERE user_id = @UserId AND role_id = @RoleId";

        return await connection.QueryFirstOrDefaultAsync<UserRole>(sql, new { UserId = userId, RoleId = roleId });
    }

    public async Task<Guid> CreateAsync(UserRole userRole, DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            INSERT INTO user_roles (id, user_id, role_id)
            VALUES (@Id, @UserId, @RoleId)";

        await connection.ExecuteAsync(sql, userRole, transaction: transaction);
        return userRole.Id;
    }

    public async Task DeleteAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = "DELETE FROM user_roles WHERE user_id = @UserId AND role_id = @RoleId";

        await connection.ExecuteAsync(sql, new { UserId = userId, RoleId = roleId });
    }

    public async Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = "DELETE FROM user_roles WHERE user_id = @UserId";

        await connection.ExecuteAsync(sql, new { UserId = userId });
    }

    public async Task DeleteByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = "DELETE FROM user_roles WHERE role_id = @RoleId";

        await connection.ExecuteAsync(sql, new { RoleId = roleId });
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = "SELECT 1 FROM user_roles WHERE user_id = @UserId AND role_id = @RoleId";

        return await connection.QueryFirstOrDefaultAsync<int?>(sql, new { UserId = userId, RoleId = roleId }) is not
            null;
    }

    public async Task<IEnumerable<string>> GetRoleNamesForUserAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();

        const string sql = @"
            SELECT r.name
            FROM user_roles ur
            INNER JOIN roles r ON ur.role_id = r.id
            WHERE ur.user_id = @UserId";

        return await connection.QueryAsync<string>(sql, new { UserId = userId });
    }
}