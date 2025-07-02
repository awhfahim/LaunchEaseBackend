namespace Common.Application.Services;

public interface IKeyValueCache
{
    Task CreateAsync<T>(string key, T data, TimeSpan? absoluteExpireTime = null,
        TimeSpan? slidingExpirationTime = null);

    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task RemoveAsync(string key);
}