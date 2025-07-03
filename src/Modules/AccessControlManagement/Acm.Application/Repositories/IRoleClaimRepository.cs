using Acm.Domain.Entities;
using System.Security.Claims;

namespace Acm.Application.Repositories;

public interface IRoleClaimRepository
{
    Task<IEnumerable<RoleClaim>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<RoleClaim?> GetAsync(Guid roleId, string claimType, string claimValue, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(RoleClaim roleClaim, CancellationToken cancellationToken = default);
    Task UpdateAsync(RoleClaim roleClaim, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid roleId, string claimType, string claimValue, CancellationToken cancellationToken = default);
    Task DeleteByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid roleId, string claimType, string claimValue, CancellationToken cancellationToken = default);
    Task<IEnumerable<Claim>> GetClaimsForRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Claim>> GetClaimsForUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Claim>> GetClaimsForUserRolesAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
}
