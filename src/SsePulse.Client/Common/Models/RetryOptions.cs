namespace SsePulse.Client.Common.Models;

public readonly struct RetryOptions
{
    public RetryStrategy Strategy { get; }
    public int MaxRetries { get; }
    public int DelayInMilliseconds { get; }
    public int MaxDelayInMilliseconds { get; }

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

    public static RetryOptions None => new(RetryStrategy.Fixed, 0, 0, 0);
    
    public static RetryOptions Default => new(RetryStrategy.Fixed, 3, 2000, 10000);

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