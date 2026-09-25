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

    public static Func<T, CancellationToken, ValueTask> ToAsync<T>(Func<T, ValueTask> handler)
    {
        return (value, _) => handler.Invoke(value);
    }
}
