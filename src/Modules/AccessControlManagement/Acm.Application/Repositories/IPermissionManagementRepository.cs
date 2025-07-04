using Acm.Application.DataTransferObjects;

namespace Acm.Application.Repositories;

public interface IPermissionManagementRepository
{
    // Master Claims/Permissions Management
    Task<IEnumerable<MasterClaimDto>> GetAllPermissionsAsync();
    Task<IEnumerable<MasterClaimDto>> GetPermissionsByCategoryAsync(string category);
    Task<MasterClaimDto> CreatePermissionAsync(CreatePermissionDto createPermissionDto);
    Task<MasterClaimDto> UpdatePermissionAsync(Guid permissionId, UpdatePermissionDto updatePermissionDto);
    Task DeletePermissionAsync(Guid permissionId);

    // Role Permission Management
    Task<IEnumerable<RolePermissionDto>> GetRolePermissionsAsync(Guid roleId);
    Task AssignPermissionsToRoleAsync(Guid roleId, IEnumerable<string> claimValues);
    Task RemovePermissionsFromRoleAsync(Guid roleId, IEnumerable<string> claimValues);
    Task ReplaceRolePermissionsAsync(Guid roleId, IEnumerable<string> claimValues);

    // User Permission Management (Direct assignments)
    Task<IEnumerable<UserPermissionDto>> GetUserDirectPermissionsAsync(Guid userId, Guid tenantId);
    Task<IEnumerable<UserPermissionDto>> GetUserAllPermissionsAsync(Guid userId, Guid tenantId);
    Task AssignDirectPermissionsToUserAsync(Guid userId, Guid tenantId, IEnumerable<string> claimValues);
    Task RemoveDirectPermissionsFromUserAsync(Guid userId, Guid tenantId, IEnumerable<string> claimValues);

    // Permission Validation
    Task<bool> UserHasPermissionAsync(Guid userId, Guid tenantId, string permission);
    Task<bool> UserHasAnyPermissionAsync(Guid userId, Guid tenantId, IEnumerable<string> permissions);
    Task<bool> UserHasAllPermissionsAsync(Guid userId, Guid tenantId, IEnumerable<string> permissions);
}