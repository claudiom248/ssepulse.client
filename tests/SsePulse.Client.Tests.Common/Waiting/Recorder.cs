namespace SsePulse.Client.Tests.Common;

public sealed class Recorder<T>
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly object _lock = new();
    private readonly List<T> _items = [];
    private readonly List<(int Count, TaskCompletionSource Signal)> _waiters = [];

    public IReadOnlyList<T> Items
    {
        get
        {
            lock (_lock)
            {
                return _items.ToArray();
            }
        }
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _items.Count;
            }
        }
    }

    public void Add(T item)
    {
        List<TaskCompletionSource> ready = [];
        lock (_lock)
        {
            _items.Add(item);
            for (int i = _waiters.Count - 1; i >= 0; i--)
            {
                if (_waiters[i].Count <= _items.Count)
                {
                    ready.Add(_waiters[i].Signal);
                    _waiters.RemoveAt(i);
                }
            }
        }

        foreach (TaskCompletionSource signal in ready)
        {
            signal.TrySetResult();
        }
    }

    public async Task WaitForCountAsync(int count, TimeSpan? timeout = null)
    {
        Task wait;
        lock (_lock)
        {
            if (_items.Count >= count)
            {
                return;
            }

            TaskCompletionSource signal = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Add((count, signal));
            wait = signal.Task;
        }

        TimeSpan limit = timeout ?? DefaultTimeout;
        try
        {
            await wait.WaitAsync(limit);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"Expected at least {count} {typeof(T).Name} item(s) within {limit}, but recorded {Count}.");
        }
    }
}
