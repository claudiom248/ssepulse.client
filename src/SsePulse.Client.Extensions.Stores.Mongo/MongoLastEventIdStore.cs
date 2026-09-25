using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using SsePulse.Client;

namespace SsePulse.Client.Extensions.Stores.Mongo;

/// <summary>
/// Persists the last event ID to a MongoDB collection so that the SSE connection can be resumed
/// after a process restart.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/store-mongo.html"/>
/// </summary>
/// <remarks>
/// <para>
/// The document is looked up by <see cref="MongoLastEventIdStoreOptions.DocumentKey"/> and
/// upserted (insert or update) on every <see cref="SetLastEventIdAsync"/> call, ensuring exactly one document
/// per key is kept in the collection.
/// </para>
/// <para>
/// The persisted value is read lazily by the first <see cref="GetLastEventIdAsync"/> call; the constructor
/// performs no I/O.
/// </para>
/// <para>
/// If MongoDB is unavailable, the value held in memory stays authoritative: the error is
/// logged at <c>Error</c> level but is never surfaced to the caller, so SSE event processing
/// continues uninterrupted. The next <see cref="SetLastEventIdAsync"/> call persists the newest value again.
/// </para>
/// </remarks>
public sealed class MongoLastEventIdStore : ILastEventIdStore
{
    private readonly MongoLastEventIdStoreOptions _options;
    private readonly ILogger<MongoLastEventIdStore> _logger;
    private readonly IMongoCollection<LastEventIdDocument> _collection;
    private volatile string? _lastEventId;
    private int _loaded;

    /// <summary>
    /// Initializes a new instance of <see cref="MongoLastEventIdStore"/>.
    /// The constructor performs no I/O: the persisted last-event-ID is read from MongoDB by the first call to
    /// <see cref="GetLastEventIdAsync"/>.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/store-mongo.html"/>
    /// </summary>
    /// <param name="options">Configuration options for the store.</param>
    /// <param name="mongoClient">The MongoDB client used to access the database.</param>
    /// <param name="logger">
    /// Optional logger. Falls back to <see cref="NullLogger{T}"/> when <see langword="null"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="mongoClient"/> is
    /// <see langword="null"/>.
    /// </exception>
    public MongoLastEventIdStore(MongoLastEventIdStoreOptions options, IMongoClient mongoClient, ILogger<MongoLastEventIdStore>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<MongoLastEventIdStore>.Instance;
        if (mongoClient is null)
        {
            throw new ArgumentNullException(nameof(mongoClient));
        }
        IMongoDatabase database = mongoClient
            .GetDatabase(options.DatabaseName);
        _collection = database.GetCollection<LastEventIdDocument>(options.CollectionName);
    }

    /// <inheritdoc/>
    public async ValueTask<string?> GetLastEventIdAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _loaded) == 0)
        {
            FilterDefinition<LastEventIdDocument> filter =
                Builders<LastEventIdDocument>.Filter.Eq(x => x.Id, _options.DocumentKey);
            try
            {
                string? persisted = await _collection
                    .Find(filter)
                    .Project(d => d.LastEventId)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
                _lastEventId ??= persisted;
                Volatile.Write(ref _loaded, 1);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error while retrieving document with with key '{DocumentKey}'", _options.DocumentKey);
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
        FilterDefinition<LastEventIdDocument> filter =
            Builders<LastEventIdDocument>.Filter.Eq(x => x.Id, _options.DocumentKey);
        UpdateDefinition<LastEventIdDocument> update = Builders<LastEventIdDocument>.Update
            .Set(x => x.LastEventId, eventId)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        try
        {
            await _collection
                .UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Error while updating document with key '{DocumentKey}'", _options.DocumentKey);
        }
    }
}
