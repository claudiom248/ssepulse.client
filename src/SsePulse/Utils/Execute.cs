using SsePulse.Common.Models;

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
        RetryOptions options,
        Action<Exception>? onError = null,
        CancellationToken cancellationToken = default)
    {
        await WithRetryAsyncCore(
            action, 
            options,
            onError ?? (ex => { }),
            cancellationToken);
    }

    private static async Task WithRetryAsyncCore(Func<CancellationToken, Task> action, RetryOptions options, 
        Action<Exception> onError,
        CancellationToken cancellationToken)
    {
        int attempts = -1;
        while (true)
        {
            attempts++;
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }
            try
            {
                await action.Invoke(cancellationToken);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                onError.Invoke(ex);
                if (attempts >= options.MaxRetries)
                {
                    throw;
                }
                TimeSpan delay = CalculateDelay();
                await Task.Delay(delay, cancellationToken);
            }
        }

        TimeSpan CalculateDelay()
        {
            return options.Strategy switch
            {
                RetryStrategy.Fixed => TimeSpan.FromMilliseconds(options.DelayInMilliseconds),
                RetryStrategy.Exponential => TimeSpan.FromMilliseconds(Math.Min(
                    Math.Pow(options.DelayInMilliseconds, attempts), 
                    options.MaxDelayInMilliseconds)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}