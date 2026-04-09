using System.Net;
using System.Text;
using SsePulse.Client.Common.Models;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.Tests.Mocks;

namespace SsePulse.Client.Tests.SseSource;

public class SseSourceConnectionTests : SseSourceTestBase
{
    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task StartConsumeAsync_WhenServerReturnsNonTransientErrorCode_ThrowsHttpRequestException(
        HttpStatusCode statusCode)
    {
        // ARRANGE
        using HttpClient client = new(new FixedStatusHttpMessageHandler(statusCode));
        client.BaseAddress = new Uri("https://example.com");
        using Core.SseSource source = CreateSource(client, new SseSourceOptions { Path = "/sse" });

        // ACT & ASSERT
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            source.StartConsumeAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task StartConsumeAsync_WhenServerReturnsNonTransientErrorCode_LeavesIsConnectedFalse(
        HttpStatusCode statusCode)
    {
        // ARRANGE
        using HttpClient client = new(new FixedStatusHttpMessageHandler(statusCode));
        client.BaseAddress = new Uri("https://example.com");
        using Core.SseSource source = CreateSource(client, new SseSourceOptions { Path = "/sse" });

        // ACT
        _ = await Record.ExceptionAsync(() => source.StartConsumeAsync(CancellationToken.None));

        // ASSERT
        Assert.False(source.IsConnected);
    }
    
    [Fact]
    public async Task StartConsumeAsync_WhenNonHttpExceptionThrownWithRetryEnabled_DoesNotRetry()
    {
        // ARRANGE
        CallCountingHttpMessageHandler handler = new(_ =>
            throw new InvalidOperationException("not a network error"));
        using HttpClient client = new(handler);
        client.BaseAddress = new Uri("https://example.com");
        using Core.SseSource source = CreateSource(client, new SseSourceOptions
        {
            Path = "/sse",
            RetryOptions = RetryOptions.Fixed(maxRetries: 3, delayInMilliseconds: 0)
        });

        // ACT
        _ = await Record.ExceptionAsync(() => source.StartConsumeAsync(CancellationToken.None));

        // ASSERT
        Assert.Equal(1, handler.CallCount);
    }
    [Fact]
    public async Task StartConsumeAsync_WhenNonTransientHttpErrorOccursWithRetryEnabled_DoesNotRetry()
    {
        // ARRANGE
        CallCountingHttpMessageHandler handler = new(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using HttpClient client = new(handler);
        client.BaseAddress = new Uri("https://example.com");
        using Core.SseSource source = CreateSource(client, new SseSourceOptions
        {
            Path = "/sse",
            RetryOptions = RetryOptions.Fixed(maxRetries: 3, delayInMilliseconds: 0)
        });

        // ACT
        _ = await Record.ExceptionAsync(() => source.StartConsumeAsync(CancellationToken.None));

        // ASSERT
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task StartConsumeAsync_WhenTimeoutExceptionWithRetryEnabled_Retries()
    {
        // ARRANGE
        CallCountingHttpMessageHandler handler = new(_ =>
            throw new HttpRequestException("timed out", new TimeoutException()));
        using HttpClient client = new(handler);
        client.BaseAddress = new Uri("https://example.com");
        using Core.SseSource source = CreateSource(client, new SseSourceOptions
        {
            Path = "/sse",
            RetryOptions = RetryOptions.Fixed(maxRetries: 1, delayInMilliseconds: 0)
        });

        // ACT
        _ = await Record.ExceptionAsync(() => source.StartConsumeAsync(CancellationToken.None));

        // ASSERT
        Assert.Equal(2, handler.CallCount);
    }
    
    [Fact]
    public async Task StartConsumeAsync_WhenConnectionEventuallySucceeds_RetriesAndCompletesNormally()
    {
        // ARRANGE
        string? received = null;
        string sseData = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "hello" });
        CallCountingHttpMessageHandler handler = new(callIndex =>
        {
            if (callIndex == 1)
                throw new HttpRequestException("timed out", new TimeoutException());

            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(sseData, Encoding.UTF8, "text/event-stream")
            };
            return Task.FromResult(response);
        });
        using HttpClient client = new(handler);
        client.BaseAddress = new Uri("https://example.com");
        using Core.SseSource source = CreateSource(client, new SseSourceOptions
        {
            Path = "/sse",
            RetryOptions = RetryOptions.Fixed(maxRetries: 1, delayInMilliseconds: 0)
        });
        source.On("e", data => received = data);

        // ACT
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.Equal(2, handler.CallCount);
        Assert.Equal("hello", received);
    }
}

