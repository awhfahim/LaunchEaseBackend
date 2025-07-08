using Acm.Domain.DTOs;

namespace Acm.Application.Services.Interfaces;

public interface IRoleService
{
    Task<(bool result, string message)>
        DeleteRoleAsync(Guid roleId, Guid tenantId, CancellationToken cancellationToken);

    Task<(bool result, string message)> AssignPermissionsAsync(Guid roleId, Guid tenantId, IEnumerable<Guid> claimIds,
        CancellationToken cancellationToken);
    
    Task<IEnumerable<RoleResponse>> GetRolesByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken);
}