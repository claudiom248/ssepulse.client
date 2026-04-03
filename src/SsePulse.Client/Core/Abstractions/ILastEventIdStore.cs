namespace SsePulse.Client.Core.Abstractions;

public interface ILastEventIdStore
{
    string? LastEventId { get; }

    void Set(string eventId);
}

