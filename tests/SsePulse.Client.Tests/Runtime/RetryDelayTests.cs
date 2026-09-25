using SsePulse.Client;

namespace SsePulse.Client.Tests.Runtime;

public class RetryDelayTests
{
    [Theory]
    [InlineData(0, 1000)]
    [InlineData(1, 2000)]
    [InlineData(2, 4000)]
    [InlineData(3, 8000)]
    [InlineData(4, 8000)]
    [InlineData(20, 8000)]
    public void Exponential_DoublesTheDelayAndIsCappedAtTheMaximum(int retryNumber, int expectedMilliseconds)
    {
        RetryOptions options = RetryOptions.Exponential(maxRetries: 30, delayInMilliseconds: 1000, maxDelayInMilliseconds: 8000, RetryJitter.None);

        TimeSpan delay = options.GetDelay(retryNumber);

        Assert.Equal(TimeSpan.FromMilliseconds(expectedMilliseconds), delay);
    }

    [Fact]
    public void Exponential_WithoutAMaximum_DoesNotOverflow()
    {
        RetryOptions options = RetryOptions.Exponential(maxRetries: 2000, delayInMilliseconds: 1000, maxDelayInMilliseconds: 0, RetryJitter.None);

        TimeSpan delay = options.GetDelay(1500);

        Assert.Equal(TimeSpan.FromMilliseconds(int.MaxValue), delay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void Fixed_AlwaysReturnsTheSameDelay(int retryNumber)
    {
        RetryOptions options = RetryOptions.Fixed(maxRetries: 10, delayInMilliseconds: 250);

        TimeSpan delay = options.GetDelay(retryNumber);

        Assert.Equal(TimeSpan.FromMilliseconds(250), delay);
    }

    [Theory]
    [InlineData(0, 1000)]
    [InlineData(2, 4000)]
    [InlineData(10, 8000)]
    public void FullJitter_ReturnsAValueBetweenZeroAndTheComputedDelay(int retryNumber, int ceilingMilliseconds)
    {
        RetryOptions options = RetryOptions.Exponential(maxRetries: 30, delayInMilliseconds: 1000, maxDelayInMilliseconds: 8000, RetryJitter.Full);
        Random random = new(42);

        List<double> delays = Enumerable.Range(0, 200)
            .Select(_ => options.GetDelay(retryNumber, random).TotalMilliseconds)
            .ToList();

        Assert.All(delays, delay => Assert.InRange(delay, 0, ceilingMilliseconds));
        Assert.True(delays.Distinct().Count() > 50);
        Assert.True(delays.Max() > ceilingMilliseconds * 0.8);
        Assert.True(delays.Min() < ceilingMilliseconds * 0.2);
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.5, 2000)]
    [InlineData(0.999, 3996)]
    public void FullJitter_ScalesTheComputedDelayByTheRandomValue(double randomValue, int expectedMilliseconds)
    {
        RetryOptions options = RetryOptions.Exponential(maxRetries: 5, delayInMilliseconds: 1000, maxDelayInMilliseconds: 30000, RetryJitter.Full);

        TimeSpan delay = options.GetDelay(2, new FixedRandom(randomValue));

        Assert.Equal(TimeSpan.FromMilliseconds(expectedMilliseconds), delay);
    }

    [Fact]
    public void Fixed_WithFullJitter_StaysBelowTheFixedDelay()
    {
        RetryOptions options = RetryOptions.Fixed(maxRetries: 3, delayInMilliseconds: 500, RetryJitter.Full);
        Random random = new(7);

        List<double> delays = Enumerable.Range(0, 100).Select(_ => options.GetDelay(1, random).TotalMilliseconds).ToList();

        Assert.All(delays, delay => Assert.InRange(delay, 0, 500));
    }

    [Fact]
    public void Exponential_DefaultsToFullJitter()
    {
        Assert.Equal(RetryJitter.Full, RetryOptions.Exponential(3, 100, 1000).Jitter);
        Assert.Equal(RetryJitter.None, RetryOptions.Fixed(3, 100).Jitter);
    }

    [Fact]
    public void UnlimitedRetries_IsTheMaximumInteger()
    {
        Assert.Equal(int.MaxValue, RetryOptions.Exponential(RetryOptions.UnlimitedRetries, 100, 1000).MaxRetries);
    }

    [Theory]
    [InlineData(-1, 100, 100)]
    [InlineData(1, -1, 100)]
    [InlineData(1, 100, -1)]
    public void Exponential_RejectsNegativeValues(int maxRetries, int delay, int maxDelay)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryOptions.Exponential(maxRetries, delay, maxDelay));
    }

    [Fact]
    public void Fixed_RejectsNegativeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryOptions.Fixed(-1, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryOptions.Fixed(1, -100));
    }

    private sealed class FixedRandom(double value) : Random
    {
        public override double NextDouble() => value;
    }}
