using Acm.Api.DTOs.Responses;
using Acm.Application.DataTransferObjects;
using Acm.Application.Repositories;
using Acm.Application.Services;
using Acm.Infrastructure.Authorization;
using Acm.Infrastructure.Authorization.Attributes;
using Common.Application.Data;
using Common.Application.Providers;
using Common.HttpApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Acm.Api.Controllers;

[Route("api/tenants")]
public class TenantsController : JsonApiControllerBase
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantService _tenantService;

    public TenantsController(
        ITenantRepository tenantRepository, ITenantService tenantService)
    {
        _tenantRepository = tenantRepository;
        _tenantService = tenantService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterTenant([FromBody] RegisterTenantRequest request)
    {
        var result = await _tenantService.RegisterTenantAsync(request, HttpContext.RequestAborted);
        if (result == null)
        {
            return BadRequest(ApiResponse<TenantResponse>.ErrorResult("Slug is already taken"));
        }
        
        return Ok(ApiResponse<TenantResponse>.SuccessResult(result, "Tenant registered successfully"));
    }

    [HttpGet("{slug}")]
    [RequirePermission(PermissionConstants.TenantSettingsView)] // Tenant users can view their own tenant
    public async Task<IActionResult> GetTenantBySlug([FromRoute, BindRequired] string slug)
    {
        try
        {
            var tenant = await _tenantRepository.GetBySlugAsync(slug);
            if (tenant == null)
            {
                return NotFound(ApiResponse<TenantResponse>.ErrorResult("Tenant not found"));
            }

            // Check if user has permission to access this specific tenant
            if (!await CanAccessTenant(tenant.Id))
            {
                return Forbid(); // This will be handled by the global exception handler
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
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<TenantResponse>.ErrorResult("An error occurred while fetching tenant"));
        }
    }

    [HttpGet]
    [RequirePermission(PermissionConstants.GlobalTenantsView)] // Only business owner/system admin can list all tenants
    public async Task<IActionResult> GetAllTenants()
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
        catch (Exception)
        {
            return StatusCode(500,
                ApiResponse<IEnumerable<TenantResponse>>.ErrorResult("An error occurred while fetching tenants"));
        }
    }

    private Task<bool> CanAccessTenant(Guid tenantId)
    {
        // Get current user's permissions
        var userPermissions = User.FindAll("permission").Select(c => c.Value).ToList();
        
        // Business owner and system admin can access any tenant
        if (userPermissions.Contains(PermissionConstants.BusinessOwner) ||
            userPermissions.Contains(PermissionConstants.SystemAdmin) ||
            userPermissions.Contains(PermissionConstants.CrossTenantAccess))
        {
            return Task.FromResult(true);
        }

        // Regular tenant users can only access their own tenant
        var currentTenantId = GetTenantId();
        return Task.FromResult(currentTenantId == tenantId);
    }
}