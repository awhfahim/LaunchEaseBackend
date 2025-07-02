using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using static Common.Infrastructure.SharedInfrastructureConstants;

namespace Common.Infrastructure.Extensions;

public static class DistributedCacheExtensions
{
    public static async Task SetRecordAsync<T>(this IDistributedCache cache,
        string key, T data, TimeSpan? absoluteExpireTime = null, TimeSpan? slidingExpirationTime = null)
    {
        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = slidingExpirationTime ?? DefaultSlidingExpiration,
            AbsoluteExpirationRelativeToNow = absoluteExpireTime ?? DefaultAbsoluteExpiration
        };

        var jsonData = JsonSerializer.Serialize(data);
        await cache.SetStringAsync(key, jsonData, options);
    }

    public static async Task<T?> GetRecordAsync<T>(this IDistributedCache cache, string key,
        CancellationToken ct = default)
    {
        var jsonData = await cache.GetStringAsync(key, ct);
        return jsonData is null ? default : JsonSerializer.Deserialize<T>(jsonData);
    }
}