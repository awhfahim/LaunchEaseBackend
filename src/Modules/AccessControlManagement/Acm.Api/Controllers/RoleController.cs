using Acm.Api.DTOs.Requests;
using Acm.Api.DTOs.Responses;
using Acm.Application.Repositories;
using Acm.Application.Services.Interfaces;
using Acm.Domain.Entities;
using Acm.Infrastructure.Authorization.Attributes;
using Acm.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Common.HttpApi.Controllers;
using Common.HttpApi.DTOs;
using Microsoft.Extensions.Logging;

namespace Acm.Api.Controllers;

[Route("api/roles")]
[Authorize]
[RequireTenant]
public class RoleController : JsonApiControllerBase
{
    private readonly IRoleRepository _roleRepository;
    private readonly IRoleClaimRepository _roleClaimRepository;
    private readonly IRoleService _roleService;
    private readonly ILogger<RoleController> _logger;

    public RoleController(
        IRoleRepository roleRepository,
        IRoleClaimRepository roleClaimRepository,
        IRoleService roleService, ILogger<RoleController> logger)
    {
        _roleRepository = roleRepository;
        _roleClaimRepository = roleClaimRepository;
        _roleService = roleService;
        _logger = logger;
    }

    [HttpGet]
    [RequirePermission(PermissionConstants.RolesAndPermissionView, PermissionConstants.UsersCreate,
        PermissionConstants.UsersEdit, PermissionConstants.UsersView)]
    public async Task<IActionResult> GetRoles()
    {
        try
        {
            var result = await _roleService.GetRolesByTenantIdAsync(GetTenantId(), HttpContext.RequestAborted);
            return Ok(ApiResponse<IEnumerable<Acm.Domain.DTOs.RoleResponse>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return StatusCode(500,
                ApiResponse<IEnumerable<Acm.Domain.DTOs.RoleResponse>>.ErrorResult("An error occurred while fetching roles"));
        }
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionConstants.RolesAndPermissionView)]
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
    [RequirePermission(PermissionConstants.RolesAndPermissionCreate)]
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
    [RequirePermission(PermissionConstants.RolesAndPermissionEdit)]
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
    [RequirePermission(PermissionConstants.RolesAndPermissionDelete)]
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
    [RequirePermission(PermissionConstants.RolesAndPermissionEdit)]
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
}