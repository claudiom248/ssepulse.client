using SsePulse.Client.Common.Models;

namespace SsePulse.Client.Utils;

internal static class Execute
{
    public static async Task WithIgnoreExceptionAsync(
        Func<CancellationToken, Task> function,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await function.Invoke(cancellationToken);
        }
        catch
        {
            // ignored
        }
    }

    public static async Task WithRetryAsync(
        Func<CancellationToken, Task> func,
        RetryOptions options,
        Action<Exception>? onError = null,
        Func<Exception, bool>? shouldRetry = null,
        CancellationToken cancellationToken = default)
    {
        await WithRetryAsyncCore<object?>(
            async _ =>
            {
                await func.Invoke(cancellationToken);
                return null!;
            },
            options,
            onError ?? (ex => { }),
            shouldRetry,
            cancellationToken);
    }

    public static async Task<TResult> WithRetryAsync<TResult>(
        Func<CancellationToken, Task<TResult>> func,
        RetryOptions options,
        Action<Exception>? onError = null,
        Func<Exception, bool>? shouldRetry = null,
        CancellationToken cancellationToken = default)
    {
        return await WithRetryAsyncCore(
            func,
            options,
            onError ?? (ex => { }),
            shouldRetry,
            cancellationToken);
    }

    private static async Task<TResult> WithRetryAsyncCore<TResult>(
        Func<CancellationToken, Task<TResult>> func, 
        RetryOptions options,
        Action<Exception> onError,
        Func<Exception, bool>? shouldRetry = null,
        CancellationToken cancellationToken = default)
    {
        int attempts = -1;
        while (true)
        {
            attempts++;
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await func.Invoke(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                onError.Invoke(ex);
                if (attempts >= options.MaxRetries 
                    || shouldRetry != null && !shouldRetry.Invoke(ex))
                {
                    throw;
                }

                TimeSpan delay = CalculateDelay();
                await Task.Delay(delay, cancellationToken);
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
}