using System.Data;
using Acm.Domain.Entities;
using System.Security.Claims;

namespace Acm.Application.Repositories;

public interface IUserClaimRepository
{
    Task<IEnumerable<UserClaim>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserClaim>> GetByUserIdAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<UserClaim?> GetAsync(Guid userId, string claimType, string claimValue, CancellationToken cancellationToken = default);
    Task<UserClaim?> GetAsync(Guid userId, string claimType, string claimValue, Guid tenantId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(UserClaim userClaim, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserClaim userClaim, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteByUserIdAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    Task DeleteByUserIdAsync(Guid userId, Guid tenantId, IDbConnection connection,
        IDbTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, string claimType, string claimValue, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, string claimType, string claimValue, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Claim>> GetClaimsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Claim>> GetClaimsForUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
}
