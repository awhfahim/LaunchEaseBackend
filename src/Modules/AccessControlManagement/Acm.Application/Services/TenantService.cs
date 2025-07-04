using Acm.Application.DataTransferObjects;
using Acm.Application.Repositories;
using Acm.Domain.Entities;
using Common.Application.Providers;
using Microsoft.Extensions.Logging;

namespace Acm.Application.Services;

public class TenantService : ITenantService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IGuidProvider _guidProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<TenantService> _logger;

    public TenantService(ITenantRepository tenantRepository, IGuidProvider guidProvider,
        IDateTimeProvider dateTimeProvider, IAuthenticationService authenticationService,
        ILogger<TenantService> logger)
    {
        _tenantRepository = tenantRepository;
        _guidProvider = guidProvider;
        _dateTimeProvider = dateTimeProvider;
        _authenticationService = authenticationService;
        _logger = logger;
    }

    public async Task<TenantResponse?> RegisterTenantAsync(RegisterTenantRequest request,
        CancellationToken cancellationToken)
    {
        var existingTenant = await _tenantRepository.GetBySlugAsync(request.Slug, cancellationToken);
        if (existingTenant != null)
        {
            return null;
        }

        var tenant = new Tenant
        {
            Id = _guidProvider.SortableGuid(),
            Name = request.Name,
            Slug = request.Slug,
            ContactEmail = request.ContactEmail,
            CreatedAt = _dateTimeProvider.CurrentUtcTime
        };

        var adminRole = new Role
        {
            Id = _guidProvider.SortableGuid(),
            TenantId = tenant.Id,
            Name = "TenantAdmin",
            Description = "Full access to all features",
            CreatedAt = _dateTimeProvider.CurrentUtcTime
        };

        var hashedPassword = await _authenticationService.HashPasswordAsync(request.AdminPassword);
        var adminUser = new User
        {
            Id = _guidProvider.SortableGuid(),
            Email = request.AdminEmail,
            FirstName = request.AdminFirstName,
            LastName = request.AdminLastName,
            PasswordHash = hashedPassword,
            SecurityStamp = _guidProvider.RandomGuid().ToString(),
            IsEmailConfirmed = true, // Auto-confirm for admin user
            CreatedAt = _dateTimeProvider.CurrentUtcTime
        };

        var userRole = new UserRole
        {
            Id = _guidProvider.SortableGuid(),
            UserId = adminUser.Id,
            RoleId = adminRole.Id,
            TenantId = tenant.Id
        };

        await _tenantRepository.CreateAsync(tenant, adminRole, adminUser, userRole, _guidProvider.SortableGuid());

        var response = new TenantResponse
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            LogoUrl = tenant.LogoUrl,
            ContactEmail = tenant.ContactEmail,
            CreatedAt = tenant.CreatedAt
        };

        return response;
    }

    public Task DeleteTenantAsync(Guid id, CancellationToken ct)
    {
        return _tenantRepository.DeleteAsync(id, ct);
    }
}