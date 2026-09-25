using SsePulse.Client;
using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Tests.Runtime;

public class EventsManagerBindingTests
{
    [Fact]
    public async Task Bind_RegistersOnlyMethodsNamedOnFollowedByAnUpperCaseLetter()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c
                .Send("Order", "1")
                .Send("Ce", "2")
                .Send("Line", "3")
                .Close()));
        RecordingManager manager = new();
        await using SseSourceHarness harness = new(server);
        harness.Source.Bind(manager);
        harness.Start();

        await harness.Consumption;

        Assert.Equal(["OnOrder"], manager.Calls);
    }

    [Fact]
    public async Task Bind_ThrowsForAHandlerLikeMethodWithTheWrongNumberOfParameters()
    {
        await using SseTestServer server = await SseTestServer.StartAsync();
        await using SseSourceHarness harness = new(server);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => harness.Source.Bind(new TwoParameterManager()));

        Assert.Contains("OnBoth", exception.Message);
        Assert.Contains("optionally, a CancellationToken", exception.Message);
    }

    [Fact]
    public async Task Bind_AwaitsHandlersReturningTaskOrValueTask()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c
                .Send("TaskEvent", "{\"id\":1}")
                .Send("ValueTaskEvent", "{\"id\":2}")
                .Send("Plain", "3")
                .Send("Sync", "4")
                .Close()));
        AsyncManager manager = new();
        await using SseSourceHarness harness = new(server);
        harness.Source.Bind(manager);
        harness.Start();

        await harness.Consumption;

        Assert.Equal(["task:1", "valuetask:2", "plain:3:True", "sync:4"], manager.Calls);
    }

    [Fact]
    public async Task Bind_PassesACancellationTokenThatIsCancelledOnStop()
    {
        await using SseTestServer server = await SseTestServer.StartAsync(s => s
            .OnConnection(c => c.Send("Blocking", "1").KeepOpen()));
        BlockingManager manager = new();
        await using SseSourceHarness harness = new(server);
        harness.Source.Bind(manager);
        harness.Start();

        await manager.Started.WaitForCountAsync(1);
        await harness.StopAsync();

        Assert.Equal(["cancelled"], manager.Outcomes.Items);
        Assert.Empty(harness.Errors.Items);
    }

    [Fact]
    public async Task Bind_ThrowsForAnAsyncVoidHandler()
    {
        await using SseTestServer server = await SseTestServer.StartAsync();
        await using SseSourceHarness harness = new(server);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => harness.Source.Bind(new AsyncVoidManager()));

        Assert.Contains("OnFireAndForget", exception.Message);
        Assert.Contains("async void", exception.Message);
    }

    [Fact]
    public async Task Bind_ThrowsForAHandlerReturningAnythingElse()
    {
        await using SseTestServer server = await SseTestServer.StartAsync();
        await using SseSourceHarness harness = new(server);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => harness.Source.Bind(new ReturningManager()));

        Assert.Contains("OnCount", exception.Message);
        Assert.Contains("void, Task or ValueTask", exception.Message);
    }

    [Fact]
    public async Task Bind_ThrowsForASynchronousHandlerTakingACancellationToken()
    {
        await using SseTestServer server = await SseTestServer.StartAsync();
        await using SseSourceHarness harness = new(server);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => harness.Source.Bind(new SynchronousTokenManager()));

        Assert.Contains("OnEvent", exception.Message);
        Assert.Contains("synchronous", exception.Message);
    }

    private sealed record Payload(int Id);

    private sealed class AsyncManager : ISseEventsManager
    {
        public List<string> Calls { get; } = [];

        public async Task OnTaskEvent(Payload payload)
        {
            await Task.Yield();
            Calls.Add($"task:{payload.Id}");
        }

        public async ValueTask OnValueTaskEvent(Payload payload)
        {
            await Task.Yield();
            Calls.Add($"valuetask:{payload.Id}");
        }

        public async Task OnPlain(string data, CancellationToken cancellationToken)
        {
            await Task.Yield();
            Calls.Add($"plain:{data}:{cancellationToken.CanBeCanceled}");
        }

        public void OnSync(string data) => Calls.Add($"sync:{data}");
    }

    private sealed class BlockingManager : ISseEventsManager
    {
        public Recorder<string> Started { get; } = new();

        public Recorder<string> Outcomes { get; } = new();

        public async ValueTask OnBlocking(string data, CancellationToken cancellationToken)
        {
            Started.Add(data);
            try
            {
                await TestWait.ForeverAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Outcomes.Add("cancelled");
                throw;
            }
        }
    }

    private sealed class AsyncVoidManager : ISseEventsManager
    {
        public async void OnFireAndForget(string data)
        {
            await Task.Yield();
        }
    }

    private sealed class ReturningManager : ISseEventsManager
    {
        public int OnCount(string data) => data.Length;
    }

    private sealed class SynchronousTokenManager : ISseEventsManager
    {
        public void OnEvent(string data, CancellationToken cancellationToken)
        {
        }
    }

    private sealed class RecordingManager : ISseEventsManager
    {
        public List<string> Calls { get; } = [];

        public void OnOrder(string data) => Calls.Add(nameof(OnOrder));

        public void Once(string data) => Calls.Add(nameof(Once));

        public void Online(string data) => Calls.Add(nameof(Online));
    }

    private sealed class TwoParameterManager : ISseEventsManager
    {
        public void OnBoth(string first, string second)
        {
        }
    }
}
