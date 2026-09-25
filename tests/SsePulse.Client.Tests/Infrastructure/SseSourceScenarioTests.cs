using SsePulse.Client;
using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Tests.Infrastructure;

public class SseSourceScenarioTests
{
    [Fact]
    public async Task ConnectionDroppedMidStream_ReconnectsAndResumesFromLastEventId()
    {
        SseGate firstBatchHandled = new();
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c
                .Send("order", "1", id: "1")
                .Send("order", "2", id: "2")
                .WaitFor(firstBatchHandled)
                .Drop())
            .OnConnection(c => c
                .Send("order", "3", id: "3")
                .Close()));
        InMemoryLastEventIdStore store = new();
        await using SseSourceHarness harness = new SseSourceHarness(server, lastEventIdStore: store)
            .Listen("order")
            .Start();

        await harness.Events.WaitForCountAsync(2);
        firstBatchHandled.Open();
        await harness.Events.WaitForCountAsync(3);
        await harness.Consumption;

        Assert.Equal(["1", "2", "3"], harness.Events.Items.Select(e => e.Data));
        Assert.Equal(2, server.Requests.Count);
        Assert.Null(server.Requests[0].LastEventId);
        Assert.Equal("2", server.Requests[1].LastEventId);
    }

    [Fact]
    public async Task ServiceUnavailableThenOk_RetriesAndDeliversEvents()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Respond(503))
            .OnConnection(c => c.Send("order", "1").Close()));
        await using SseSourceHarness harness = new SseSourceHarness(
                server,
                options => options.ConnectionRetryOptions = RetryOptions.Fixed(3, 1000))
            .Listen("order")
            .Start();

        await harness.AdvanceTimeUntilAsync(() => harness.Events.Count >= 1, TimeSpan.FromSeconds(1));
        await harness.Consumption;

        Assert.Equal(2, server.Requests.Count);
        Assert.Equal("1", Assert.Single(harness.Events.Items).Data);
    }

    [Fact]
    public async Task Unauthorized_FailsWithoutRetrying()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Respond(401)));
        await using SseSourceHarness harness = new SseSourceHarness(
                server,
                options => options.ConnectionRetryOptions = RetryOptions.Fixed(3, 1000))
            .Listen("order")
            .Start();

        await Assert.ThrowsAsync<HttpRequestException>(() => harness.Consumption);

        Assert.Single(server.Requests);
    }

    [Fact]
    public async Task StalledStream_StaysConnectedUntilStopped()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.KeepOpen()));
        await using SseSourceHarness harness = new SseSourceHarness(server).Listen("order").Start();

        await harness.Lifecycle.WaitForCountAsync(1);
        bool connectedWhileStalled = harness.Source.IsConnected;
        await harness.StopAsync();

        Assert.True(connectedWhileStalled);
        Assert.Equal("established", harness.Lifecycle.Items[0]);
        Assert.Empty(harness.Events.Items);
    }
}
