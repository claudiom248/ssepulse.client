using System.Net.ServerSentEvents;
using SsePulse.Client;
using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Tests.Runtime;

public class AsyncHandlerTests
{
    [Fact]
    public async Task SingleWorker_AwaitsEachHandlerBeforeDispatchingTheNextEvent()
    {
        SseGate release = new();
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("order", "1").Send("order", "2").KeepOpen()));
        Recorder<string> started = new();
        Recorder<string> completed = new();
        await using SseSourceHarness harness = new(server);
        harness.Source.On("order", async (string data, CancellationToken token) =>
        {
            started.Add(data);
            await release.WaitAsync(token);
            completed.Add(data);
        });
        harness.Start();

        await started.WaitForCountAsync(1);
        for (int i = 0; i < 100; i++)
        {
            await Task.Yield();
        }

        int startedWhileFirstIsBlocked = started.Count;
        release.Open();
        await completed.WaitForCountAsync(2);
        await harness.StopAsync();

        Assert.Equal(1, startedWhileFirstIsBlocked);
        Assert.Equal(["1", "2"], completed.Items);
    }

    [Fact]
    public async Task MultipleWorkers_RunAsyncHandlersConcurrently()
    {
        SseGate release = new();
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("order", "1").Send("order", "2").KeepOpen()));
        Recorder<string> started = new();
        Recorder<string> completed = new();
        await using SseSourceHarness harness = new(server, options => options.MaxDegreeOfParallelism = 2);
        harness.Source.On("order", async (string data, CancellationToken token) =>
        {
            started.Add(data);
            await release.WaitAsync(token);
            completed.Add(data);
        });
        harness.Start();

        await started.WaitForCountAsync(2);
        int completedWhileBlocked = completed.Count;
        release.Open();
        await completed.WaitForCountAsync(2);
        await harness.StopAsync();

        Assert.Equal(0, completedWhileBlocked);
    }

    [Fact]
    public async Task AsyncHandlerException_IsReportedToOnErrorAndTheLoopContinues()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("order", "boom").Send("order", "fine").KeepOpen()));
        Recorder<string> handled = new();
        await using SseSourceHarness harness = new(server);
        harness.Source.On("order", async (string data) =>
        {
            await Task.Yield();
            if (data == "boom")
            {
                throw new InvalidOperationException("boom");
            }

            handled.Add(data);
        });
        harness.Start();

        await handled.WaitForCountAsync(1);
        await harness.StopAsync();

        Assert.Equal(["fine"], handled.Items);
        Assert.Single(harness.Errors.Items);
        Assert.IsType<InvalidOperationException>(harness.Errors.Items[0]);
    }

    [Fact]
    public async Task Stop_CancelsTheTokenSeenByARunningHandler()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("order", "1").KeepOpen()));
        Recorder<string> started = new();
        Recorder<string> cancelled = new();
        await using SseSourceHarness harness = new(server);
        harness.Source.On("order", async (string data, CancellationToken token) =>
        {
            started.Add(data);
            try
            {
                await TestWait.ForeverAsync(token);
            }
            catch (OperationCanceledException)
            {
                cancelled.Add(data);
                throw;
            }
        });
        harness.Start();

        await started.WaitForCountAsync(1);
        await harness.StopAsync();

        Assert.Equal(["1"], cancelled.Items);
        Assert.Empty(harness.Errors.Items);
    }

    [Fact]
    public async Task Stop_DoesNotStoreTheEventIdOfAHandlerThatWasCancelled()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("order", "1", id: "1").Send("order", "2", id: "2").KeepOpen()));
        InMemoryLastEventIdStore store = new();
        Recorder<string> started = new();
        await using SseSourceHarness harness = new(server, lastEventIdStore: store);
        harness.Source.OnItem("order", async (SseItem<string> item, CancellationToken token) =>
        {
            started.Add(item.Data);
            if (item.Data == "2")
            {
                await TestWait.ForeverAsync(token);
            }
        });
        harness.Start();

        await started.WaitForCountAsync(2);
        await harness.StopAsync();

        Assert.Equal("1", store.LastEventId);
    }

    [Fact]
    public async Task EventId_IsStoredOnlyOnceTheAsyncHandlerHasCompleted()
    {
        SseGate release = new();
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("order", "1", id: "1").KeepOpen()));
        InMemoryLastEventIdStore store = new();
        Recorder<string> started = new();
        await using SseSourceHarness harness = new(server, lastEventIdStore: store);
        harness.Source.On("order", async (string data, CancellationToken token) =>
        {
            started.Add(data);
            await release.WaitAsync(token);
        });
        harness.Start();

        await started.WaitForCountAsync(1);
        string? storedWhileRunning = store.LastEventId;
        release.Open();
        await TestWait.UntilAsync(() => Task.FromResult(store.LastEventId == "1"));
        await harness.StopAsync();

        Assert.Null(storedWhileRunning);
    }

    [Fact]
    public async Task TypedHandlers_AcceptAsyncDelegatesWithAndWithoutTheToken()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c
                .Send("Order", "{\"id\":1}")
                .Send("Invoice", "{\"id\":2}")
                .Send("shipment", "{\"id\":3}")
                .Send("receipt", "{\"id\":4}")
                .KeepOpen()));
        Recorder<string> handled = new();
        await using SseSourceHarness harness = new(server);
        harness.Source
            .On<Order>(async (order, token) =>
            {
                await Task.Yield();
                handled.Add($"order:{order.Id}:{token.CanBeCanceled}");
            })
            .On<Invoice>(async invoice =>
            {
                await Task.Yield();
                handled.Add($"invoice:{invoice.Id}");
            })
            .OnItem<Order>("shipment", async (item, token) =>
            {
                await Task.Yield();
                handled.Add($"shipment:{item.Data.Id}:{item.EventType}");
            })
            .OnItem<Invoice>("receipt", async item =>
            {
                await Task.Yield();
                handled.Add($"receipt:{item.Data.Id}");
            });
        harness.Start();

        await handled.WaitForCountAsync(4);
        await harness.StopAsync();

        Assert.Equal(["order:1:True", "invoice:2", "shipment:3:shipment", "receipt:4"], handled.Items);
    }

    [Fact]
    public async Task SyncHandlers_KeepWorkingNextToAsyncOnes()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("sync", "a").Send("async", "b").KeepOpen()));
        Recorder<string> handled = new();
        await using SseSourceHarness harness = new(server);
        harness.Source
            .On("sync", data => handled.Add($"sync:{data}"))
            .On("async", async (string data) =>
            {
                await Task.Yield();
                handled.Add($"async:{data}");
            });
        harness.Start();

        await handled.WaitForCountAsync(2);
        await harness.StopAsync();

        Assert.Equal(["sync:a", "async:b"], handled.Items);
    }

    private sealed record Order(int Id);

    private sealed record Invoice(int Id);
}
