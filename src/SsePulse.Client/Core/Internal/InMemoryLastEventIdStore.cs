using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.Core.Internal;

internal class InMemoryLastEventIdStore : ILastEventIdStore
{
    public string? LastEventId { get; private set; }

    public void Set(string eventId)
    {
        if (!string.IsNullOrWhiteSpace(eventId))
        {
            LastEventId = eventId;
        }
    }
}

