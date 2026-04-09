#if NET10_0
namespace SsePulse.Client.Tests.SseSource;

public partial class SseSourceLifecycleTests
{
    [Fact]
    public async Task StopAsync_NotStarted_ThrowsInvalidOperationException()
    {
        // ARRANGE
        await using Core.SseSource source = CreateSource();

        // ACT & ASSERT
        await Assert.ThrowsAsync<InvalidOperationException>(source.StopAsync);
    }

    [Fact]
    public async Task StopAsync_AfterDisposeAsync_ThrowsObjectDisposedException()
    {
        // ARRANGE
        Core.SseSource source = CreateSource();
        await source.DisposeAsync();

        // ACT & ASSERT
        await Assert.ThrowsAsync<ObjectDisposedException>(source.StopAsync);
    }

    [Fact]
    public async Task DisposeAsync_Start_ThrowsObjectDisposedException()
    {
        // ARRANGE
        Core.SseSource source = CreateSource();
        await source.DisposeAsync();

        // ACT & ASSERT
        await Assert.ThrowsAsync<ObjectDisposedException>(() => source.StartConsumeAsync(CancellationToken.None));
    }

    [Fact]
    public async Task DisposeAsync_NewInstance_CompletesSuccessfully()
    {
        // ARRANGE
        Core.SseSource source = CreateSource();

        // ACT
        await source.DisposeAsync();

        // ASSERT
        Assert.True(source.Completion.IsCompleted);
    }

    [Fact]
    public async Task DisposeAsync_MultipleTimes_IsIdempotent()
    {
        // ARRANGE
        Core.SseSource source = CreateSource();

        // ACT
        Exception? exception = await Record.ExceptionAsync(async () =>
        {
            await source.DisposeAsync();
            await source.DisposeAsync();
        });

        // ASSERT
        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // ARRANGE
        Core.SseSource source = CreateSource();
        await source.DisposeAsync();

        // ACT & ASSERT
        Assert.Throws<ObjectDisposedException>(() => source.On("t", _ => { }));
    }

    [Fact]
    public async Task DisposeAsync_CancelsTcs()
    {
        // ARRANGE
        Core.SseSource source = CreateSource();

        // ACT
        await source.DisposeAsync();

        // ASSERT
        Assert.True(source.Completion.IsCompleted);
    }
}
#endif

