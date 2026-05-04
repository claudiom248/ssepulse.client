using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.Core;

/// <summary>
/// Stores the last event ID received from the server.
/// This implementation keeps the last event ID in memory, which means it will be lost if the application is restarted.
/// It is suitable for scenarios where persistence across sessions is not required.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/last-event-id.html"/>
/// </summary>
public class InMemoryLastEventIdStore : ILastEventIdStore
{
    /// <inheritdoc/>
    public string? LastEventId { get; private set; }

    /// <inheritdoc/>
    public void Set(string eventId)
    {
        if (!string.IsNullOrWhiteSpace(eventId))
        {
            LastEventId = eventId;
        }
    }
}

