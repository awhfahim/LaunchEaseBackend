using Acm.Application.Services;
using Acm.Application.Services.RoleServices;
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
        return services;
    }
}
