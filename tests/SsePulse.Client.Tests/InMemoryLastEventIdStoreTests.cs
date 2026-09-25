using SsePulse.Client;

namespace SsePulse.Client.Tests;

public class InMemoryLastEventIdStoreTests
{
    [Fact]
    public async Task GetLastEventId_Initially_ReturnsNull()
    {
        // ARRANGE
        InMemoryLastEventIdStore store = new();

        // ACT
        string? result = await store.GetLastEventIdAsync();

        // ASSERT
        Assert.Null(result);
    }

    [Fact]
    public async Task SetLastEventId_WithValidId_StoresValue()
    {
        // ARRANGE
        InMemoryLastEventIdStore store = new();
        string eventId = "event-456";

        // ACT
        await store.SetLastEventIdAsync(eventId);

        // ASSERT
        Assert.Equal(eventId, await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task SetLastEventId_WithEmptyString_DoesNotStore()
    {
        // ARRANGE
        InMemoryLastEventIdStore store = new();

        // ACT
        await store.SetLastEventIdAsync("");

        // ASSERT
        Assert.Null(await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task SetLastEventId_WithWhitespace_DoesNotStore()
    {
        // ARRANGE
        InMemoryLastEventIdStore store = new();

        // ACT
        await store.SetLastEventIdAsync("   ");

        // ASSERT
        Assert.Null(await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task SetLastEventId_Multiple_UsesLastValue()
    {
        // ARRANGE
        InMemoryLastEventIdStore store = new();

        // ACT
        await store.SetLastEventIdAsync("id-1");
        await store.SetLastEventIdAsync("id-2");
        await store.SetLastEventIdAsync("id-3");

        // ASSERT
        Assert.Equal("id-3", await store.GetLastEventIdAsync());
    }
}

