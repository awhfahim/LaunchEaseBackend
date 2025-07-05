using System.Data;
using Acm.Domain.Entities;
using Common.Domain.Interfaces;

namespace Acm.Application.Repositories;

/// <summary>
/// User repository interface extending generic repository with custom user-specific methods
/// </summary>
public interface IUserRepository : IGenericRepository<User, Guid>
{
    // Custom user-specific methods without transaction
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetByTenantIdAsync(Guid tenantId, int page, int limit, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<int> GetAccessFailedCountAsync(Guid id, CancellationToken cancellationToken = default);
    Task SetAccessFailedCountAsync(Guid id, int count, CancellationToken cancellationToken = default);
    Task SetLockoutEndAsync(Guid id, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken = default);
    Task<DateTimeOffset?> GetLockoutEndAsync(Guid id, CancellationToken cancellationToken = default);
    Task SetPasswordHashAsync(Guid id, string passwordHash, CancellationToken cancellationToken = default);
    Task SetSecurityStampAsync(Guid id, string securityStamp, CancellationToken cancellationToken = default);

    // Custom user-specific methods with transaction support
    Task<User?> GetByEmailAsync(string email, IDbConnection connection, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetByTenantIdAsync(Guid tenantId, int page, int limit, IDbConnection connection, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, IDbConnection connection, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int> GetAccessFailedCountAsync(Guid id, IDbConnection connection, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task SetAccessFailedCountAsync(Guid id, int count, IDbConnection connection, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task SetLockoutEndAsync(Guid id, DateTimeOffset? lockoutEnd, IDbConnection connection, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<DateTimeOffset?> GetLockoutEndAsync(Guid id, IDbConnection connection, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task SetPasswordHashAsync(Guid id, string passwordHash, IDbConnection connection, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task SetSecurityStampAsync(Guid id, string securityStamp, IDbConnection connection, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}