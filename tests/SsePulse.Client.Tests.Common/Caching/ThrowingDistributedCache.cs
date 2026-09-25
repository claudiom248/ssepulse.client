using Microsoft.Extensions.Caching.Distributed;

namespace SsePulse.Client.Tests.Common;

public sealed class ThrowingDistributedCache : IDistributedCache
{
    private static InvalidOperationException Failure() => new("The distributed cache is unavailable.");

    public byte[]? Get(string key) => throw Failure();

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => throw Failure();

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => throw Failure();

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => throw Failure();

    public void Refresh(string key) => throw Failure();

    public Task RefreshAsync(string key, CancellationToken token = default) => throw Failure();

    public void Remove(string key) => throw Failure();

    public Task RemoveAsync(string key, CancellationToken token = default) => throw Failure();
}
