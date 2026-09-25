namespace SsePulse.Client.Internal;

internal sealed class EventIdCommitTracker
{
    private readonly object _lock = new();
    private readonly Dictionary<long, string?> _registered = [];
    private readonly HashSet<long> _completed = [];
    private long _next;
    private long _nextToCommit;
    private long _version;

    public long Register(string? eventId)
    {
        lock (_lock)
        {
            long sequence = _next++;
            _registered[sequence] = string.IsNullOrWhiteSpace(eventId) ? null : eventId;
            return sequence;
        }
    }

    public EventIdAdvance? Complete(long sequence)
    {
        lock (_lock)
        {
            _completed.Add(sequence);
            string? lastId = null;
            while (_completed.Remove(_nextToCommit))
            {
                if (_registered.Remove(_nextToCommit, out string? id) && id is not null)
                {
                    lastId = id;
                }

                _nextToCommit++;
            }

            return lastId is null ? null : new EventIdAdvance(++_version, lastId);
        }
    }
}

internal readonly record struct EventIdAdvance(long Version, string EventId);
