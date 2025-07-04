using Acm.Application.DataTransferObjects;
using Acm.Application.Repositories;
using Acm.Infrastructure.Authorization;
using Acm.Infrastructure.Authorization.Attributes;
using Common.HttpApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acm.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class PermissionsController : JsonApiControllerBase
    {
        private readonly IPermissionManagementRepository _permissionRepository;

        public PermissionsController(
            IPermissionManagementRepository permissionRepository)
        {
            _permissionRepository = permissionRepository;
        }

        #region Master Permissions Management

        /// <summary>
        /// Get all available permissions in the system
        /// Requires: system.admin or business.owner
        /// </summary>
        [HttpGet]
        [RequirePermission(PermissionConstants.SystemAdmin)]
        public async Task<ActionResult<IEnumerable<MasterClaimDto>>> GetAllPermissions()
        {
            var permissions = await _permissionRepository.GetAllPermissionsAsync();
            return Ok(permissions);
        }

        /// <summary>
        /// Get permissions by category
        /// </summary>
        [HttpGet("category/{category}")]
        [RequirePermission(PermissionConstants.SystemAdmin)]
        public async Task<ActionResult<IEnumerable<MasterClaimDto>>> GetPermissionsByCategory(string category)
        {
            var permissions = await _permissionRepository.GetPermissionsByCategoryAsync(category);
            return Ok(permissions);
        }

        /// <summary>
        /// Create a new permission (system admin only)
        /// </summary>
        [HttpPost]
        [RequirePermission(PermissionConstants.SystemAdmin)]
        public async Task<ActionResult<MasterClaimDto>> CreatePermission([FromBody] CreatePermissionDto createPermissionDto)
        {
            var permission = await _permissionRepository.CreatePermissionAsync(createPermissionDto);
            return CreatedAtAction(nameof(GetAllPermissions), permission);
        }

        /// <summary>
        /// Update an existing permission
        /// </summary>
        [HttpPut("{permissionId}")]
        [RequirePermission(PermissionConstants.SystemAdmin)]
        public async Task<ActionResult<MasterClaimDto>> UpdatePermission(Guid permissionId, [FromBody] UpdatePermissionDto updatePermissionDto)
        {
            var permission = await _permissionRepository.UpdatePermissionAsync(permissionId, updatePermissionDto);
            return Ok(permission);
        }

        /// <summary>
        /// Delete a permission (removes from all roles and users)
        /// </summary>
        [HttpDelete("{permissionId}")]
        [RequirePermission(PermissionConstants.SystemAdmin)]
        public async Task<IActionResult> DeletePermission(Guid permissionId)
        {
            await _permissionRepository.DeletePermissionAsync(permissionId);
            return NoContent();
        }

        #endregion

        #region Role Permission Management

        /// <summary>
        /// Get all permissions assigned to a role
        /// </summary>
        [HttpGet("roles/{roleId}")]
        [RequirePermission(PermissionConstants.RolesView)]
        public async Task<ActionResult<IEnumerable<RolePermissionDto>>> GetRolePermissions(Guid roleId)
        {
            var permissions = await _permissionRepository.GetRolePermissionsAsync(roleId);
            return Ok(permissions);
        }

        /// <summary>
        /// Assign permissions to a role
        /// </summary>
        [HttpPost("roles/{roleId}/assign")]
        [RequirePermission(PermissionConstants.RolesManagePermissions)]
        public async Task<IActionResult> AssignPermissionsToRole(Guid roleId, [FromBody] AssignPermissionsDto assignPermissionsDto)
        {
            await _permissionRepository.AssignPermissionsToRoleAsync(roleId, assignPermissionsDto.ClaimValues);
            return NoContent();
        }

        /// <summary>
        /// Remove permissions from a role
        /// </summary>
        [HttpPost("roles/{roleId}/remove")]
        [RequirePermission(PermissionConstants.RolesManagePermissions)]
        public async Task<IActionResult> RemovePermissionsFromRole(Guid roleId, [FromBody] AssignPermissionsDto removePermissionsDto)
        {
            await _permissionRepository.RemovePermissionsFromRoleAsync(roleId, removePermissionsDto.ClaimValues);
            return NoContent();
        }

        /// <summary>
        /// Replace all permissions for a role
        /// </summary>
        [HttpPut("roles/{roleId}")]
        [RequirePermission(PermissionConstants.RolesManagePermissions)]
        public async Task<IActionResult> ReplaceRolePermissions(Guid roleId, [FromBody] AssignPermissionsDto replacePermissionsDto)
        {
            await _permissionRepository.ReplaceRolePermissionsAsync(roleId, replacePermissionsDto.ClaimValues);
            return NoContent();
        }

        #endregion

        #region User Permission Management

        /// <summary>
        /// Get all permissions for a user in a tenant (includes role and direct permissions)
        /// </summary>
        [HttpGet("users/{userId}/tenants/{tenantId}")]
        [RequirePermission(PermissionConstants.UsersView)]
        public async Task<ActionResult<IEnumerable<UserPermissionDto>>> GetUserPermissions(Guid userId, Guid tenantId)
        {
            var permissions = await _permissionRepository.GetUserAllPermissionsAsync(userId, tenantId);
            return Ok(permissions);
        }

        /// <summary>
        /// Get only direct permissions assigned to a user
        /// </summary>
        [HttpGet("users/{userId}/tenants/{tenantId}/direct")]
        [RequirePermission(PermissionConstants.UsersView)]
        public async Task<ActionResult<IEnumerable<UserPermissionDto>>> GetUserDirectPermissions(Guid userId, Guid tenantId)
        {
            var permissions = await _permissionRepository.GetUserDirectPermissionsAsync(userId, tenantId);
            return Ok(permissions);
        }

        /// <summary>
        /// Assign direct permissions to a user
        /// </summary>
        [HttpPost("users/{userId}/tenants/{tenantId}/assign")]
        [RequirePermission(PermissionConstants.UsersManageRoles)]
        public async Task<IActionResult> AssignDirectPermissionsToUser(Guid userId, Guid tenantId, [FromBody] AssignPermissionsDto assignPermissionsDto)
        {
            await _permissionRepository.AssignDirectPermissionsToUserAsync(userId, tenantId, assignPermissionsDto.ClaimValues);
            return NoContent();
        }

        /// <summary>
        /// Remove direct permissions from a user
        /// </summary>
        [HttpPost("users/{userId}/tenants/{tenantId}/remove")]
        [RequirePermission(PermissionConstants.UsersManageRoles)]
        public async Task<IActionResult> RemoveDirectPermissionsFromUser(Guid userId, Guid tenantId, [FromBody] AssignPermissionsDto removePermissionsDto)
        {
            await _permissionRepository.RemoveDirectPermissionsFromUserAsync(userId, tenantId, removePermissionsDto.ClaimValues);
            return NoContent();
        }

        #endregion

        #region Permission Validation

        /// <summary>
        /// Check if a user has a specific permission
        /// </summary>
        [HttpPost("validate")]
        [RequirePermission(PermissionConstants.AuthenticationView)]
        public async Task<ActionResult<PermissionValidationDto>> ValidateUserPermission([FromBody] PermissionValidationDto validationDto)
        {
            var hasPermission = await _permissionRepository.UserHasPermissionAsync(
                validationDto.UserId, 
                validationDto.TenantId, 
                validationDto.Permission);

            validationDto.HasPermission = hasPermission;
            return Ok(validationDto);
        }

        /// <summary>
        /// Bulk permission validation
        /// </summary>
        [HttpPost("validate/bulk")]
        [RequirePermission(PermissionConstants.AuthenticationView)]
        public async Task<ActionResult<BulkPermissionValidationDto>> ValidateUserPermissions([FromBody] BulkPermissionValidationDto validationDto)
        {
            var hasAll = await _permissionRepository.UserHasAllPermissionsAsync(
                validationDto.UserId, 
                validationDto.TenantId, 
                validationDto.Permissions);

            var hasAny = await _permissionRepository.UserHasAnyPermissionAsync(
                validationDto.UserId, 
                validationDto.TenantId, 
                validationDto.Permissions);

            // Get individual permission checks
            var userPermissions = await _permissionRepository.GetUserAllPermissionsAsync(validationDto.UserId, validationDto.TenantId);
            var userPermissionValues = userPermissions.Select(p => p.ClaimValue).ToHashSet();

            validationDto.HasAllPermissions = hasAll;
            validationDto.HasAnyPermission = hasAny;
            validationDto.GrantedPermissions = validationDto.Permissions.Where(p => userPermissionValues.Contains(p)).ToList();
            validationDto.MissingPermissions = validationDto.Permissions.Where(p => !userPermissionValues.Contains(p)).ToList();

            return Ok(validationDto);
        }

        #endregion
    }
}
