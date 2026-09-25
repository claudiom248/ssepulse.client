using System.Net.ServerSentEvents;
using SsePulse.Client;
using SsePulse.Client.Internal;
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
        string? storedWhileSecondHandlerRuns = await store.GetLastEventIdAsync();
        release.Open();
        await TestWait.UntilAsync(async () => await store.GetLastEventIdAsync() == "2");
        await harness.StopAsync();

        Assert.Equal("1", storedWhileSecondHandlerRuns);
        Assert.Equal("2", await store.GetLastEventIdAsync());
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
        string? storedWhileFirstHandlerRuns = await store.GetLastEventIdAsync();
        release.Open();
        await TestWait.UntilAsync(async () => await store.GetLastEventIdAsync() == "3");
        await harness.StopAsync();

        Assert.Null(storedWhileFirstHandlerRuns);
        Assert.Equal("3", await store.GetLastEventIdAsync());
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

        Assert.Equal("7", await store.GetLastEventIdAsync());
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

        Assert.Equal("9", await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task EventCancelledMidHandler_IsRedeliveredAfterResume()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("order", "1", id: "1").Send("order", "2", id: "2").KeepOpen())
            .OnConnection(c => c.Send("order", "2", id: "2").Close()));
        InMemoryLastEventIdStore store = new();
        Recorder<string> started = new();
        await using (SseSourceHarness first = new(server, lastEventIdStore: store))
        {
            first.Source.OnItem("order", async (SseItem<string> item, CancellationToken token) =>
            {
                started.Add(item.Data);
                if (item.Data == "2")
                {
                    await TestWait.ForeverAsync(token);
                }
            });
            first.Start();
            await started.WaitForCountAsync(2);
            await first.StopAsync();
        }

        Recorder<string> redelivered = new();
        await using SseSourceHarness second = new(server, lastEventIdStore: store);
        second.Source.On("order", data => redelivered.Add(data));
        second.Start();
        await second.Consumption;

        Assert.Equal("1", server.Requests[1].LastEventId);
        Assert.Equal(["2"], redelivered.Items);
    }

    [Fact]
    public async Task StoreFailure_IsLoggedAndDoesNotStopTheConsumption()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("order", "1", id: "1").Send("order", "2", id: "2").Close()));
        ThrowingStore store = new();
        await using SseSourceHarness harness = new(server, lastEventIdStore: store);
        harness.Listen("order").Start();

        await harness.Consumption;

        Assert.Equal(["1", "2"], harness.Events.Items.Select(e => e.Data));
        Assert.Equal(2, store.SetCalls);
    }

    [Fact]
    public void Tracker_CommitsOnlyTheContiguousCompletedPrefix()
    {
        EventIdCommitTracker tracker = new();
        long first = tracker.Register("a");
        long second = tracker.Register(null);
        long third = tracker.Register("c");
        List<string> committed = [];

        foreach (long sequence in new[] { third, first, second })
        {
            EventIdAdvance? advance = tracker.Complete(sequence);
            if (advance is { } value)
            {
                committed.Add(value.EventId);
            }
        }

        Assert.Equal(["a", "c"], committed);
    }

    private sealed class ThrowingStore : ILastEventIdStore
    {
        private int _setCalls;

        public int SetCalls => Volatile.Read(ref _setCalls);

        public ValueTask<string?> GetLastEventIdAsync(CancellationToken cancellationToken = default) => new((string?)null);

        public ValueTask SetLastEventIdAsync(string eventId, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _setCalls);
            throw new InvalidOperationException("The store is unavailable.");
        }
    }}
