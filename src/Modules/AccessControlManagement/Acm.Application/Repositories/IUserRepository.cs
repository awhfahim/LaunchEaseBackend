using Acm.Domain.Entities;

namespace Acm.Application.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default); // Global email lookup

    Task<IEnumerable<User>> GetByTenantIdAsync(Guid tenantId, int page, int limit,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default); // Global email check
    Task<int> GetAccessFailedCountAsync(Guid id, CancellationToken cancellationToken = default);
    Task SetAccessFailedCountAsync(Guid id, int count, CancellationToken cancellationToken = default);
    Task SetLockoutEndAsync(Guid id, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken = default);
    Task<DateTimeOffset?> GetLockoutEndAsync(Guid id, CancellationToken cancellationToken = default);
    Task SetPasswordHashAsync(Guid id, string passwordHash, CancellationToken cancellationToken = default);
    Task SetSecurityStampAsync(Guid id, string securityStamp, CancellationToken cancellationToken = default);
}