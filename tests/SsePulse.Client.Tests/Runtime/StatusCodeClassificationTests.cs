using System.Net;
using SsePulse.Client;
using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Tests.Runtime;

public class StatusCodeClassificationTests
{
    [Theory]
    [InlineData(408)]
    [InlineData(425)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(504)]
    public async Task TransientStatusCode_IsRetriedUntilTheServerRecovers(int statusCode)
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Respond(statusCode))
            .OnConnection(c => c.Send("order", "1").Close()));
        await using SseSourceHarness harness = new SseSourceHarness(
                server,
                options => options.ConnectionRetryOptions = RetryOptions.Fixed(maxRetries: 3, delayInMilliseconds: 0))
            .Listen("order")
            .Start();

        await harness.Consumption;

        Assert.Equal(2, server.Requests.Count);
        Assert.Equal(["1"], harness.Events.Items.Select(e => e.Data));
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(405)]
    [InlineData(406)]
    [InlineData(410)]
    public async Task NonTransientStatusCode_FailsWithoutRetrying(int statusCode)
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Respond(statusCode))
            .OnConnection(c => c.Send("order", "1").Close()));
        await using SseSourceHarness harness = new SseSourceHarness(
                server,
                options => options.ConnectionRetryOptions = RetryOptions.Fixed(maxRetries: 3, delayInMilliseconds: 0))
            .Listen("order")
            .Start();

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(() => harness.Consumption);

        Assert.Equal((HttpStatusCode)statusCode, (HttpStatusCode)exception.Data["HttpStatusCode"]!);
        Assert.Single(server.Requests);
    }

    [Fact]
    public async Task CustomTransientStatusCodes_ReplaceTheDefaults()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Respond(418))
            .OnConnection(c => c.Respond(503))
            .OnConnection(c => c.Send("order", "1").Close()));
        await using SseSourceHarness harness = new SseSourceHarness(
                server,
                options =>
                {
                    options.ConnectionRetryOptions = RetryOptions.Fixed(maxRetries: 3, delayInMilliseconds: 0);
                    options.TransientStatusCodes = [(HttpStatusCode)418];
                })
            .Listen("order")
            .Start();

        await Assert.ThrowsAsync<HttpRequestException>(() => harness.Consumption);

        Assert.Equal(2, server.Requests.Count);
    }

    [Fact]
    public void Defaults_MatchTheDocumentedSet()
    {
        SseSourceOptions options = new();

        Assert.Equal(
            [408, 425, 429, 500, 502, 503, 504],
            options.TransientStatusCodes.Select(code => (int)code).Order());
        Assert.Equal(RetryOptions.Default.MaxRetries, options.ConnectionRetryOptions!.Value.MaxRetries);
    }

    [Fact]
    public async Task RetryDelay_IsWaitedOnFakeTimeBeforeTheNextAttempt()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Respond(503))
            .OnConnection(c => c.Send("order", "1").Close()));
        await using SseSourceHarness harness = new SseSourceHarness(
                server,
                options => options.ConnectionRetryOptions = RetryOptions.Fixed(maxRetries: 1, delayInMilliseconds: 5000))
            .Listen("order")
            .Start();

        await server.RequestLog.WaitForCountAsync(1);
        for (int i = 0; i < 100; i++)
        {
            await Task.Yield();
        }

        int requestsWhileWaiting = server.Requests.Count;
        await harness.AdvanceTimeUntilAsync(() => server.Requests.Count >= 2, TimeSpan.FromSeconds(5));
        await harness.Consumption;

        Assert.Equal(1, requestsWhileWaiting);
        Assert.Equal(2, server.Requests.Count);
    }
}