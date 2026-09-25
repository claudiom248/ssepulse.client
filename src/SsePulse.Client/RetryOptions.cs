namespace SsePulse.Client;

/// <summary>
/// Describes the retry policy applied when an SSE connection attempt fails or an established connection is lost.
/// Use the static factory properties and methods (<see cref="None"/>, <see cref="Default"/>,
/// <see cref="Fixed"/>, <see cref="Exponential"/>) to construct instances.
/// </summary>
public readonly struct RetryOptions
{
    /// <summary>The value of <see cref="MaxRetries"/> that retries forever.</summary>
    public const int UnlimitedRetries = int.MaxValue;

    /// <summary>Gets the retry delay algorithm.</summary>
    public RetryStrategy Strategy { get; }

    /// <summary>Gets the maximum number of retry attempts before the error is propagated.</summary>
    public int MaxRetries { get; }

    /// <summary>
    /// Gets the base delay between retries in milliseconds.
    /// For <see cref="RetryStrategy.Fixed"/> this is the constant delay;
    /// for <see cref="RetryStrategy.Exponential"/> the delay before the retry number <c>n</c> (starting from zero)
    /// is this value multiplied by <c>2^n</c>.
    /// </summary>
    public int DelayInMilliseconds { get; }

    /// <summary>
    /// Gets the upper bound on the computed delay in milliseconds when using
    /// <see cref="RetryStrategy.Exponential"/>. Zero means no bound. Ignored for <see cref="RetryStrategy.Fixed"/>.
    /// </summary>
    public int MaxDelayInMilliseconds { get; }

    /// <summary>Gets how a random component is applied to the computed delay.</summary>
    public RetryJitter Jitter { get; }

    /// <summary>Initializes a <see cref="RetryOptions"/> with zero retries (no retry).</summary>
    public RetryOptions() { }

    private RetryOptions(
        RetryStrategy strategy,
        int maxRetries,
        int delayInMilliseconds,
        int maxDelayInMilliseconds,
        RetryJitter jitter) : this()
    {
        Strategy = strategy;
        MaxRetries = maxRetries;
        DelayInMilliseconds = delayInMilliseconds;
        MaxDelayInMilliseconds = maxDelayInMilliseconds;
        Jitter = jitter;
    }

    internal TimeSpan GetDelay(int retryNumber, Random? random = null)
    {
        double milliseconds = Strategy switch
        {
            RetryStrategy.Fixed => DelayInMilliseconds,
            RetryStrategy.Exponential => DelayInMilliseconds * Math.Pow(2, retryNumber),
            _ => throw new ArgumentOutOfRangeException()
        };

        if (Strategy == RetryStrategy.Exponential && MaxDelayInMilliseconds > 0)
        {
            milliseconds = Math.Min(milliseconds, MaxDelayInMilliseconds);
        }

        milliseconds = Math.Min(milliseconds, int.MaxValue);
        if (Jitter == RetryJitter.Full)
        {
            milliseconds *= (random ?? Random.Shared).NextDouble();
        }

        return TimeSpan.FromMilliseconds(Math.Floor(milliseconds));
    }

    /// <summary>Gets a <see cref="RetryOptions"/> that disables retries entirely.</summary>
    public static RetryOptions None => new(RetryStrategy.Fixed, 0, 0, 0, RetryJitter.None);

    /// <summary>
    /// Gets a <see cref="RetryOptions"/> with sensible defaults: 5 retries with an exponential delay that starts
    /// at 1 second and is capped at 30 seconds, with full jitter.
    /// </summary>
    public static RetryOptions Default => new(RetryStrategy.Exponential, 5, 1000, 30000, RetryJitter.Full);

    /// <summary>
    /// Creates a <see cref="RetryOptions"/> with a fixed delay between each attempt.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="delayInMilliseconds">Milliseconds to wait between each attempt.</param>
    /// <param name="jitter">How a random component is applied to the delay.</param>
    /// <exception cref="ArgumentOutOfRangeException">A value is negative.</exception>
    public static RetryOptions Fixed(
        int maxRetries,
        int delayInMilliseconds,
        RetryJitter jitter = RetryJitter.None)
    {
        Validate(maxRetries, delayInMilliseconds, 0);
        return new RetryOptions(
            RetryStrategy.Fixed,
            maxRetries,
            delayInMilliseconds,
            0,
            jitter);
    }

    /// <summary>
    /// Creates a <see cref="RetryOptions"/> with an exponentially growing delay between attempts,
    /// capped at <paramref name="maxDelayInMilliseconds"/>.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts; <see cref="UnlimitedRetries"/> retries forever.</param>
    /// <param name="delayInMilliseconds">Delay before the first retry in milliseconds; it doubles at every retry.</param>
    /// <param name="maxDelayInMilliseconds">Upper bound on the computed delay in milliseconds; zero means no bound.</param>
    /// <param name="jitter">How a random component is applied to the delay. Defaults to <see cref="RetryJitter.Full"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">A value is negative.</exception>
    public static RetryOptions Exponential(
        int maxRetries,
        int delayInMilliseconds,
        int maxDelayInMilliseconds,
        RetryJitter jitter = RetryJitter.Full)
    {
        Validate(maxRetries, delayInMilliseconds, maxDelayInMilliseconds);
        return new RetryOptions(
            RetryStrategy.Exponential,
            maxRetries,
            delayInMilliseconds,
            maxDelayInMilliseconds,
            jitter);
    }

    private static void Validate(int maxRetries, int delayInMilliseconds, int maxDelayInMilliseconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);
        ArgumentOutOfRangeException.ThrowIfNegative(delayInMilliseconds);
        ArgumentOutOfRangeException.ThrowIfNegative(maxDelayInMilliseconds);
    }
}
