using Acm.Application;
using Acm.Application.Interfaces;
using Acm.Application.Repositories;
using Acm.Application.Services;
using Acm.Infrastructure.Authorization;
using Acm.Infrastructure.Authorization.Handlers;
using Acm.Infrastructure.Extensions;
using Acm.Infrastructure.Persistence;
using Acm.Infrastructure.Persistence.Repositories;
using Acm.Infrastructure.Services;
using Common.Application.Services;
using Common.Infrastructure.Caching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Acm.Infrastructure;

public static class DependencyInjection
{
    public static async Task<IServiceCollection> RegisterSecurityManagementInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<IKeyValueCache, InMemoryKeyValueCache>();

        // Authorization
        services.AddSingleton<IAuthorizationHandler, TenantAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // Data Access Layer - Repositories
        services.TryAddScoped<IUserRepository, EnhancedUserRepository>();
        services.TryAddScoped<IRoleRepository, RoleRepository>();
        services.TryAddScoped<IUserRoleRepository, UserRoleRepository>();
        services.TryAddScoped<IUserClaimRepository, UserClaimRepository>();
        services.TryAddScoped<IRoleClaimRepository, RoleClaimRepository>();
        services.TryAddScoped<ITenantRepository, TenantRepository>();
        services.TryAddScoped<IUserTenantRepository, UserTenantRepository>();
        
        // Enhanced Unit of Work for ACM
        services.TryAddScoped<IAcmUnitOfWork, AcmUnitOfWork>();
        
        // Services
        services.TryAddScoped<UserService>();

        // Services
        services.TryAddScoped<IAuthenticationService, AuthenticationService>();
        await services.SeedPermissionsAsync(configuration);

        services.TryAddScoped<IAcmAppUnitOfWork, AcmAppUnitOfWork>();
        services.TryAddScoped<IRoleManagementRepository, RoleManagementRepository>();
        services.TryAddScoped<IPermissionManagementRepository, PermissionManagementRepository>();

        services.AddJwtAuth(configuration);
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        return services;
    }
}
