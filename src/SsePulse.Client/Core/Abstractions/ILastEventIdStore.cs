namespace SsePulse.Client.Core.Abstractions;

internal interface ILastEventIdStore
{
    string? LastEventId { get; }

    void Set(string eventId);
}

