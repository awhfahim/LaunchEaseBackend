using Acm.Application;
using Acm.Application.Features.AccessControlFeatures.Interfaces;
using Acm.Application.Providers;
using Acm.Application.Repositories;
using Acm.Application.Services;
using Acm.Domain.Entities;
using Acm.Infrastructure.Authorization;
using Acm.Infrastructure.Authorization.Handlers;
using Acm.Infrastructure.Identity.Stores;
using Acm.Infrastructure.Misc;
using Acm.Infrastructure.Persistence;
using Acm.Infrastructure.Persistence.Repositories;
using Acm.Infrastructure.Providers;
using Acm.Infrastructure.Services;
using Common.Application.Services;
using Common.Infrastructure.Caching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Acm.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection RegisterSecurityManagementInfrastructureServices(
        this IServiceCollection services)
    {
        services.TryAddScoped<IKeyValueCache, InMemoryKeyValueCache>();
        services.TryAddSingleton<IJwtProvider, JwtProvider>();

        // Authorization
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, TenantAuthorizationHandler>();

        services.TryAddSingleton<IAuthCryptographyService, AuthCryptographyService>();

        // Data Access Layer - Repositories
        services.TryAddScoped<IUserRepository, UserRepository>();
        services.TryAddScoped<IRoleRepository, RoleRepository>();
        services.TryAddScoped<IUserRoleRepository, UserRoleRepository>();
        services.TryAddScoped<IUserClaimRepository, UserClaimRepository>();
        services.TryAddScoped<IRoleClaimRepository, RoleClaimRepository>();
        services.TryAddScoped<ITenantRepository, TenantRepository>();

        // Identity Stores
        services.TryAddScoped<IUserStore<User>, UserStore>();
        services.TryAddScoped<IRoleStore<Role>, RoleStore>();

        // Services
        services.TryAddScoped<IAuthenticationService, AuthenticationService>();

        services.TryAddScoped<IAcmAppUnitOfWork, AcmAppUnitOfWork>();

        return services;
    }
}
