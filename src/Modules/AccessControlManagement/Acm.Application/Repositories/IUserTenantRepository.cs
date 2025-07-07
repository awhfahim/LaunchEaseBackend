using System.Data;
using Acm.Domain.Entities;

namespace Acm.Application.Repositories;

public interface IUserTenantRepository
{
    Task<IEnumerable<UserTenant>> GetUserTenantsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserTenant>> GetTenantUsersAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<UserTenant?> GetUserTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<Guid> AddUserToTenantAsync(UserTenant userTenant, CancellationToken cancellationToken = default);
    Task RemoveUserFromTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> IsUserMemberOfTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tenant>> GetUserAccessibleTenantsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Guid> AddUserToTenantAsync(UserTenant userTenant, IDbConnection connection,
        IDbTransaction transaction, CancellationToken cancellationToken = default);

    Task RemoveUserFromTenantAsync(Guid userId, Guid tenantId, IDbConnection connection,
        IDbTransaction transaction, CancellationToken cancellationToken = default);
    
    Task<bool> UpdateUserTenantAsync(UserTenant userTenant, CancellationToken cancellationToken = default);
}
