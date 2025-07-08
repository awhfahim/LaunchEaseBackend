using Acm.Application.DataTransferObjects;
using Acm.Application.Repositories;
using Acm.Infrastructure.Authorization;
using Acm.Infrastructure.Authorization.Attributes;
using Common.HttpApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acm.Api.Controllers
{
    [Route("api/[controller]s")]
    [Authorize]
    public class PermissionController : JsonApiControllerBase
    {
        private readonly IPermissionManagementRepository _permissionRepository;
        public PermissionController(
            IPermissionManagementRepository permissionRepository)
        {
            _permissionRepository = permissionRepository;
        }
        
        #region Tenant Permission Management
        
        [HttpGet("tenant")]
        [RequirePermission(PermissionConstants.RolesAndPermissionView)]
        public async Task<ActionResult<IEnumerable<MasterClaimDto>>> GetTenantPermissions()
        {
            var permissions = await _permissionRepository.GetTenantPermissionsAsync("tenant");
            return Ok(permissions);
        }
        
        #endregion

        #region Master Permissions Management
        
        [HttpGet]
        [RequirePermission(PermissionConstants.SystemAdmin)]
        public async Task<ActionResult<IEnumerable<MasterClaimDto>>> GetAllPermissions()
        {
            var permissions = await _permissionRepository.GetAllPermissionsAsync();
            return Ok(permissions);
        }
        
        [HttpGet("category/{category}")]
        [RequirePermission(PermissionConstants.SystemAdmin)]
        public async Task<ActionResult<IEnumerable<MasterClaimDto>>> GetPermissionsByCategory(string category)
        {
            var permissions = await _permissionRepository.GetTenantPermissionsAsync(category.ToLowerInvariant());
            return Ok(permissions);
        }

        #endregion

        #region Role Permission Management
        
        [HttpGet("roles/{roleId}")]
        [RequirePermission(PermissionConstants.RolesAndPermissionView)]
        public async Task<IActionResult> GetRolePermissions(Guid roleId)
        {
            var permissions = await _permissionRepository.GetRolePermissionsAsync(roleId);
            return Ok(permissions);
        }
        
        [HttpPost("roles/{roleId}/assign")]
        [RequirePermission(PermissionConstants.RolesManagePermissions)]
        public async Task<IActionResult> AssignPermissionsToRole(Guid roleId, [FromBody] AssignPermissionsDto assignPermissionsDto)
        {
            await _permissionRepository.AssignPermissionsToRoleAsync(roleId, assignPermissionsDto.ClaimValues);
            return NoContent();
        }
        
        [HttpPost("roles/{roleId}/remove")]
        [RequirePermission(PermissionConstants.RolesManagePermissions)]
        public async Task<IActionResult> RemovePermissionsFromRole(Guid roleId, [FromBody] AssignPermissionsDto removePermissionsDto)
        {
            await _permissionRepository.RemovePermissionsFromRoleAsync(roleId, removePermissionsDto.ClaimValues);
            return NoContent();
        }
        
        [HttpPut("roles/{roleId}")]
        [RequirePermission(PermissionConstants.RolesManagePermissions)]
        public async Task<IActionResult> ReplaceRolePermissions(Guid roleId, [FromBody] AssignPermissionsDto replacePermissionsDto)
        {
            await _permissionRepository.ReplaceRolePermissionsAsync(roleId, replacePermissionsDto.ClaimValues);
            return NoContent();
        }

        #endregion

        #region User Permission Management
        
        [HttpGet("users/{userId}/tenants/{tenantId}")]
        [RequirePermission(PermissionConstants.UsersView)]
        public async Task<ActionResult<IEnumerable<UserPermissionDto>>> GetUserPermissions(Guid userId, Guid tenantId)
        {
            var permissions = await _permissionRepository.GetUserAllPermissionsAsync(userId, tenantId);
            return Ok(permissions);
        }
        
        [HttpGet("users/{userId}/tenants/{tenantId}/direct")]
        [RequirePermission(PermissionConstants.UsersView)]
        public async Task<ActionResult<IEnumerable<UserPermissionDto>>> GetUserDirectPermissions(Guid userId, Guid tenantId)
        {
            var permissions = await _permissionRepository.GetUserDirectPermissionsAsync(userId, tenantId);
            return Ok(permissions);
        }
        
        [HttpPost("users/{userId}/tenants/{tenantId}/assign")]
        [RequirePermission(PermissionConstants.UsersManageRoles)]
        public async Task<IActionResult> AssignDirectPermissionsToUser(Guid userId, Guid tenantId, [FromBody] AssignPermissionsDto assignPermissionsDto)
        {
            await _permissionRepository.AssignDirectPermissionsToUserAsync(userId, tenantId, assignPermissionsDto.ClaimValues);
            return NoContent();
        }
        
        [HttpPost("users/{userId}/tenants/{tenantId}/remove")]
        [RequirePermission(PermissionConstants.UsersManageRoles)]
        public async Task<IActionResult> RemoveDirectPermissionsFromUser(Guid userId, Guid tenantId, [FromBody] AssignPermissionsDto removePermissionsDto)
        {
            await _permissionRepository.RemoveDirectPermissionsFromUserAsync(userId, tenantId, removePermissionsDto.ClaimValues);
            return NoContent();
        }

        #endregion
    }
}
