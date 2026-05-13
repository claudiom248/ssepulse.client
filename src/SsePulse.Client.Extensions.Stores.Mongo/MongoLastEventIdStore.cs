using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.Extensions.Stores.Mongo;

/// <summary>
/// Persists the last event ID to a MongoDB collection so that the SSE connection can be resumed
/// after a process restart.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/last-event-id-stores.html"/>
/// </summary>
/// <remarks>
/// <para>
/// The document is looked up by <see cref="MongoLastEventIdStoreOptions.DocumentKey"/> and
/// upserted (insert or update) on every <see cref="Set"/> call, ensuring exactly one document
/// per key is kept in the collection.
/// </para>
/// <para>
/// If MongoDB is unavailable, the store falls back to the in-memory value only: the error is
/// logged at <c>Error</c> level but is never surfaced to the caller, so SSE event processing
/// continues uninterrupted.
/// </para>
/// </remarks>
public sealed class MongoLastEventIdStore : ILastEventIdStore
{
    private readonly MongoLastEventIdStoreOptions _options;
    private readonly ILogger<MongoLastEventIdStore> _logger;
    private readonly IMongoCollection<LastEventIdDocument> _collection;

    /// <inheritdoc/>
    public string? LastEventId { get; private set; }

    /// <summary>
    /// Initializes a new instance of <see cref="MongoLastEventIdStore"/>.
    /// The constructor immediately attempts to read the persisted last-event-ID from MongoDB;
    /// if MongoDB is unavailable the error is logged and <see cref="LastEventId"/> remains
    /// <see langword="null"/>.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/last-event-id-stores.html"/>
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
        LastEventId = TryGetLastEventId();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Empty or whitespace values are silently ignored. If the upsert operation fails, the error
    /// is logged and the in-memory <see cref="LastEventId"/> is still updated so the caller is
    /// not affected.
    /// </remarks>
    public void Set(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        FilterDefinition<LastEventIdDocument>? filter =
            Builders<LastEventIdDocument>.Filter.Eq(x => x.Id, _options.DocumentKey);
        UpdateDefinition<LastEventIdDocument>? update = Builders<LastEventIdDocument>.Update
            .Set(x => x.LastEventId, eventId)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        try
        {
            _collection.UpdateOne(filter, update, new UpdateOptions { IsUpsert = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating document with key '{DocumentKey}'", _options.DocumentKey);
        }

        LastEventId = eventId;
    }

    private string? TryGetLastEventId()
    {
        FilterDefinition<LastEventIdDocument>? filter =
            Builders<LastEventIdDocument>.Filter.Eq(x => x.Id, _options.DocumentKey);

        try
        {
            string? id = _collection
                .Find(filter)
                .Project(d => d.LastEventId)
                .FirstOrDefault();
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving document with with key '{DocumentKey}'", _options.DocumentKey);
            return null;
        }
    }
}