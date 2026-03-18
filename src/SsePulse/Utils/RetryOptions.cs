namespace SsePulse.Utils;

public readonly struct RetryOptions
{
    public int RetryCount { get; }
    public int StartDelaySeconds { get; }
    public int MaxDelaySeconds { get; }

    public RetryOptions(
        int retryCount = 3, 
        int startDelaySeconds = 2, 
        int maxDelaySeconds = 10, 
        Action<Exception>? onError = null) : this()
    {
        RetryCount = retryCount;
        StartDelaySeconds = startDelaySeconds;
        MaxDelaySeconds = maxDelaySeconds;
    }

    public static RetryOptions None => new(0, 0, 0);
    
    public static RetryOptions Default => new();
}