using System.Data.Common;
using Acm.Domain.Entities;

namespace Acm.Application.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Role?> GetByNameAsync(string name, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Role>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Role role, DbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task UpdateAsync(Role role, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid tenantId, CancellationToken cancellationToken = default);
}
