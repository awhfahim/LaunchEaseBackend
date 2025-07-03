using System.Data.Common;
using Acm.Domain.Entities;

namespace Acm.Application.Repositories;

public interface IUserRoleRepository
{
    Task<IEnumerable<UserRole>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserRole>> GetByUserIdAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserRole>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<UserRole?> GetAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task<UserRole?> GetAsync(Guid userId, Guid roleId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(UserRole userRole,
        CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid roleId, Guid tenantId, CancellationToken cancellationToken = default);
    Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task DeleteByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, Guid roleId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetRoleNamesForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetRoleNamesForUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
}