using Acm.Application.Services;
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
        return services;
    }
}
