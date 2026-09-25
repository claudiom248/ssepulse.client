using SsePulse.Client;
using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Tests.Runtime;

public class HandlerFailureBehaviorTests
{
    [Fact]
    public async Task Default_SkipsTheFailedEventAndStoresItsId()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c
                .Send("order", "1", id: "1")
                .Send("order", "boom", id: "2")
                .Send("order", "3", id: "3")
                .Close()));
        InMemoryLastEventIdStore store = new();
        Recorder<string> handled = new();
        await using SseSourceHarness harness = new(server, lastEventIdStore: store);
        harness.Source.On("order", data =>
        {
            if (data == "boom")
            {
                throw new InvalidOperationException("boom");
            }

            handled.Add(data);
        });
        harness.Start();

        await harness.Consumption;

        Assert.Equal(["1", "3"], handled.Items);
        Assert.Single(harness.Errors.Items);
        Assert.Equal("3", await store.GetLastEventIdAsync());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task StopSource_FaultsTheSourceAndDoesNotStoreTheIdOfTheFailedEvent(int parallelism)
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c
                .Send("order", "1", id: "1")
                .Send("order", "boom", id: "2")
                .KeepOpen()));
        InMemoryLastEventIdStore store = new();
        Recorder<string> handled = new();
        await using SseSourceHarness harness = new(
            server,
            options =>
            {
                options.HandlerFailureBehavior = HandlerFailureBehavior.StopSource;
                options.MaxDegreeOfParallelism = parallelism;
            },
            store);
        harness.Source.On("order", async (string data, CancellationToken token) =>
        {
            await Task.Yield();
            if (data == "boom")
            {
                await handled.WaitForCountAsync(1);
                throw new InvalidOperationException("boom");
            }

            handled.Add(data);
        });
        harness.Start();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Consumption);

        Assert.Equal("boom", exception.Message);
        Assert.Single(harness.Errors.Items);
        Assert.Equal("1", await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task StopSource_RedeliversTheFailedEventWhenTheSourceIsRestarted()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c
                .Send("order", "1", id: "1")
                .Send("order", "2", id: "2")
                .KeepOpen())
            .OnConnection(c => c.Send("order", "2", id: "2").Close()));
        InMemoryLastEventIdStore store = new();
        await using (SseSourceHarness first = new(
                         server,
                         options => options.HandlerFailureBehavior = HandlerFailureBehavior.StopSource,
                         store))
        {
            first.Source.On("order", data =>
            {
                if (data == "2")
                {
                    throw new InvalidOperationException("boom");
                }
            });
            first.Start();
            await Assert.ThrowsAsync<InvalidOperationException>(() => first.Consumption);
        }

        Recorder<string> redelivered = new();
        await using SseSourceHarness second = new(server, lastEventIdStore: store);
        second.Source.On("order", data => redelivered.Add(data));
        second.Start();
        await second.Consumption;

        Assert.Equal("1", server.Requests[1].LastEventId);
        Assert.Equal(["2"], redelivered.Items);
    }
}
