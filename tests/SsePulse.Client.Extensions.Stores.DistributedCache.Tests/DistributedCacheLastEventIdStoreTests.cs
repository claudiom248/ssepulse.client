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
        // ARRANGE
        IDistributedCache cache = Substitute.For<IDistributedCache>();
        
        // ACT & ASSERT
        Assert.Throws<ArgumentNullException>(() =>
            new DistributedCacheLastEventIdStore(null!, cache, NullLogger<DistributedCacheLastEventIdStore>.Instance));
    }
    
    [Fact]
    public void Constructor_WhenCacheIsNull_ThrowsArgumentNullException()
    {
        // ARRANGE
        DistributedCacheLastEventIdStoreOptions options = new();
        
        // ACT & ASSERT
        Assert.Throws<ArgumentNullException>(() =>
            new DistributedCacheLastEventIdStore(options, null!, NullLogger<DistributedCacheLastEventIdStore>.Instance));
    }
    
    [Fact]
    public void Constructor_WhenCacheReturnsNull_LastEventIdIsNull()
    {
        // ARRANGE
        IDistributedCache cache = CreateCacheWithValue(null);
        
        // ACT
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);
        
        // ASSERT
        Assert.Null(store.LastEventId);
    }
    [Fact]
    public void Constructor_WhenCacheReturnsValue_LastEventIdIsRestored()
    {
        // ARRANGE
        IDistributedCache cache = CreateCacheWithValue("event-from-previous-session");
        
        // ACT
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);
        
        // ASSERT
        Assert.Equal("event-from-previous-session", store.LastEventId);
    }
    [Fact]
    public void Constructor_WhenCacheIsUnavailable_LastEventIdIsNull()
    {
        // ARRANGE
        IDistributedCache cache = Substitute.For<IDistributedCache>();
        cache.Get(Arg.Any<string>()).Throws(new Exception("Cache unavailable"));
        
        // ACT
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);
        
        // ASSERT
        Assert.Null(store.LastEventId);
    }
    [Fact]
    public void Constructor_WhenCacheIsUnavailable_LogsError()
    {
        // ARRANGE
        MockLogger<DistributedCacheLastEventIdStore> logger = new();
        IDistributedCache cache = Substitute.For<IDistributedCache>();
        cache.Get(Arg.Any<string>()).Throws(new Exception("Cache unavailable"));
        
        // ACT
        DistributedCacheLastEventIdStore _ = new(new(), cache, logger);
        
        // ASSERT
        Assert.True(logger.HasLog(LogLevel.Error, "Failed to retrieve last event ID", typeof(Exception)));
    }
    [Fact]
    public void Set_WithValidId_UpdatesLastEventIdProperty()
    {
        // ARRANGE
        IDistributedCache cache = CreateCacheWithValue(null);
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);
        
        // ACT
        store.Set("event-42");
        
        // ASSERT
        Assert.Equal("event-42", store.LastEventId);
    }
    [Fact]
    public void Set_WithValidId_CallsCacheSet()
    {
        // ARRANGE
        IDistributedCache cache = CreateCacheWithValue(null);
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);
        
        // ACT
        store.Set("event-42");
        
        // ASSERT
        cache.Received(1).Set(
            Arg.Any<string>(),
            Arg.Is<byte[]>(b => Encoding.UTF8.GetString(b) == "event-42"),
            Arg.Any<DistributedCacheEntryOptions>());
    }
    [Fact]
    public void Set_WithAbsoluteExpirationConfigured_PassesExpirationToCache()
    {
        // ARRANGE
        TimeSpan ttl = TimeSpan.FromMinutes(30);
        IDistributedCache cache = CreateCacheWithValue(null);
        DistributedCacheLastEventIdStoreOptions options = new() { AbsoluteExpirationRelativeToNow = ttl };
        DistributedCacheLastEventIdStore store = new(options, cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);
        
        // ACT
        store.Set("event-42");
        
        // ASSERT
        cache.Received(1).Set(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == ttl));
    }
    [Fact]
    public void Set_WithNoAbsoluteExpirationConfigured_PassesNullExpirationToCache()
    {
        // ARRANGE
        IDistributedCache cache = CreateCacheWithValue(null);
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);
        
        // ACT
        store.Set("event-42");
        
        // ASSERT
        cache.Received(1).Set(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == null));
    }
    [Fact]
    public void Set_Multiple_LastEventIdIsLatestValue()
    {
        // ARRANGE
        IDistributedCache cache = CreateCacheWithValue(null);
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);
       
        // ACT
        store.Set("event-1");
        store.Set("event-2");
        store.Set("event-3");
        
        // ASSERT
        Assert.Equal("event-3", store.LastEventId);
    }
    [Fact]
    public void Set_WithEmptyString_DoesNotUpdateLastEventIdOrWriteToCache()
    {
        // ARRANGE
        IDistributedCache cache = CreateCacheWithValue(null);
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);
        
        // ACT
        store.Set(string.Empty);
        
        // ASSERT
        Assert.Null(store.LastEventId);
        cache.DidNotReceive().Set(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>());
    }
    [Fact]
    public void Set_WithWhiteSpace_DoesNotUpdateLastEventIdOrWriteToCache()
    {
        // ARRANGE
        IDistributedCache cache = CreateCacheWithValue(null);
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);
        
        // ACT
        store.Set("   ");
        
        // ASSERT
        Assert.Null(store.LastEventId);
        cache.DidNotReceive().Set(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>());
    }
    [Fact]
    public void Set_WhenCacheThrows_LogsError()
    {
        // ARRANGE
        MockLogger<DistributedCacheLastEventIdStore> logger = new();
        IDistributedCache cache = CreateCacheWithValue(null);
        DistributedCacheLastEventIdStore store = new(new(), cache, logger);
        cache.When(c => c.Set(
                Arg.Any<string>(),
                Arg.Any<byte[]>(),
                Arg.Any<DistributedCacheEntryOptions>()))
            .Throw(new Exception("Write failure"));
        
        // ACT
        store.Set("event-42");
        
        // ASSERT
        Assert.True(logger.HasLog(LogLevel.Error, "Failed to persist last event ID", typeof(Exception)));
    }
    [Fact]
    public void Set_WhenCacheThrows_DoesNotUpdateLastEventId()
    {
        // ARRANGE
        IDistributedCache cache = CreateCacheWithValue(null);
        DistributedCacheLastEventIdStore store = new(new(), cache, NullLogger<DistributedCacheLastEventIdStore>.Instance);
        cache.When(c => c.Set(
                Arg.Any<string>(),
                Arg.Any<byte[]>(),
                Arg.Any<DistributedCacheEntryOptions>()))
            .Throw(new Exception("Write failure"));
        
        // ACT
        store.Set("event-42");
        
        // ASSERT
        Assert.Null(store.LastEventId);
    }
    
    private static IDistributedCache CreateCacheWithValue(string? value)
    {
        IDistributedCache cache = Substitute.For<IDistributedCache>();
        cache.Get(Arg.Any<string>()).Returns(
            value is not null ? Encoding.UTF8.GetBytes(value) : null);
        return cache;
    }
}

