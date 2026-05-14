using Microsoft.Extensions.Caching.Distributed;

namespace SsePulse.Client.Extensions.Stores.DistributedCache;

/// <summary>
/// Configuration options for <see cref="DistributedCacheLastEventIdStore"/>.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/store-distributed-cache.html"/>
/// </summary>
public sealed class DistributedCacheLastEventIdStoreOptions
{
    /// <summary>
    /// Gets or sets the cache key under which the last event ID is stored.
    /// Defaults to <c>ssepulse.client.lastEventId</c>. Use a unique key per SSE source when
    /// multiple sources share the same distributed cache instance.
    /// </summary>
    public string Key { get; set; } = "ssepulse.client.lastEventId";

    /// <summary>
    /// Gets or sets the absolute expiration time for each cached entry, relative to now.
    /// When <see langword="null"/> (the default) the entry has no expiration and persists
    /// until it is explicitly evicted by the cache backend.
    /// </summary>
    /// <remarks>
    /// Setting a TTL is useful when you want stale last-event-ID values to expire automatically,
    /// for example after a deployment that resets the event stream. The value is forwarded to
    /// <see cref="DistributedCacheEntryOptions.AbsoluteExpirationRelativeToNow"/> on every
    /// <see cref="DistributedCacheLastEventIdStore.Set"/> call.
    /// </remarks>
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
}