using Acm.Domain.Entities;

namespace Acm.Application.Services.RoleServices;

public interface IRoleService
{
    Task<(bool result, string message)>
        DeleteRoleAsync(Guid roleId, Guid tenantId, CancellationToken cancellationToken);

    Task<(bool result, string message)> AssignPermissionsAsync(Guid roleId, Guid tenantId, IEnumerable<Guid> claimIds,
        CancellationToken cancellationToken);
    
    Task<IEnumerable<Role>> GetRolesByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken);
}