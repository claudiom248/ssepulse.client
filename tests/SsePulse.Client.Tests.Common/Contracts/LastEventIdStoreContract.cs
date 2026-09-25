using SsePulse.Client;

namespace SsePulse.Client.Tests.Common;

public abstract class LastEventIdStoreContract
{
    protected abstract ILastEventIdStore CreateStore();

    [Fact]
    public void NewStore_HasNoLastEventId()
    {
        ILastEventIdStore store = CreateStore();

        Assert.Null(store.LastEventId);
    }

    [Fact]
    public void Set_StoresTheValue()
    {
        ILastEventIdStore store = CreateStore();

        store.Set("event-1");

        Assert.Equal("event-1", store.LastEventId);
    }

    [Fact]
    public void Set_ReplacesThePreviousValue()
    {
        ILastEventIdStore store = CreateStore();

        store.Set("event-1");
        store.Set("event-2");

        Assert.Equal("event-2", store.LastEventId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Set_IgnoresEmptyValues(string value)
    {
        ILastEventIdStore store = CreateStore();
        store.Set("event-1");

        store.Set(value);

        Assert.Equal("event-1", store.LastEventId);
    }

    [Fact]
    public void Set_FromManyThreads_DoesNotThrowAndKeepsOneOfTheValues()
    {
        ILastEventIdStore store = CreateStore();

        Exception? exception = Record.Exception(() =>
            Parallel.For(0, 50, index => store.Set($"event-{index}")));

        Assert.Null(exception);
        Assert.StartsWith("event-", store.LastEventId);
    }
}
