using Common.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCommonApplicationServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(LazyService<>), typeof(LazyService<>));
        return services;
    }
}
