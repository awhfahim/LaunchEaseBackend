using Acm.Api.DTOs.Requests;
using Acm.Api.DTOs.Responses;
using Acm.Application.Repositories;
using Acm.Domain.Entities;
using Acm.Infrastructure.Authorization.Attributes;
using Acm.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Acm.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
[RequireTenant]
public class RolesController : ControllerBase
{
    private readonly IRoleRepository _roleRepository;
    private readonly IRoleClaimRepository _roleClaimRepository;
    private readonly IUserRoleRepository _userRoleRepository;

    public RolesController(
        IRoleRepository roleRepository,
        IRoleClaimRepository roleClaimRepository,
        IUserRoleRepository userRoleRepository)
    {
        _roleRepository = roleRepository;
        _roleClaimRepository = roleClaimRepository;
        _userRoleRepository = userRoleRepository;
    }

    private Guid GetTenantId()
    {
        return (Guid)HttpContext.Items["TenantId"]!;
    }

    [HttpGet]
    [RequirePermission(PermissionConstants.RolesView)]
    public async Task<ActionResult<ApiResponse<IEnumerable<RoleResponse>>>> GetRoles()
    {
        try
        {
            var tenantId = GetTenantId();
            var roles = await _roleRepository.GetByTenantIdAsync(tenantId);

            var responses = new List<RoleResponse>();
            foreach (var role in roles)
            {
                var claims = await _roleClaimRepository.GetClaimsForRoleAsync(role.Id);
                var permissions = claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();

                responses.Add(new RoleResponse
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    CreatedAt = role.CreatedAt,
                    Permissions = permissions
                });
            }

            return Ok(ApiResponse<IEnumerable<RoleResponse>>.SuccessResult(responses));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<RoleResponse>>.ErrorResult("An error occurred while fetching roles"));
        }
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionConstants.RolesView)]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> GetRole([FromRoute] Guid id)
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
    public async Task<ActionResult<ApiResponse<RoleResponse>>> CreateRole([FromBody] CreateRoleRequest request)
    {
        try
        {
            var tenantId = GetTenantId();

            // Check if role name already exists in this tenant
            var existingRole = await _roleRepository.GetByNameAsync(request.Name, tenantId);
            if (existingRole != null)
            {
                return BadRequest(ApiResponse<RoleResponse>.ErrorResult("Role name already exists"));
            }

            // Create role
            var role = new Role
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = request.Name,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _roleRepository.CreateAsync(role);

            // Add permissions as claims
            foreach (var permission in request.Permissions)
            {
                var roleClaim = new RoleClaim
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    ClaimType = "permission",
                    ClaimValue = permission
                };
                await _roleClaimRepository.CreateAsync(roleClaim);
            }

            var response = new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt,
                Permissions = request.Permissions.ToList()
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
    public async Task<ActionResult<ApiResponse<RoleResponse>>> UpdateRole([FromRoute] Guid id, [FromBody] UpdateRoleRequest request)
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
    public async Task<ActionResult<ApiResponse<object>>> DeleteRole([FromRoute] Guid id)
    {
        try
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null || role.TenantId != GetTenantId())
            {
                return NotFound(ApiResponse<object>.ErrorResult("Role not found"));
            }

            // Delete role claims
            await _roleClaimRepository.DeleteByRoleIdAsync(id);

            // Delete user roles
            await _userRoleRepository.DeleteByRoleIdAsync(id);

            // Delete role
            await _roleRepository.DeleteAsync(id);

            return Ok(ApiResponse<object>.SuccessResult(null, "Role deleted successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while deleting role"));
        }
    }

    [HttpPost("{id:guid}/permissions")]
    [RequirePermission(PermissionConstants.RolesEdit)]
    public async Task<ActionResult<ApiResponse<object>>> AssignPermissions([FromRoute] Guid id, [FromBody] AssignPermissionsRequest request)
    {
        try
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null || role.TenantId != GetTenantId())
            {
                return NotFound(ApiResponse<object>.ErrorResult("Role not found"));
            }

            // Remove existing permission claims
            var existingClaims = await _roleClaimRepository.GetByRoleIdAsync(id);
            var permissionClaims = existingClaims.Where(c => c.ClaimType == "permission");
            foreach (var claim in permissionClaims)
            {
                await _roleClaimRepository.DeleteAsync(claim.Id);
            }

            // Add new permission claims
            foreach (var permission in request.Permissions)
            {
                var roleClaim = new RoleClaim
                {
                    Id = Guid.NewGuid(),
                    RoleId = id,
                    ClaimType = "permission",
                    ClaimValue = permission
                };
                await _roleClaimRepository.CreateAsync(roleClaim);
            }

            return Ok(ApiResponse<object>.SuccessResult(null, "Permissions assigned successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while assigning permissions"));
        }
    }

    [HttpGet("permissions")]
    [RequirePermission(PermissionConstants.RolesView)]
    public ActionResult<ApiResponse<IEnumerable<string>>> GetAvailablePermissions()
    {
        var permissions = new[]
        {
            PermissionConstants.UsersView,
            PermissionConstants.UsersCreate,
            PermissionConstants.UsersEdit,
            PermissionConstants.UsersDelete,
            PermissionConstants.RolesView,
            PermissionConstants.RolesCreate,
            PermissionConstants.RolesEdit,
            PermissionConstants.RolesDelete,
            PermissionConstants.TenantsView,
            PermissionConstants.TenantsEdit,
            PermissionConstants.DashboardView,
            PermissionConstants.SystemAdmin
        };

        return Ok(ApiResponse<IEnumerable<string>>.SuccessResult(permissions));
    }
}
