using SsePulse.Client.Core;
using SsePulse.Client.Core.Internal;
using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Tests.Runtime;

public class EventIdCommitTests
{
    [Fact]
    public async Task EventId_IsNotStoredWhileItsHandlerIsStillRunning()
    {
        SseGate release = new();
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c
                .Send("order", "1", id: "1")
                .Send("order", "2", id: "2")
                .KeepOpen()));
        InMemoryLastEventIdStore store = new();
        Recorder<string> started = new();
        await using SseSourceHarness harness = new(server, lastEventIdStore: store);
        harness.Source.OnItem("order", item =>
        {
            started.Add(item.EventId!);
            if (item.EventId == "2")
            {
                release.WaitAsync().GetAwaiter().GetResult();
            }
        });
        harness.Start();

        await started.WaitForCountAsync(2);
        string? storedWhileSecondHandlerRuns = store.LastEventId;
        release.Open();
        await TestWait.UntilAsync(() => Task.FromResult(store.LastEventId == "2"));
        await harness.StopAsync();

        Assert.Equal("1", storedWhileSecondHandlerRuns);
        Assert.Equal("2", store.LastEventId);
    }

    [Fact]
    public async Task EventId_DoesNotAdvancePastAnEventStillBeingHandled_WhenHandlersRunInParallel()
    {
        SseGate release = new();
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c
                .Send("order", "1", id: "1")
                .Send("order", "2", id: "2")
                .Send("order", "3", id: "3")
                .KeepOpen()));
        InMemoryLastEventIdStore store = new();
        Recorder<string> started = new();
        await using SseSourceHarness harness = new(server, options => options.MaxDegreeOfParallelism = 2, store);
        harness.Source.OnItem("order", item =>
        {
            started.Add(item.EventId!);
            if (item.EventId == "1")
            {
                release.WaitAsync().GetAwaiter().GetResult();
            }
        });
        harness.Start();

        await started.WaitForCountAsync(3);
        string? storedWhileFirstHandlerRuns = store.LastEventId;
        release.Open();
        await TestWait.UntilAsync(() => Task.FromResult(store.LastEventId == "3"));
        await harness.StopAsync();

        Assert.Null(storedWhileFirstHandlerRuns);
        Assert.Equal("3", store.LastEventId);
    }
    [Fact]
    public async Task EventId_IsStoredEvenWhenTheHandlerThrows()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("order", "1", id: "7").Close()));
        InMemoryLastEventIdStore store = new();
        await using SseSourceHarness harness = new(server, lastEventIdStore: store);
        harness.Source.OnItem("order", _ => throw new InvalidOperationException("boom"));
        harness.Start();

        await harness.Consumption;

        Assert.Equal("7", store.LastEventId);
        Assert.Single(harness.Errors.Items);
    }

    [Fact]
    public async Task EventId_IsStoredForEventsWithoutAHandler()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("unknown", "1", id: "9").Close()));
        InMemoryLastEventIdStore store = new();
        await using SseSourceHarness harness = new(server, lastEventIdStore: store);
        harness.Start();

        await harness.Consumption;

        Assert.Equal("9", store.LastEventId);
    }

    [Fact]
    public void Tracker_CommitsOnlyTheContiguousCompletedPrefix()
    {
        EventIdCommitTracker tracker = new();
        long first = tracker.Register("a");
        long second = tracker.Register(null);
        long third = tracker.Register("c");
        List<string> committed = [];

        tracker.Complete(third, committed.Add);
        tracker.Complete(first, committed.Add);
        tracker.Complete(second, committed.Add);

        Assert.Equal(["a", "c"], committed);
    }
}
