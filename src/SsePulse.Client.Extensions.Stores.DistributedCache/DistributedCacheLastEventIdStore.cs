using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SsePulse.Client;

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
/// <see cref="SetLastEventIdAsync"/> call and read back from the cache by the first <see cref="GetLastEventIdAsync"/>
/// call, allowing the SSE stream to resume after a process restart. The constructor performs no I/O.
/// </para>
/// <para>
/// If the cache is unavailable, the value held in memory stays authoritative: the error is logged at
/// <c>Error</c> level, SSE processing continues uninterrupted, and the next <see cref="SetLastEventIdAsync"/>
/// call persists the newest value again.
/// </para>
/// </remarks>
public class DistributedCacheLastEventIdStore : ILastEventIdStore
{
    private readonly DistributedCacheLastEventIdStoreOptions _options;
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheLastEventIdStore> _logger;
    private volatile string? _lastEventId;
    private int _loaded;

    /// <summary>
    /// Initializes a new instance of <see cref="DistributedCacheLastEventIdStore"/>.
    /// The constructor performs no I/O: the persisted last-event-ID is read from the cache by the first call to
    /// <see cref="GetLastEventIdAsync"/>.
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
    }

    /// <inheritdoc/>
    public async ValueTask<string?> GetLastEventIdAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _loaded) == 0)
        {
            try
            {
                string? persisted = await _cache.GetStringAsync(_options.Key, cancellationToken).ConfigureAwait(false);
                _lastEventId ??= persisted;
                Volatile.Write(ref _loaded, 1);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Failed to retrieve last event ID with key '{Key}'", _options.Key);
            }
        }

        return _lastEventId;
    }

    /// <inheritdoc/>
    public async ValueTask SetLastEventIdAsync(string eventId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        _lastEventId = eventId;
        try
        {
            DistributedCacheEntryOptions entryOptions = new()
            {
                AbsoluteExpirationRelativeToNow = _options.AbsoluteExpirationRelativeToNow
            };

            await _cache.SetStringAsync(_options.Key, eventId, entryOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Failed to persist last event ID with key '{Key}'", _options.Key);
        }
    }
}
