using Acm.Api.DTOs.Requests;
using Acm.Api.DTOs.Responses;
using Acm.Application.Repositories;
using Acm.Application.Services;
using Acm.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Acm.Api.Controllers;

[ApiController]
[Route("api/tenants")]
public class TenantsController : ControllerBase
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IAuthenticationService _authenticationService;

    public TenantsController(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IAuthenticationService authenticationService)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _authenticationService = authenticationService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<TenantResponse>>> RegisterTenant([FromBody] RegisterTenantRequest request)
    {
        try
        {
            // Check if slug is already taken
            var existingTenant = await _tenantRepository.GetBySlugAsync(request.Slug);
            if (existingTenant != null)
            {
                return BadRequest(ApiResponse<TenantResponse>.ErrorResult("Slug is already taken"));
            }

            // Create tenant
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Slug = request.Slug,
                ContactEmail = request.ContactEmail,
                CreatedAt = DateTime.UtcNow
            };

            await _tenantRepository.CreateAsync(tenant);

            // Create admin role for the tenant
            var adminRole = new Role
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = "Admin",
                Description = "Full access to all features",
                CreatedAt = DateTime.UtcNow
            };

            await _roleRepository.CreateAsync(adminRole);

            // Create admin user
            var hashedPassword = await _authenticationService.HashPasswordAsync(request.AdminPassword);
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Email = request.AdminEmail,
                FirstName = request.AdminFirstName,
                LastName = request.AdminLastName,
                PasswordHash = hashedPassword,
                SecurityStamp = Guid.NewGuid().ToString(),
                IsEmailConfirmed = true, // Auto-confirm for admin user
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(adminUser);

            // Assign admin role to admin user
            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            };

            await _userRoleRepository.CreateAsync(userRole);

            var response = new TenantResponse
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Slug = tenant.Slug,
                LogoUrl = tenant.LogoUrl,
                ContactEmail = tenant.ContactEmail,
                CreatedAt = tenant.CreatedAt
            };

            return Ok(ApiResponse<TenantResponse>.SuccessResult(response, "Tenant registered successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<TenantResponse>.ErrorResult("An error occurred during tenant registration"));
        }
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<ApiResponse<TenantResponse>>> GetTenantBySlug([FromRoute] string slug)
    {
        try
        {
            var tenant = await _tenantRepository.GetBySlugAsync(slug);
            if (tenant == null)
            {
                return NotFound(ApiResponse<TenantResponse>.ErrorResult("Tenant not found"));
            }

            var response = new TenantResponse
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Slug = tenant.Slug,
                LogoUrl = tenant.LogoUrl,
                ContactEmail = tenant.ContactEmail,
                CreatedAt = tenant.CreatedAt
            };

            return Ok(ApiResponse<TenantResponse>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<TenantResponse>.ErrorResult("An error occurred while fetching tenant"));
        }
    }

    [HttpGet]
    [Authorize] // Only authenticated users can list all tenants
    public async Task<ActionResult<ApiResponse<IEnumerable<TenantResponse>>>> GetAllTenants()
    {
        try
        {
            var tenants = await _tenantRepository.GetAllAsync();
            var responses = tenants.Select(t => new TenantResponse
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                LogoUrl = t.LogoUrl,
                ContactEmail = t.ContactEmail,
                CreatedAt = t.CreatedAt
            });

            return Ok(ApiResponse<IEnumerable<TenantResponse>>.SuccessResult(responses));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<TenantResponse>>.ErrorResult("An error occurred while fetching tenants"));
        }
    }
}
