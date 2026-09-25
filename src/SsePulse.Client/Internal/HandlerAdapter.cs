namespace SsePulse.Client.Internal;

internal static class HandlerAdapter
{
    public static Func<T, CancellationToken, ValueTask> ToAsync<T>(Action<T> handler)
    {
        return (value, _) =>
        {
            handler.Invoke(value);
            return ValueTask.CompletedTask;
        };
    }

    public static Func<ValueTask> ToValueTask(Action callback)
    {
        return () =>
        {
            callback.Invoke();
            return ValueTask.CompletedTask;
        };
    }

    public static Func<T, ValueTask> ToValueTask<T>(Action<T> callback)
    {
        return value =>
        {
            callback.Invoke(value);
            return ValueTask.CompletedTask;
        };
    }

    public static Func<T, CancellationToken, ValueTask> ToAsync<T>(Func<T, ValueTask> handler)
    {
        return (value, _) => handler.Invoke(value);
    }
}
