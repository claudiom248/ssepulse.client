using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Extensions.Stores.DistributedCache.Tests;

public sealed class DistributedCacheLastEventIdStoreTests
{
    [Fact]
    public void Constructor_WhenOptionsIsNull_ThrowsArgumentNullException()
    {
        IDistributedCache cache = Substitute.For<IDistributedCache>();

        Assert.Throws<ArgumentNullException>(() =>
            new DistributedCacheLastEventIdStore(null!, cache, NullLogger<DistributedCacheLastEventIdStore>.Instance));
    }

    [Fact]
    public void Constructor_WhenCacheIsNull_ThrowsArgumentNullException()
    {
        DistributedCacheLastEventIdStoreOptions options = new();

        Assert.Throws<ArgumentNullException>(() =>
            new DistributedCacheLastEventIdStore(options, null!, NullLogger<DistributedCacheLastEventIdStore>.Instance));
    }

    [Fact]
    public void Constructor_DoesNotTouchTheCache()
    {
        IDistributedCache cache = CreateCacheWithValue("event-from-previous-session");

        _ = new DistributedCacheLastEventIdStore(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);

        Assert.Empty(cache.ReceivedCalls());
    }

    [Fact]
    public async Task Get_WhenCacheReturnsNull_ReturnsNull()
    {
        IDistributedCache cache = CreateCacheWithValue(null);
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);

        Assert.Null(await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Get_WhenCacheReturnsValue_ReturnsThePersistedValue()
    {
        IDistributedCache cache = CreateCacheWithValue("event-from-previous-session");
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);

        Assert.Equal("event-from-previous-session", await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Get_ReadsTheCacheOnlyOnce()
    {
        IDistributedCache cache = CreateCacheWithValue("event-from-previous-session");
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);

        await store.GetLastEventIdAsync();
        await store.GetLastEventIdAsync();

        await cache.Received(1).GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_AfterASetBeforeTheFirstRead_KeepsTheValueSetInMemory()
    {
        IDistributedCache cache = CreateCacheWithValue("persisted");
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);

        await store.SetLastEventIdAsync("in-memory");

        Assert.Equal("in-memory", await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Get_WhenCacheIsUnavailable_ReturnsNull()
    {
        IDistributedCache cache = CreateUnavailableCache();
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);

        Assert.Null(await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Get_WhenCacheIsUnavailable_LogsErrorAndReadsAgainOnTheNextCall()
    {
        MockLogger<DistributedCacheLastEventIdStore> logger = new();
        IDistributedCache cache = CreateUnavailableCache();
        DistributedCacheLastEventIdStore store = new(new(), cache, logger);

        await store.GetLastEventIdAsync();
        await store.GetLastEventIdAsync();

        Assert.True(logger.HasLog(LogLevel.Error, "Failed to retrieve last event ID", typeof(Exception)));
        await cache.Received(2).GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_WithValidId_UpdatesTheValueReturnedByGet()
    {
        IDistributedCache cache = CreateCacheWithValue(null);
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);

        await store.SetLastEventIdAsync("event-42");

        Assert.Equal("event-42", await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Set_WithValidId_CallsCacheSet()
    {
        IDistributedCache cache = CreateCacheWithValue(null);
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);

        await store.SetLastEventIdAsync("event-42");

        await cache.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Is<byte[]>(b => Encoding.UTF8.GetString(b) == "event-42"),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_WithAbsoluteExpirationConfigured_PassesExpirationToCache()
    {
        TimeSpan ttl = TimeSpan.FromMinutes(30);
        IDistributedCache cache = CreateCacheWithValue(null);
        DistributedCacheLastEventIdStoreOptions options = new() { AbsoluteExpirationRelativeToNow = ttl };
        DistributedCacheLastEventIdStore store = new(options, cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);

        await store.SetLastEventIdAsync("event-42");

        await cache.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == ttl),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_WithNoAbsoluteExpirationConfigured_PassesNullExpirationToCache()
    {
        IDistributedCache cache = CreateCacheWithValue(null);
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);

        await store.SetLastEventIdAsync("event-42");

        await cache.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_Multiple_GetReturnsTheLatestValue()
    {
        IDistributedCache cache = CreateCacheWithValue(null);
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);

        await store.SetLastEventIdAsync("event-1");
        await store.SetLastEventIdAsync("event-2");
        await store.SetLastEventIdAsync("event-3");

        Assert.Equal("event-3", await store.GetLastEventIdAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Set_WithEmptyValue_DoesNotUpdateTheValueOrWriteToCache(string value)
    {
        IDistributedCache cache = CreateCacheWithValue(null);
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);

        await store.SetLastEventIdAsync(value);

        Assert.Null(await store.GetLastEventIdAsync());
        await cache.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_WhenCacheThrows_LogsError()
    {
        MockLogger<DistributedCacheLastEventIdStore> logger = new();
        IDistributedCache cache = CreateFailingWriteCache();
        DistributedCacheLastEventIdStore store = new(new(), cache, logger);

        await store.SetLastEventIdAsync("event-42");

        Assert.True(logger.HasLog(LogLevel.Error, "Failed to persist last event ID", typeof(Exception)));
    }

    [Fact]
    public async Task Set_WhenCacheThrows_KeepsTheValueInMemory()
    {
        IDistributedCache cache = CreateFailingWriteCache();
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);

        await store.SetLastEventIdAsync("event-42");

        Assert.Equal("event-42", await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Set_WhenTheTokenIsCancelled_PropagatesTheCancellation()
    {
        IDistributedCache cache = CreateCacheWithValue(null);
        cache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromCanceled(call.Arg<CancellationToken>()));
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await store.SetLastEventIdAsync("event-42", cts.Token));
    }

    private static IDistributedCache CreateCacheWithValue(string? value)
    {
        IDistributedCache cache = Substitute.For<IDistributedCache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(
            Task.FromResult(value is not null ? Encoding.UTF8.GetBytes(value) : null));
        return cache;
    }

    private static IDistributedCache CreateUnavailableCache()
    {
        IDistributedCache cache = Substitute.For<IDistributedCache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("Cache unavailable"));
        return cache;
    }

    private static IDistributedCache CreateFailingWriteCache()
    {
        IDistributedCache cache = CreateCacheWithValue(null);
        cache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Write failure"));
        return cache;
    }
}
