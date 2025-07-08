using Acm.Application.DataTransferObjects;
using Acm.Application.DataTransferObjects.Request;
using Acm.Application.DataTransferObjects.Response;
using Acm.Domain.Entities;
using Common.Application.Misc;

namespace Acm.Application.Services.Interfaces;

public interface IUserService
{
    Task<Result<User>> CreateUserWithTenantAsync(CreateUserRequest request, Guid tenantId, Guid roleId,
        CancellationToken cancellationToken = default);

    Task UpdateUserSecurityInfoAsync(Guid userId, string newPasswordHash, string newSecurityStamp,
        CancellationToken cancellationToken = default);

    Task<UserWithTenantsDto> GetUserWithTenantsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task BulkCreateUsersAsync(IEnumerable<User> users, Guid tenantId, CancellationToken cancellationToken = default);

    Task<(int, IEnumerable<User>)> GetUsersByTenantIdAsync(Guid tenantId, int page, int limit, string? searchString,
        CancellationToken cancellationToken);

    Task<Result<UserResponse>> GetUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken);

    Task<Result<User>> UpdateUserAsync(Guid id, UpdateUserRequest request, Guid tenantId,
        CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
    Task<Result<bool>> DeleteUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken);

    Task<Result<bool>> AssignRoleToUserAsync(Guid userId, ICollection<Guid> roleIds, Guid tenantId,
        CancellationToken cancellationToken);
    
    Task<Result<bool>> RemoveRoleFromUserAsync(Guid userId, Guid tenantId, ICollection<Guid> roleIds,
        CancellationToken cancellationToken);
    
    Task<Result<bool>> InviteUserAsync(string email, Guid tenantId,
        CancellationToken cancellationToken = default);
    
    Task<bool> IsUserMemberOfTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken);
    Task<IEnumerable<string>> GetExistingEmailsAsync(string email, CancellationToken ct);
    
    Task<Result<UserInfoDto>> GetRefreshTokenAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken);
    Task<Result<UserInfoDto>> GetUserInfoAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken);
}