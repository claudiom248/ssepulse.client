namespace SsePulse.Client.Common.Models;

/// <summary>Specifies the algorithm used to compute the delay between connection retry attempts.</summary>
public enum RetryStrategy
{
    /// <summary>Each retry waits the same fixed number of milliseconds.</summary>
    Fixed,
    /// <summary>Each retry waits exponentially longer, up to the configured maximum delay.</summary>
    Exponential
}