using Acm.Api.DTOs.Requests;
using Acm.Api.DTOs.Responses;
using Acm.Application.Repositories;
using Acm.Application.Services.RoleServices;
using Acm.Domain.Entities;
using Acm.Infrastructure.Authorization.Attributes;
using Acm.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Common.HttpApi.Controllers;
using Common.HttpApi.DTOs;

namespace Acm.Api.Controllers;

[Route("api/roles")]
[Authorize]
[RequireTenant]
public class RolesController : JsonApiControllerBase
{
    private readonly IRoleRepository _roleRepository;
    private readonly IRoleClaimRepository _roleClaimRepository;
    private readonly IRoleService _roleService;

    public RolesController(
        IRoleRepository roleRepository,
        IRoleClaimRepository roleClaimRepository,
        IUserRoleRepository userRoleRepository, IRoleService roleService)
    {
        _roleRepository = roleRepository;
        _roleClaimRepository = roleClaimRepository;
        _roleService = roleService;
    }

    [HttpGet]
    [RequirePermission(PermissionConstants.RolesView)]
    public async Task<IActionResult> GetRoles()
    {
        try
        {
            var result = await _roleService.GetRolesByTenantIdAsync(GetTenantId(), HttpContext.RequestAborted);
            return Ok(ApiResponse<IEnumerable<Role>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500,
                ApiResponse<IEnumerable<RoleResponse>>.ErrorResult("An error occurred while fetching roles"));
        }
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionConstants.RolesView)]
    public async Task<IActionResult> GetRole([FromRoute] Guid id)
    {
        try
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null || role.TenantId != GetTenantId())
            {
                return NotFound(ApiResponse<RoleResponse>.ErrorResult("Role not found"));
            }

            var claims = await _roleClaimRepository.GetClaimsForRoleAsync(role.Id);
            var permissions = claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();

            var response = new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt,
                Permissions = permissions
            };

            return Ok(ApiResponse<RoleResponse>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<RoleResponse>.ErrorResult("An error occurred while fetching role"));
        }
    }

    [HttpPost]
    [RequirePermission(PermissionConstants.RolesCreate)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        try
        {
            // Check if role name already exists in this tenant
            var existingRole = await _roleRepository.GetByNameAsync(request.Name, GetTenantId());
            if (existingRole != null)
            {
                return BadRequest(ApiResponse<RoleResponse>.ErrorResult("Role name already exists"));
            }

            // Create role
            var role = new Role
            {
                Id = Guid.NewGuid(),
                TenantId = GetTenantId(),
                Name = request.Name,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _roleRepository.CreateAsync(role);

            var response = new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt
            };

            return CreatedAtAction(
                nameof(GetRole),
                new { id = role.Id },
                ApiResponse<RoleResponse>.SuccessResult(response, "Role created successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<RoleResponse>.ErrorResult("An error occurred while creating role"));
        }
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionConstants.RolesEdit)]
    public async Task<IActionResult> UpdateRole([FromRoute] Guid id, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null || role.TenantId != GetTenantId())
            {
                return NotFound(ApiResponse<RoleResponse>.ErrorResult("Role not found"));
            }

            // Check if role name already exists (excluding current role)
            var existingRole = await _roleRepository.GetByNameAsync(request.Name, GetTenantId());
            if (existingRole != null && existingRole.Id != id)
            {
                return BadRequest(ApiResponse<RoleResponse>.ErrorResult("Role name already exists"));
            }

            // Update role
            role.Name = request.Name;
            role.Description = request.Description;
            role.UpdatedAt = DateTime.UtcNow;

            await _roleRepository.UpdateAsync(role);

            var claims = await _roleClaimRepository.GetClaimsForRoleAsync(role.Id);
            var permissions = claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();

            var response = new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt,
                Permissions = permissions
            };

            return Ok(ApiResponse<RoleResponse>.SuccessResult(response, "Role updated successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<RoleResponse>.ErrorResult("An error occurred while updating role"));
        }
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionConstants.RolesDelete)]
    public async Task<IActionResult> DeleteRole([FromRoute] Guid id)
    {
        try
        {
            var tenantId = GetTenantId();
            var result = await _roleService.DeleteRoleAsync(id, tenantId, HttpContext.RequestAborted);

            if (!result.result)
                return NotFound(ApiResponse<object>.ErrorResult(result.message));

            return Ok(ApiResponse<object>.SuccessResult(result.result, result.message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while deleting role"));
        }
    }

    [HttpPost("{id:guid}/permissions")]
    [RequirePermission(PermissionConstants.RolesEdit)]
    public async Task<IActionResult> AssignPermissions([FromRoute] Guid id, [FromBody] AssignPermissionsRequest request)
    {
        try
        {
            if (request.Permissions.Count == 0)
            {
                return BadRequest("No permissions provided to assign");
            }
            
            var result = await _roleService.AssignPermissionsAsync(id, GetTenantId(), request.Permissions,
                HttpContext.RequestAborted);

            return Ok(ApiResponse<object>.SuccessResult(result.result, "Permissions assigned successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while assigning permissions"));
        }
    }

    [HttpGet("permissions")]
    [RequirePermission(PermissionConstants.RolesView)]
    public IActionResult GetAvailablePermissions()
    {
        //Todo: This should ideally be fetched from a configuration or database
        var permissions = new[]
        {
            // User Management (Tenant-scoped)
            PermissionConstants.UsersView,
            PermissionConstants.UsersCreate,
            PermissionConstants.UsersEdit,
            PermissionConstants.UsersDelete,

            // Role Management (Tenant-scoped)
            PermissionConstants.RolesView,
            PermissionConstants.RolesCreate,
            PermissionConstants.RolesEdit,
            PermissionConstants.RolesDelete,

            // Tenant Settings (Own tenant only)
            PermissionConstants.TenantSettingsView,
            PermissionConstants.TenantSettingsEdit,

            // Dashboard (Tenant-scoped)
            PermissionConstants.DashboardView,

            // Authentication & Authorization (Tenant-scoped)
            PermissionConstants.AuthenticationView,
            PermissionConstants.AuthenticationEdit,
            PermissionConstants.AuthorizationView,
            PermissionConstants.AuthorizationEdit,

            // SYSTEM-WIDE PERMISSIONS
            // Global Tenant Management
            PermissionConstants.GlobalTenantsView,
            PermissionConstants.GlobalTenantsCreate,
            PermissionConstants.GlobalTenantsEdit,
            PermissionConstants.GlobalTenantsDelete,

            // Global User Management (Cross-tenant)
            PermissionConstants.GlobalUsersView,
            PermissionConstants.GlobalUsersCreate,
            PermissionConstants.GlobalUsersEdit,
            PermissionConstants.GlobalUsersDelete,

            // Global Role Management (Cross-tenant)
            PermissionConstants.GlobalRolesView,
            PermissionConstants.GlobalRolesCreate,
            PermissionConstants.GlobalRolesEdit,
            PermissionConstants.GlobalRolesDelete,

            // System Administration
            PermissionConstants.SystemAdmin,
            PermissionConstants.SystemDashboard,
            PermissionConstants.SystemLogs,
            PermissionConstants.SystemConfiguration,

            // BUSINESS OWNER PERMISSIONS
            PermissionConstants.BusinessOwner,
            PermissionConstants.CrossTenantAccess
        };
        return Ok(ApiResponse<IEnumerable<string>>.SuccessResult(permissions));
    }
}