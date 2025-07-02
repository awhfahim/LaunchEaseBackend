using Microsoft.Extensions.DependencyInjection;

namespace Common.Application.Services;

public class LazyService<T>(IServiceProvider provider) : Lazy<T>(provider.GetRequiredService<T>)
    where T : notnull;