using Common.Application.Services;
using Common.Infrastructure.Extensions;
using Microsoft.Extensions.Caching.Distributed;

namespace Common.Infrastructure.Caching;

public class RedisKeyValueCache : IKeyValueCache
{
    private readonly IDistributedCache _distributedCache;

    public RedisKeyValueCache(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task CreateAsync<T>(string key, T data, TimeSpan? absoluteExpireTime = null,
        TimeSpan? slidingExpirationTime = null)
    {
        await _distributedCache.SetRecordAsync(key, data,
            absoluteExpireTime ?? SharedInfrastructureConstants.DefaultAbsoluteExpiration,
            slidingExpirationTime ?? SharedInfrastructureConstants.DefaultSlidingExpiration);
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        return _distributedCache.GetRecordAsync<T>(key, ct);
    }

    public async Task RemoveAsync(string key)
    {
        await _distributedCache.RemoveAsync(key);
    }
}