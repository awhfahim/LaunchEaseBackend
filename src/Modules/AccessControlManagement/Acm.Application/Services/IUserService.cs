using Acm.Domain.Entities;
using Common.Application.Misc;

namespace Acm.Application.Services;

public interface IUserService
{
    Task<Guid> CreateUserWithTenantAsync(User user, Guid tenantId, Guid roleId,
        CancellationToken cancellationToken = default);

    Task UpdateUserSecurityInfoAsync(Guid userId, string newPasswordHash, string newSecurityStamp,
        CancellationToken cancellationToken = default);

    Task<UserWithTenantsDto> GetUserWithTenantsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task BulkCreateUsersAsync(IEnumerable<User> users, Guid tenantId, CancellationToken cancellationToken = default);

    Task<IEnumerable<User>> GetUsersByTenantIdAsync(Guid tenantId, int page, int limit,
        CancellationToken cancellationToken);

    Task<Result<User>> GetUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken);
    Task<bool> UpdateUserAsync(User user, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
    Task<bool> DeleteUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken);

    Task<bool> AssignRoleToUserAsync(Guid userId, ICollection<Guid> roleIds, Guid tenantId,
        CancellationToken cancellationToken);
    
    Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid tenantId, ICollection<Guid> roleIds,
        CancellationToken cancellationToken);
    
    Task<bool> InviteUserAsync(string email, Guid tenantId,
        CancellationToken cancellationToken = default);
    
    Task<bool> IsUserMemberOfTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken);
}