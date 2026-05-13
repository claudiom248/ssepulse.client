namespace SsePulse.Client.Extensions.Stores.Mongo;

/// <summary>
/// Configuration options for <see cref="MongoLastEventIdStore"/>.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/last-event-id-stores.html"/>
/// </summary>
public sealed class MongoLastEventIdStoreOptions
{
    /// <summary>
    /// Gets or sets the name of the MongoDB database that contains the last-event-ID collection.
    /// This property is required and must be set before the store is created.
    /// </summary>
    public string DatabaseName { get; set; } = null!;

    /// <summary>Gets or sets the name of the collection used to persist last-event-IDs.
    /// Defaults to <c>sse_last_event_ids</c>.</summary>
    public string CollectionName { get; set; } = "sse_last_event_ids";

    /// <summary>Gets or sets the document key (the <c>_id</c> field value) used to identify the
    /// last-event-ID document for this SSE source. Defaults to <c>DefaultSseSource</c>.</summary>
    public string? DocumentKey { get; set; } = "DefaultSseSource";
}