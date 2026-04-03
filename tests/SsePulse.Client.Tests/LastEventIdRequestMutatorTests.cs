using SsePulse.Client.Core.Internal;

namespace SsePulse.Client.Tests;

public class LastEventIdRequestMutatorTests
{
    [Fact]
    public async Task ApplyAsync_WithLastEventId_AddsHeaderToRequest()
    {
        // ARRANGE
        InMemoryLastEventIdStore store = new();
        store.Set("event-123");
        LastEventIdRequestMutator mutator = new(store);
        HttpRequestMessage request = new();

        // ACT
        await mutator.ApplyAsync(request, CancellationToken.None);

        // ASSERT
        Assert.True(request.Headers.TryGetValues("Last-Event-ID", out var values));
        Assert.Single(values);
        Assert.Equal("event-123", values.First());
    }

    [Fact]
    public async Task ApplyAsync_WithoutLastEventId_DoesNotAddHeader()
    {
        // ARRANGE
        InMemoryLastEventIdStore store = new();
        LastEventIdRequestMutator mutator = new(store);
        HttpRequestMessage request = new();

        // ACT
        await mutator.ApplyAsync(request, CancellationToken.None);
        
        // ASSERT
        Assert.False(request.Headers.Contains("Last-Event-ID"));
    }
}

