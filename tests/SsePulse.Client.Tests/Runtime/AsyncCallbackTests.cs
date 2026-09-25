using Microsoft.Extensions.Logging;
using SsePulse.Client;
using SsePulse.Client.Tests.Common;
using SsePulse.Client.Tests.Mocks;

namespace SsePulse.Client.Tests.Runtime;

public class AsyncCallbackTests
{
    [Fact]
    public async Task UseOnError_AwaitsTheCallbackWithTheHandlerException()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("order", "1").KeepOpen()));
        Recorder<string> reported = new();
        await using SseSourceHarness harness = new(server);
        harness.Source
            .UseOnError(async exception =>
            {
                await Task.Yield();
                reported.Add(exception.Message);
            })
            .On("order", _ => throw new InvalidOperationException("boom"));
        harness.Start();

        await reported.WaitForCountAsync(1);
        await harness.StopAsync();

        Assert.Equal(["boom"], reported.Items);
    }

    [Fact]
    public async Task UseOnConnectionEstablished_IsAwaitedBeforeEventsAreConsumed()
    {
        SseGate release = new();
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("order", "1").KeepOpen()));
        Recorder<string> established = new();
        Recorder<string> handled = new();
        await using SseSourceHarness harness = new(server);
        harness.Source
            .UseOnConnectionEstablished(async () =>
            {
                established.Add("established");
                await release.WaitAsync();
            })
            .On("order", data => handled.Add(data));
        harness.Start();

        await established.WaitForCountAsync(1);
        for (int i = 0; i < 100; i++)
        {
            await Task.Yield();
        }

        int handledWhileCallbackRuns = handled.Count;
        release.Open();
        await handled.WaitForCountAsync(1);
        await harness.StopAsync();

        Assert.Equal(0, handledWhileCallbackRuns);
    }

    [Fact]
    public async Task UseOnConnectionClosed_IsInvokedWhenTheServerEndsTheStream()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("order", "1").Close()));
        Recorder<string> closed = new();
        await using SseSourceHarness harness = new(server);
        harness.Source
            .UseOnConnectionClosed(async () =>
            {
                await Task.Yield();
                closed.Add("closed");
            })
            .On("order", _ => { });
        harness.Start();

        await harness.Consumption;

        Assert.Equal(["closed"], closed.Items);
    }

    [Fact]
    public async Task ThrowingAsyncCallbacks_AreLoggedAndDoNotStopTheConsumption()
    {
        MockLogger<SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(
            new SseEvent { EventType = "order", Data = "1" },
            new SseEvent { EventType = "order", Data = "2" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = new(client, new SseSourceOptions { Path = "/events" }, logger);
        List<string> handled = [];
        source
            .UseOnConnectionEstablished(() => throw new InvalidOperationException("established"))
            .UseOnConnectionClosed(async () =>
            {
                await Task.Yield();
                throw new InvalidOperationException("closed");
            })
            .UseOnError(async _ =>
            {
                await Task.Yield();
                throw new InvalidOperationException("error");
            })
            .On("order", data =>
            {
                handled.Add(data);
                if (data == "1")
                {
                    throw new InvalidOperationException("handler");
                }
            });

        await source.StartConsumeAsync(CancellationToken.None);

        Assert.Equal(["1", "2"], handled);
        Assert.True(logger.HasLog(LogLevel.Error, "OnConnectionEstablished"));
        Assert.True(logger.HasLog(LogLevel.Error, "OnConnectionClosed"));
        Assert.True(logger.HasLog(LogLevel.Error, "The OnError callback threw an exception"));
    }

    [Fact]
    public async Task SynchronousCallbackProperties_KeepWorking()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("order", "1").Close()));
        await using SseSourceHarness harness = new(server);
        harness.Listen("order").Start();

        await harness.Consumption;

        Assert.Equal(["established", "closed"], harness.Lifecycle.Items);
    }
}
