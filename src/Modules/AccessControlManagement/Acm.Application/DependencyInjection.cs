using Acm.Application.Services;
using Acm.Application.Services.Implementations;
using Acm.Application.Services.Interfaces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Acm.Application;

public static class DependencyInjection
{
    public static IServiceCollection RegisterSecurityManagementApplicationServices(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining(typeof(DependencyInjection));
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}
