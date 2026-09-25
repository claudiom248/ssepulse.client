using SsePulse.Client;

namespace SsePulse.Client.Tests.Common;

public abstract class LastEventIdStoreContract
{
    protected abstract ILastEventIdStore CreateStore();

    [Fact]
    public async Task NewStore_HasNoLastEventId()
    {
        ILastEventIdStore store = CreateStore();

        Assert.Null(await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Set_StoresTheValue()
    {
        ILastEventIdStore store = CreateStore();

        await store.SetLastEventIdAsync("event-1");

        Assert.Equal("event-1", await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Set_ReplacesThePreviousValue()
    {
        ILastEventIdStore store = CreateStore();

        await store.SetLastEventIdAsync("event-1");
        await store.SetLastEventIdAsync("event-2");

        Assert.Equal("event-2", await store.GetLastEventIdAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Set_IgnoresEmptyValues(string value)
    {
        ILastEventIdStore store = CreateStore();
        await store.SetLastEventIdAsync("event-1");

        await store.SetLastEventIdAsync(value);

        Assert.Equal("event-1", await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Set_FromManyThreads_DoesNotThrowAndKeepsOneOfTheValues()
    {
        ILastEventIdStore store = CreateStore();

        Exception? exception = await Record.ExceptionAsync(() => Parallel.ForEachAsync(
            Enumerable.Range(0, 50),
            async (index, token) => await store.SetLastEventIdAsync($"event-{index}", token)));

        Assert.Null(exception);
        Assert.StartsWith("event-", await store.GetLastEventIdAsync());
    }
}