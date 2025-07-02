using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Acm.Application;

public static class DependencyInjection
{
    public static IServiceCollection RegisterSecurityManagementApplicationServices(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining(typeof(DependencyInjection));
        return services;
    }
}
