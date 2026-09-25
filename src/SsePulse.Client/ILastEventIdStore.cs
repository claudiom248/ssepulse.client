namespace SsePulse.Client;

/// <summary>
/// Represents a store for the last event ID associated with a Server-Sent Events (SSE) source.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/last-event-id.html"/>
/// </summary>
/// <remarks>
/// The source stores an event ID only after every handler of that event completed, so an event is delivered
/// again after a crash in the middle of a handler (at-least-once). Implementations must not perform I/O in
/// their constructors: the persisted value is loaded lazily by <see cref="GetLastEventIdAsync"/>.
/// The value held in memory is authoritative for the running process: a persistence error is logged, never
/// thrown, and the next call to <see cref="SetLastEventIdAsync"/> persists the newest value again.
/// </remarks>
public interface ILastEventIdStore
{
    /// <summary>
    /// Gets the identifier of the last event stored. It is used to resume an SSE connection from the last
    /// processed event, so that the client does not receive events it already handled.
    /// </summary>
    /// <param name="cancellationToken">Token that cancels the read.</param>
    /// <returns>The last event ID, or <see langword="null"/> when none has been stored.</returns>
    ValueTask<string?> GetLastEventIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last event ID stored. Empty and whitespace values are ignored.
    /// </summary>
    /// <param name="eventId">The event ID to be stored.</param>
    /// <param name="cancellationToken">Token that cancels the write.</param>
    ValueTask SetLastEventIdAsync(string eventId, CancellationToken cancellationToken = default);
}
