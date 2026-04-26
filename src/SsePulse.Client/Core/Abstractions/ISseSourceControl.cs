namespace SsePulse.Client.Core.Abstractions;

/// <summary>
/// Interface defining control operations for an <see cref="SseSource"/>, such as starting/stopping consumption and resetting state.
/// </summary>
public interface ISseSourceControl
{
    /// <summary>
    /// Starts consuming the SSE stream.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation</param>
    Task StartConsumeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Signals the consumption loop to stop.
    /// </summary>
    void Stop();

    /// <summary>
    /// Asynchronously signals the consumption loop to stop.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Resets the source to its initial state.
    /// </summary>
    void Reset();
}