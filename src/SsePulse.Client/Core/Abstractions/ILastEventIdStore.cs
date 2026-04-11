namespace SsePulse.Client.Core.Abstractions;

/// <summary>
/// Represents a store for the last event ID associated with a Server-Sent Events (SSE) source.
/// </summary>
public interface ILastEventIdStore
{
    /// <summary>
    /// Gets the identifier of the last event received from an SSE (Server-Sent Events) stream.
    /// This property is typically used to resume an SSE connection from the last received event,
    /// allowing the client to avoid processing duplicate or already-received events.
    /// </summary>
    string? LastEventId { get; }

    /// <summary>
    /// Updates the last event ID stored in the implementation.
    /// </summary>
    /// <param name="eventId">The event ID to be stored.</param>
    void Set(string eventId);
}

