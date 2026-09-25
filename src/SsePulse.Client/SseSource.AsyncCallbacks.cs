namespace SsePulse.Client;

public partial class SseSource
{
    /// <summary>
    /// Replaces the callback invoked each time the SSE connection is successfully established with an asynchronous one.
    /// The callback is awaited before the stream is consumed. Exceptions it throws are logged and swallowed.
    /// The <see cref="OnConnectionEstablished"/> property keeps returning the last synchronous callback assigned to it.
    /// </summary>
    /// <param name="callback">The asynchronous callback.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource UseOnConnectionEstablished(Func<ValueTask> callback)
    {
        AssertNotDisposed();
        AssertNotStarted();
        ArgumentNullException.ThrowIfNull(callback);
        _connectionHandlers.OnConnectionEstablished = callback;
        return this;
    }

    /// <summary>
    /// Replaces the callback invoked when the SSE connection is closed cleanly with an asynchronous one.
    /// Exceptions it throws are logged and swallowed.
    /// The <see cref="OnConnectionClosed"/> property keeps returning the last synchronous callback assigned to it.
    /// </summary>
    /// <param name="callback">The asynchronous callback.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource UseOnConnectionClosed(Func<ValueTask> callback)
    {
        AssertNotDisposed();
        AssertNotStarted();
        ArgumentNullException.ThrowIfNull(callback);
        _connectionHandlers.OnConnectionClosed = callback;
        return this;
    }

    /// <summary>
    /// Replaces the callback invoked when the SSE connection drops unexpectedly with an asynchronous one.
    /// Exceptions it throws are logged and swallowed.
    /// The <see cref="OnConnectionLost"/> property keeps returning the last synchronous callback assigned to it.
    /// </summary>
    /// <param name="callback">The asynchronous callback receiving the exception that dropped the connection.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource UseOnConnectionLost(Func<Exception, ValueTask> callback)
    {
        AssertNotDisposed();
        AssertNotStarted();
        ArgumentNullException.ThrowIfNull(callback);
        _connectionHandlers.OnConnectionLost = callback;
        return this;
    }

    /// <summary>
    /// Replaces the callback invoked when an error occurs while processing an individual SSE event with an asynchronous one.
    /// Exceptions it throws are logged and swallowed.
    /// The <see cref="OnError"/> property keeps returning the last synchronous callback assigned to it.
    /// </summary>
    /// <param name="callback">The asynchronous callback receiving the exception.</param>
    /// <returns>The current <see cref="SseSource"/> for chaining.</returns>
    public SseSource UseOnError(Func<Exception, ValueTask> callback)
    {
        AssertNotDisposed();
        AssertNotStarted();
        ArgumentNullException.ThrowIfNull(callback);
        _onError = callback;
        return this;
    }
}
