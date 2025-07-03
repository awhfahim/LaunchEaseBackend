using Acm.Api.DTOs.Requests;
using Acm.Api.DTOs.Responses;
using Acm.Application.Repositories;
using Acm.Application.Services;
using Acm.Domain.Entities;
using Common.Application.Data;
using Common.Application.Providers;
using Common.HttpApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Acm.Api.Controllers;

[Route("api/tenants")]
public class TenantsController : JsonApiControllerBase
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IAuthenticationService _authenticationService;
    private readonly IGuidProvider _guidProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IDbConnectionFactory _connectionFactory;

    public TenantsController(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IAuthenticationService authenticationService, 
        IGuidProvider guidProvider, 
        IDateTimeProvider dateTimeProvider,
        IDbConnectionFactory connectionFactory)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _authenticationService = authenticationService;
        _guidProvider = guidProvider;
        _dateTimeProvider = dateTimeProvider;
        _connectionFactory = connectionFactory;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterTenant([FromBody] RegisterTenantRequest request)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        
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
                Id = _guidProvider.SortableGuid(),
                Name = request.Name,
                Slug = request.Slug,
                ContactEmail = request.ContactEmail,
                CreatedAt = _dateTimeProvider.CurrentUtcTime
            };

            await _tenantRepository.CreateAsync(tenant, transaction);

            // Create admin role for the tenant
            var adminRole = new Role
            {
                Id = _guidProvider.SortableGuid(),
                TenantId = tenant.Id,
                Name = "Admin",
                Description = "Full access to all features",
                CreatedAt = _dateTimeProvider.CurrentUtcTime
            };

            await _roleRepository.CreateAsync(adminRole, transaction);

            // Create admin user
            var hashedPassword = await _authenticationService.HashPasswordAsync(request.AdminPassword);
            var adminUser = new User
            {
                Id = _guidProvider.SortableGuid(),
                TenantId = tenant.Id,
                Email = request.AdminEmail,
                FirstName = request.AdminFirstName,
                LastName = request.AdminLastName,
                PasswordHash = hashedPassword,
                SecurityStamp = _guidProvider.RandomGuid().ToString(),
                IsEmailConfirmed = true, // Auto-confirm for admin user
                CreatedAt = _dateTimeProvider.CurrentUtcTime
            };

            await _userRepository.CreateAsync(adminUser, transaction);

            // Assign admin role to admin user
            var userRole = new UserRole
            {
                Id = _guidProvider.SortableGuid(),
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            };

            await _userRoleRepository.CreateAsync(userRole, transaction);

            var response = new TenantResponse
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Slug = tenant.Slug,
                LogoUrl = tenant.LogoUrl,
                ContactEmail = tenant.ContactEmail,
                CreatedAt = tenant.CreatedAt
            };

            await transaction.CommitAsync();

            return Ok(ApiResponse<TenantResponse>.SuccessResult(response, "Tenant registered successfully"));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500,
                ApiResponse<TenantResponse>.ErrorResult("An error occurred during tenant registration"));
        }
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetTenantBySlug([FromRoute, BindRequired] string slug)
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
        catch (Exception ex)
        {
            return StatusCode(500,
                ApiResponse<IEnumerable<TenantResponse>>.ErrorResult("An error occurred while fetching tenants"));
        }
    }
}