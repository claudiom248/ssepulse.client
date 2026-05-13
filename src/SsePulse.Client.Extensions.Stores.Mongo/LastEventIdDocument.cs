using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SsePulse.Client.Extensions.Stores.Mongo;

/// <summary>
/// MongoDB document that stores the last received SSE event ID for a given source.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/last-event-id-stores.html"/>
/// </summary>
/// <remarks>
/// This type is used as the BSON schema type for the collection. Instances are constructed
/// and populated by the MongoDB driver deserialiser; application code never creates them
/// directly.
/// </remarks>
public sealed class LastEventIdDocument
{
    /// <summary>
    /// Gets or sets the document identifier (<c>_id</c> field in MongoDB).
    /// The value corresponds to <see cref="MongoLastEventIdStoreOptions.DocumentKey"/>
    /// and is used to distinguish last-event-ID documents belonging to different SSE sources
    /// within the same collection.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = null!;

    /// <summary>Gets or sets the persisted last-event-ID value.</summary>
    [BsonElement("lastEventId")]
    public string LastEventId { get; set; } = null!;

    /// <summary>Gets or sets the UTC timestamp of the last update.</summary>
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}