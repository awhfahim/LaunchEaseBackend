using System.Collections.Concurrent;
using Common.Domain.CoreProviders;

namespace Common.Infrastructure.Providers;

public class ReflectionCacheProvider : IReflectionCacheProvider
{
    public ConcurrentDictionary<string, IReadOnlyCollection<(string PropertyName, Type DataType)>>
        DynamicLinqCache { get; } = new();
}