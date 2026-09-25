using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.Extensions.Stores.DistributedCache;

/// <summary>
/// Persists the last event ID to a distributed cache so that the SSE connection can be resumed
/// after a process restart.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/store-distributed-cache.html"/>
/// </summary>
/// <remarks>
/// <para>
/// The last event ID is written to the configured <see cref="IDistributedCache"/> key on every
/// <see cref="Set"/> call and read back from the cache when the store is constructed, allowing
/// the SSE stream to resume after a process restart.
/// </para>
/// <para>
/// If the cache is unavailable at construction time, <see cref="LastEventId"/> is initialised
/// to <see langword="null"/>. If a <see cref="Set"/> call fails, the error is logged at
/// <c>Error</c> level and <see cref="LastEventId"/> is <b>not</b> updated — SSE processing
/// continues uninterrupted but the failed write is not retried.
/// </para>
/// </remarks>
public class DistributedCacheLastEventIdStore : ILastEventIdStore
{
    private readonly DistributedCacheLastEventIdStoreOptions _options;
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheLastEventIdStore> _logger;

    /// <inheritdoc/>
    public string? LastEventId { get; private set; }

    /// <summary>
    /// Initializes a new instance of <see cref="DistributedCacheLastEventIdStore"/>.
    /// The constructor immediately attempts to read the persisted last-event-ID from the cache;
    /// if the cache is unavailable the error is logged and <see cref="LastEventId"/> remains
    /// <see langword="null"/>.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/store-distributed-cache.html"/>
    /// </summary>
    /// <param name="options">Configuration options for the store.</param>
    /// <param name="cache">The distributed cache used to persist the last event ID.</param>
    /// <param name="logger">Logger used to report errors. Falls back to <see cref="NullLogger{T}"/> when <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="cache"/> is
    /// <see langword="null"/>.
    /// </exception>
    public DistributedCacheLastEventIdStore(
        DistributedCacheLastEventIdStoreOptions options,
        IDistributedCache cache,
        ILogger<DistributedCacheLastEventIdStore>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? NullLogger<DistributedCacheLastEventIdStore>.Instance;

        LastEventId = TryGetLastEventId();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Empty or whitespace values are silently ignored. If the write to the distributed cache
    /// fails, the error is logged and <see cref="LastEventId"/> is <b>not</b> updated — the
    /// caller is not affected and SSE processing continues uninterrupted.
    /// </remarks>
    public void Set(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        try
        {
            DistributedCacheEntryOptions entryOptions = new()
            {
                AbsoluteExpirationRelativeToNow = _options.AbsoluteExpirationRelativeToNow
            };

            _cache.SetString(_options.Key, eventId, entryOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist last event ID with key '{Key}'", _options.Key);
            return;
        }
        
        LastEventId = eventId;
    }
    
    private string? TryGetLastEventId()
    {
        try
        {
            return _cache.GetString(_options.Key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve last event ID with key '{Key}'", _options.Key);
            return null;
        }
    }
}