using SsePulse.Client.Tests.Mocks;

namespace SsePulse.Client.Tests.SseSource;

public partial class SseSourceLifecycleTests : SseSourceTestBase
{
    [Fact]
    public void IsConnected_Initially_ReturnsFalse()
    {
        // ARRANGE & ACT
        using Core.SseSource source = CreateSource();

        // ASSERT
        Assert.False(source.IsConnected);
    }

    [Fact]
    public void Completion_Initially_ReturnsNotCompletedTask()
    {
        // ARRANGE & ACT
        using Core.SseSource source = CreateSource();

        // ASSERT
        Assert.NotNull(source.Completion);
        Assert.False(source.Completion.IsCompleted);
    }

    [Fact]
    public async Task StartConsumeAsync_WhenAlreadyStarted_ThrowsInvalidOperationException()
    {
        // ARRANGE
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client);

        // ACT & ASSERT
        _ = Task.Run(() => source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token));
        await Task.Delay(3000);
        await Assert.ThrowsAsync<InvalidOperationException>(() => source.StartConsumeAsync(default));
    }

    [Fact]
    public void Reset_WhileRunning_ThrowsInvalidOperationException()
    {
        // ARRANGE & ACT & ASSERT
        using Core.SseSource source = CreateSource();
        Assert.Throws<InvalidOperationException>(() => source.Reset());
    }

    [Fact]
    public async Task Reset_AfterCompletion_ReinitializesSource()
    {
        // ARRANGE
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client);
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ACT
        source.Reset();

        // ASSERT
        Assert.False(source.IsConnected);
    }

    [Fact]
    public void Stop_NotStarted_ThrowsInvalidOperationException()
    {
        // ARRANGE
        using Core.SseSource source = CreateSource();

        // ACT & ASSERT
        Assert.Throws<InvalidOperationException>(() => source.Stop());
    }

    [Fact]
    public void Stop_AfterDispose_ThrowsObjectDisposedException()
    {
        // ARRANGE
        Core.SseSource source = CreateSource();
        source.Dispose();

        // ACT & ASSERT
        Assert.Throws<ObjectDisposedException>(() => source.Stop());
    }


    [Fact]
    public void Dispose_MultipleTimes_IsIdempotent()
    {
        // ARRANGE
        Core.SseSource source = CreateSource();

        // ACT
        Exception? exception = Record.Exception(() =>
        {
            source.Dispose();
            source.Dispose();
        });

        // ASSERT
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_AfterDispose_ThrowsObjectDisposedException()
    {
        // ARRANGE
        Core.SseSource source = CreateSource();
        source.Dispose();

        // ACT & ASSERT
        Assert.Throws<ObjectDisposedException>(() => source.On("t", _ => { }));
    }

    [Fact]
    public void Dispose_GenericOn_ThrowsObjectDisposedException()
    {
        // ARRANGE
        Core.SseSource source = CreateSource();
        source.Dispose();

        // ACT & ASSERT
        Assert.Throws<ObjectDisposedException>(() => source.On<TestEventData>(_ => { }));
    }

    [Fact]
    public void Dispose_Reset_ThrowsObjectDisposedException()
    {
        // ARRANGE
        Core.SseSource source = CreateSource();
        source.Dispose();

        // ACT & ASSERT
        Assert.Throws<ObjectDisposedException>(() => source.Reset());
    }

    [Fact]
    public void Dispose_Setter_ThrowsObjectDisposedException()
    {
        // ARRANGE
        Core.SseSource source = CreateSource();
        source.Dispose();

        // ACT & ASSERT
        Assert.Throws<ObjectDisposedException>(() => source.OnConnectionEstablished = () => { });
    }

    [Fact]
    public async Task Dispose_Start_ThrowsObjectDisposedException()
    {
        // ARRANGE
        Core.SseSource source = CreateSource();
        source.Dispose();

        // ACT & ASSERT
        await Assert.ThrowsAsync<ObjectDisposedException>(() => source.StartConsumeAsync(CancellationToken.None));
    }

    [Fact]
    public void Dispose_NewInstance_CompletesSuccessfully()
    {
        // ARRANGE
        Core.SseSource source = CreateSource();

        // ACT
        source.Dispose();

        // ASSERT
        Assert.True(source.Completion.IsCompleted);
    }

    [Fact]
    public void Dispose_CancelsTcs()
    {
        // ARRANGE
        Core.SseSource source = CreateSource();

        // ACT
        source.Dispose();

        // ASSERT
        Assert.True(source.Completion.IsCompleted);
    }

}

