using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;

namespace SsePulse.Client.Tests.Common;

public sealed class InMemoryDistributedCache : IDistributedCache
{
    private readonly ConcurrentDictionary<string, byte[]> _entries = new();

    public byte[]? Get(string key) => _entries.TryGetValue(key, out byte[]? value) ? value : null;

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _entries[key] = value;

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }

    public void Refresh(string key)
    {
    }

    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

    public void Remove(string key) => _entries.TryRemove(key, out _);

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }
}
