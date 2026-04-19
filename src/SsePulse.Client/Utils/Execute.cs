using SsePulse.Client.Common.Models;

namespace SsePulse.Client.Utils;

/// <summary>
/// Provides static helpers for executing asynchronous operations with exception suppression
/// or automatic retry logic.
/// </summary>
public static class Execute
{
    /// <summary>
    /// Executes <paramref name="function"/> and swallows any exception it throws.
    /// The optional <paramref name="onError"/> callback receives the exception before it is discarded.
    /// </summary>
    /// <param name="function">The async operation to run.</param>
    /// <param name="onError">Optional callback invoked with the caught exception.</param>
    /// <param name="cancellationToken">Cancellation token forwarded to <paramref name="function"/>.</param>
    public static async Task WithIgnoreExceptionAsync(
        Func<CancellationToken, Task> function,
        Action<Exception>? onError = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await function.Invoke(cancellationToken).ConfigureAwait(false);
        }
        catch(Exception ex)
        {
            onError?.Invoke(ex);
        }
    }

    /// <summary>
    /// Executes <paramref name="func"/> and retries on failure according to <paramref name="options"/>.
    /// Stops retrying when <paramref name="options"/> max retries are exhausted, the
    /// <paramref name="shouldRetry"/> predicate returns <see langword="false"/>, or the operation
    /// is cancelled.
    /// </summary>
    /// <param name="func">The async operation to retry.</param>
    /// <param name="options">Retry policy (strategy, max attempts, delays).</param>
    /// <param name="onError">Optional callback invoked on each failure before retrying.</param>
    /// <param name="shouldRetry">Optional predicate; return <see langword="false"/> to stop retrying early.</param>
    /// <param name="cancellationToken">Cancellation token forwarded to <paramref name="func"/> and delays.</param>
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
                await func.Invoke(cancellationToken).ConfigureAwait(false);
                return null!;
            },
            options,
            onError ?? (ex => { }),
            shouldRetry,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes <paramref name="func"/> and retries on failure according to <paramref name="options"/>,
    /// returning the result of the first successful invocation.
    /// </summary>
    /// <typeparam name="TResult">The return type of <paramref name="func"/>.</typeparam>
    /// <param name="func">The async operation to retry.</param>
    /// <param name="options">Retry policy (strategy, max attempts, delays).</param>
    /// <param name="onError">Optional callback invoked on each failure before retrying.</param>
    /// <param name="shouldRetry">Optional predicate; return <see langword="false"/> to stop retrying early.</param>
    /// <param name="cancellationToken">Cancellation token forwarded to <paramref name="func"/> and delays.</param>
    /// <returns>The result produced by <paramref name="func"/> on success.</returns>
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
            cancellationToken).ConfigureAwait(false);
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
                return await func.Invoke(cancellationToken).ConfigureAwait(false);
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
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
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