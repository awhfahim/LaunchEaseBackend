using Acm.Application.DataTransferObjects;
using Acm.Application.Services;

namespace Acm.Application.Repositories;

public interface IRoleManagementRepository
{
    // Role CRUD Operations
    Task<IEnumerable<RoleDto>> GetTenantRolesAsync(Guid tenantId);
    Task<RoleDto?> GetRoleByIdAsync(Guid roleId);
    Task<RoleDto?> GetRoleByNameAsync(Guid tenantId, string roleName);
    Task<RoleDto> CreateRoleAsync(CreateRoleDto createRoleDto);
    Task<RoleDto> UpdateRoleAsync(Guid roleId, UpdateRoleDto updateRoleDto);
    Task DeleteRoleAsync(Guid roleId);

    // Role-User Management
    Task<IEnumerable<UserDto>> GetUsersInRoleAsync(Guid roleId);
    Task<IEnumerable<RoleDto>> GetUserRolesAsync(Guid userId, Guid tenantId);
    Task AssignUserToRoleAsync(Guid userId, Guid roleId, Guid tenantId);
    Task RemoveUserFromRoleAsync(Guid userId, Guid roleId, Guid tenantId);
    Task ReplaceUserRolesAsync(Guid userId, Guid tenantId, IEnumerable<Guid> roleIds);

    // Role Permission Management (integration with Permission Service)
    Task AssignPermissionsToRoleAsync(Guid roleId, IEnumerable<string> permissions);
    Task RemovePermissionsFromRoleAsync(Guid roleId, IEnumerable<string> permissions);
    Task ReplaceRolePermissionsAsync(Guid roleId, IEnumerable<string> permissions);

    // Role Templates/Presets
    Task<RoleDto> CreateRoleFromTemplateAsync(Guid tenantId, RoleTemplateType templateType, string roleName);
    Task<IEnumerable<RoleTemplateDto>> GetAvailableRoleTemplatesAsync();

    // Validation
    Task<bool> RoleExistsAsync(Guid tenantId, string roleName);
    Task<bool> CanDeleteRoleAsync(Guid roleId);
}