using SsePulse.Client;

namespace SsePulse.Client.Tests.Common;

public abstract class DurableLastEventIdStoreContract : LastEventIdStoreContract
{
    protected abstract ILastEventIdStore CreateStoreOverSameBackend();

    protected abstract ILastEventIdStore CreateStoreWithFailingBackend();

    [Fact]
    public async Task Set_IsVisibleToANewInstanceOverTheSameBackend()
    {
        ILastEventIdStore first = CreateStore();
        await first.SetLastEventIdAsync("persisted");

        ILastEventIdStore second = CreateStoreOverSameBackend();

        Assert.Equal("persisted", await second.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Set_WhenTheBackendFails_DoesNotThrow()
    {
        ILastEventIdStore store = CreateStoreWithFailingBackend();

        Exception? exception = await Record.ExceptionAsync(async () => await store.SetLastEventIdAsync("event-1"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Set_WhenTheBackendFails_KeepsTheValueInMemory()
    {
        ILastEventIdStore store = CreateStoreWithFailingBackend();

        await store.SetLastEventIdAsync("event-1");

        Assert.Equal("event-1", await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Get_WhenTheBackendFails_DoesNotThrow()
    {
        ILastEventIdStore store = CreateStoreWithFailingBackend();

        Exception? exception = await Record.ExceptionAsync(async () => await store.GetLastEventIdAsync());

        Assert.Null(exception);
    }
}