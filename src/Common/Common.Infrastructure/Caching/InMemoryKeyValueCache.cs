using Common.Application.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Common.Infrastructure.Caching;

public class InMemoryKeyValueCache : IKeyValueCache
{
    private readonly IMemoryCache _memoryCache;

    public InMemoryKeyValueCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task CreateAsync<T>(string key, T data, TimeSpan? absoluteExpireTime = null,
        TimeSpan? slidingExpirationTime = null)
    {
        var absoluteExpiration = absoluteExpireTime;
        var slidingExpiration = slidingExpirationTime;

        if (slidingExpiration is null)
        {
            slidingExpiration = SharedInfrastructureConstants.DefaultSlidingExpiration;
        }

        if (absoluteExpiration is null)
        {
            absoluteExpiration = SharedInfrastructureConstants.DefaultAbsoluteExpiration;
        }

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(slidingExpiration.Value)
            .SetAbsoluteExpiration(absoluteExpiration.Value);

        _memoryCache.Set(key, data, cacheEntryOptions);

        return Task.CompletedTask;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        _memoryCache.TryGetValue(key, out T? result);
        return Task.FromResult(result);
    }

    public Task RemoveAsync(string key)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }
}
