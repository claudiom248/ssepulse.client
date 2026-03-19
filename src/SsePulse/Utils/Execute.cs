namespace SsePulse.Utils;

public static class Execute
{
    public static async Task WithIgnoreExceptionAsync(
        Func<CancellationToken, Task> action, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            await action.Invoke(cancellationToken);
        }
        catch
        {
            // ignored
        }
    }
    
public static async Task WithRetryAsync(
        Func<CancellationToken, Task> action, 
        RetryOptions? options = null,
        Action<Exception>? onError = null,
        CancellationToken cancellationToken = default)
    {
        await WithRetryAsyncCore(
            action, 
            options ?? RetryOptions.Default,
            onError ?? (ex => { }),
            cancellationToken);
    }

    private static async Task WithRetryAsyncCore(Func<CancellationToken, Task> action, RetryOptions options, 
        Action<Exception> onError,
        CancellationToken cancellationToken)
    {
        int retryCount = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await action.Invoke(cancellationToken);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                onError.Invoke(ex);
                if (retryCount++ > options.RetryCount)
                {
                    throw;
                }
                
#if UNIT_TEST
                await Task.Delay(10, cancellationToken);
#else
                    TimeSpan delay = TimeSpan.FromSeconds(Math.Min(
                        Math.Pow(options.Value.StartDelaySeconds, retryCount),
                        options.Value.MaxDelaySeconds));
                    @await Task.Delay(delay, cancellationToken);
#endif
            }
        }
    }
}