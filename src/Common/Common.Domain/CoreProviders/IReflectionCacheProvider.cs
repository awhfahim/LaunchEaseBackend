using System.Collections.Concurrent;

namespace Common.Domain.CoreProviders;

public interface IReflectionCacheProvider
{
    public ConcurrentDictionary<string, IReadOnlyCollection<(string PropertyName, Type DataType)>>
        DynamicLinqCache { get; }
}