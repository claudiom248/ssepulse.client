namespace SsePulse.Client.Tests.Common;

public static class TestWait
{
    public static Task ForeverAsync(CancellationToken cancellationToken)
    {
        return new TaskCompletionSource().Task.WaitAsync(cancellationToken);
    }

#pragma warning disable RS0030
    public static async Task UntilAsync(Func<Task<bool>> condition, TimeSpan? timeout = null)
    {
        TimeSpan limit = timeout ?? TimeSpan.FromSeconds(10);
        using CancellationTokenSource cts = new(limit);
        while (!await condition())
        {
            if (cts.IsCancellationRequested)
            {
                throw new TimeoutException($"The condition was not met within {limit}.");
            }

            await Task.Delay(50);
        }
    }
#pragma warning restore RS0030
}
