namespace SsePulse.Client.Common.Models;

/// <summary>
/// Describes the retry policy applied when an SSE connection attempt fails.
/// Use the static factory properties and methods (<see cref="None"/>, <see cref="Default"/>,
/// <see cref="Fixed"/>, <see cref="Exponential"/>) to construct instances.
/// </summary>
public readonly struct RetryOptions
{
    /// <summary>Gets the retry delay algorithm.</summary>
    public RetryStrategy Strategy { get; }

    /// <summary>Gets the maximum number of retry attempts before the error is propagated.</summary>
    public int MaxRetries { get; }

    /// <summary>
    /// Gets the base delay between retries in milliseconds.
    /// For <see cref="RetryStrategy.Fixed"/> this is the constant delay;
    /// for <see cref="RetryStrategy.Exponential"/> this is the base of the exponent.
    /// </summary>
    public int DelayInMilliseconds { get; }

    /// <summary>
    /// Gets the upper bound on the computed delay in milliseconds when using
    /// <see cref="RetryStrategy.Exponential"/>. Ignored for <see cref="RetryStrategy.Fixed"/>.
    /// </summary>
    public int MaxDelayInMilliseconds { get; }

    /// <summary>Initializes a <see cref="RetryOptions"/> with zero retries (no retry).</summary>
    public RetryOptions() { }

    private RetryOptions(
        RetryStrategy strategy, 
        int maxRetries, 
        int delayInMilliseconds, 
        int maxDelayInMilliseconds) : this()
    {
        Strategy = strategy;
        MaxRetries = maxRetries;
        DelayInMilliseconds = delayInMilliseconds;
        MaxDelayInMilliseconds = maxDelayInMilliseconds;
    }

    /// <summary>Gets a <see cref="RetryOptions"/> that disables retries entirely.</summary>
    public static RetryOptions None => new(RetryStrategy.Fixed, 0, 0, 0);

    /// <summary>
    /// Gets a <see cref="RetryOptions"/> with sensible defaults: 3 retries, 2-second fixed delay,
    /// up to a 10-second ceiling.
    /// </summary>
    public static RetryOptions Default => new(RetryStrategy.Fixed, 3, 2000, 10000);

    /// <summary>
    /// Creates a <see cref="RetryOptions"/> with a fixed delay between each attempt.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="delayInMilliseconds">Milliseconds to wait between each attempt.</param>
    public static RetryOptions Fixed(
        int maxRetries,
        int delayInMilliseconds)
    {
        return new RetryOptions(
            RetryStrategy.Fixed,
            maxRetries,
            delayInMilliseconds,
            0);
    }

    /// <summary>
    /// Creates a <see cref="RetryOptions"/> with an exponentially growing delay between attempts,
    /// capped at <paramref name="maxDelayInMilliseconds"/>.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="delayInMilliseconds">Base delay in milliseconds (used as the exponent base).</param>
    /// <param name="maxDelayInMilliseconds">Upper bound on the computed delay in milliseconds.</param>
    public static RetryOptions Exponential(
        int maxRetries,
        int delayInMilliseconds,
        int maxDelayInMilliseconds)
    {
        return new RetryOptions(
            RetryStrategy.Exponential,
            maxRetries,
            delayInMilliseconds,
            maxDelayInMilliseconds);
    }
}