using SsePulse.Client.Core.Internal;

namespace SsePulse.Client.Tests;

public class LastEventIdStoreTests
{
    [Fact]
    public void GetLastEventId_Initially_ReturnsNull()
    {
        // ARRANGE
        LastEventIdStore store = new();

        // ACT
        string? result = store.LastEventId;

        // ASSERT
        Assert.Null(result);
    }

    [Fact]
    public void SetLastEventId_WithValidId_StoresValue()
    {
        // ARRANGE
        LastEventIdStore store = new();
        string eventId = "event-456";

        // ACT
        store.Set(eventId);

        // ASSERT
        Assert.Equal(eventId, store.LastEventId);
    }

    [Fact]
    public void SetLastEventId_WithEmptyString_DoesNotStore()
    {
        // ARRANGE
        LastEventIdStore store = new();

        // ACT
        store.Set("");

        // ASSERT
        Assert.Null(store.LastEventId);
    }

    [Fact]
    public void SetLastEventId_WithWhitespace_DoesNotStore()
    {
        // ARRANGE
        LastEventIdStore store = new();

        // ACT
        store.Set("   ");

        // ASSERT
        Assert.Null(store.LastEventId);
    }

    [Fact]
    public void SetLastEventId_Multiple_UsesLastValue()
    {
        // ARRANGE
        LastEventIdStore store = new();

        // ACT
        store.Set("id-1");
        store.Set("id-2");
        store.Set("id-3");

        // ASSERT
        Assert.Equal("id-3", store.LastEventId);
    }
}

