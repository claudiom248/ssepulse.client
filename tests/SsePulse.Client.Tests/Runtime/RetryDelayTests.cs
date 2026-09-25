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
        RetryOptions options = RetryOptions.Exponential(maxRetries: 30, delayInMilliseconds: 1000, maxDelayInMilliseconds: 8000);

        TimeSpan delay = options.GetDelay(retryNumber);

        Assert.Equal(TimeSpan.FromMilliseconds(expectedMilliseconds), delay);
    }

    [Fact]
    public void Exponential_WithoutAMaximum_DoesNotOverflow()
    {
        RetryOptions options = RetryOptions.Exponential(maxRetries: 2000, delayInMilliseconds: 1000, maxDelayInMilliseconds: 0);

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
}
