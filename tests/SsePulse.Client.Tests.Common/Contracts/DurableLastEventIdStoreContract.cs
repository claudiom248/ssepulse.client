using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.Tests.Common;

public abstract class DurableLastEventIdStoreContract : LastEventIdStoreContract
{
    protected abstract ILastEventIdStore CreateStoreOverSameBackend();

    protected abstract ILastEventIdStore CreateStoreWithFailingBackend();

    [Fact]
    public void Set_IsVisibleToANewInstanceOverTheSameBackend()
    {
        ILastEventIdStore first = CreateStore();
        first.Set("persisted");

        ILastEventIdStore second = CreateStoreOverSameBackend();

        Assert.Equal("persisted", second.LastEventId);
    }

    [Fact]
    public void Set_WhenTheBackendFails_DoesNotThrow()
    {
        ILastEventIdStore store = CreateStoreWithFailingBackend();

        Exception? exception = Record.Exception(() => store.Set("event-1"));

        Assert.Null(exception);
    }
}
