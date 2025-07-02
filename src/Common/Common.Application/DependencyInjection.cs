using Microsoft.Extensions.DependencyInjection;

namespace Common.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCommonApplicationServices(this IServiceCollection services)
    {
        return services;
    }
}
