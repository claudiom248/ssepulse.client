using SsePulse.Client.Tests.Mocks;

namespace SsePulse.Client.Tests.SseSource;

public class SseSourceLifecycleTests : SseSourceTestBase
{
    // --- Gruppo: Initial state (2 tests) ---

    [Fact]
    public void IsConnected_Initially_ReturnsFalse()
    {
        using Core.SseSource source = CreateSource();
        Assert.False(source.IsConnected);
    }

    [Fact]
    public void Completion_Initially_ReturnsNotCompletedTask()
    {
        using Core.SseSource source = CreateSource();
        Assert.NotNull(source.Completion);
        Assert.False(source.Completion.IsCompleted);
    }

    // --- Gruppo: StartConsumeAsync guard (1 test) ---

    [Fact]
    public async Task StartConsumeAsync_Twice_ThrowsInvalidOperationException()
    {
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client);
        _ = Task.Run(() => source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token));
        await Task.Delay(3000);
        await Assert.ThrowsAsync<InvalidOperationException>(() => source.StartConsumeAsync(default));
    }

    // --- Gruppo: Reset (2 tests) ---

    [Fact]
    public void Reset_WhileRunning_ThrowsInvalidOperationException()
    {
        using Core.SseSource source = CreateSource();
        Assert.Throws<InvalidOperationException>(() => source.Reset());
    }

    [Fact]
    public async Task Reset_AfterCompletion_ReinitializesSource()
    {
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client);
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        source.Reset();
        Assert.False(source.IsConnected);
    }

    // --- Gruppo: StopAsync (2 tests) ---

    [Fact]
    public async Task StopAsync_NotStarted_ThrowsInvalidOperationException()
    {
        await using Core.SseSource source = CreateSource();
        await Assert.ThrowsAsync<InvalidOperationException>(() => source.StopAsync());
    }

    [Fact]
    public async Task StopAsync_AfterDisposeAsync_ThrowsObjectDisposedException()
    {
        Core.SseSource source = CreateSource();
        await source.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => source.StopAsync());
    }

    // --- Gruppo: Dispose (6 tests) ---

    [Fact]
    public void Dispose_MultipleTimes_IsIdempotent()
    {
        Core.SseSource source = CreateSource();
        source.Dispose();
        source.Dispose();
    }

    [Fact]
    public void Dispose_On_ThrowsObjectDisposedException()
    {
        Core.SseSource source = CreateSource();
        source.Dispose();
        Assert.Throws<ObjectDisposedException>(() => source.On("t", _ => { }));
    }

    [Fact]
    public void Dispose_GenericOn_ThrowsObjectDisposedException()
    {
        Core.SseSource source = CreateSource();
        source.Dispose();
        Assert.Throws<ObjectDisposedException>(() => source.On<TestEventData>(_ => { }));
    }

    [Fact]
    public void Dispose_Reset_ThrowsObjectDisposedException()
    {
        Core.SseSource source = CreateSource();
        source.Dispose();
        Assert.Throws<ObjectDisposedException>(() => source.Reset());
    }

    [Fact]
    public void Dispose_Setter_ThrowsObjectDisposedException()
    {
        Core.SseSource source = CreateSource();
        source.Dispose();
        Assert.Throws<ObjectDisposedException>(() => source.OnConnectionEstablished = () => { });
    }

    [Fact]
    public async Task Dispose_Start_ThrowsObjectDisposedException()
    {
        Core.SseSource source = CreateSource();
        await source.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => source.StartConsumeAsync(CancellationToken.None));
    }

    // --- Gruppo: DisposeAsync (4 tests) ---

    [Fact]
    public async Task DisposeAsync_NewInstance_CompletesSuccessfully()
    {
        Core.SseSource source = CreateSource();
        await source.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_MultipleTimes_IsIdempotent()
    {
        Core.SseSource source = CreateSource();
        await source.DisposeAsync();
        await source.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        Core.SseSource source = CreateSource();
        await source.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => source.On("t", _ => { }));
    }

    [Fact]
    public async Task DisposeAsync_CancelsTcs()
    {
        Core.SseSource source = CreateSource();
        await source.DisposeAsync();
        Assert.True(source.Completion.IsCompleted);
    }
}

