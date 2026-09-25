using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;

namespace SsePulse.Client.Extensions.Stores.DistributedCache.IntegrationTests;

[Trait("Category", "IntegrationTests")]
[Collection(RedisContainerCollection.Name)]
public sealed class DistributedCacheLastEventIdStoreIntegrationTests
{
    private readonly RedisContainerFixture _fixture;

    public DistributedCacheLastEventIdStoreIntegrationTests(RedisContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private DistributedCacheLastEventIdStore CreateStore(string key, TimeSpan? absoluteExpirationRelativeToNow = null)
    {
        DistributedCacheLastEventIdStoreOptions options = new()
        {
            Key = key,
            AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow
        };
        return new DistributedCacheLastEventIdStore(options, _fixture.Cache,
            NullLogger<DistributedCacheLastEventIdStore>.Instance);
    }

    [Fact]
    public void Constructor_WhenCacheIsEmpty_LastEventIdIsNull()
    {
        // ARRANGE & ACT
        DistributedCacheLastEventIdStore store = CreateStore(nameof(Constructor_WhenCacheIsEmpty_LastEventIdIsNull));

        // ASSERT
        Assert.Null(store.LastEventId);
    }

    [Fact]
    public void Set_WithValidId_EventIdIsPersistedInCache()
    {
        // ARRANGE
        string key = nameof(Set_WithValidId_EventIdIsPersistedInCache);
        DistributedCacheLastEventIdStore store = CreateStore(key);
        
        // ACT
        store.Set("event-100");
        
        // ASSERT
        string? storedValue = _fixture.Cache.GetString(key);
        Assert.Equal("event-100", storedValue);
    }

    [Fact]
    public void Constructor_WhenKeyExistsFromPreviousInstance_LastEventIdIsRehydrated()
    {
        // ARRANGE
        string key = nameof(Constructor_WhenKeyExistsFromPreviousInstance_LastEventIdIsRehydrated);
        DistributedCacheLastEventIdStore firstStore = CreateStore(key);
        firstStore.Set("event-session-1");

        // ACT
        DistributedCacheLastEventIdStore secondStore = CreateStore(key);

        // ASSERT
        Assert.Equal("event-session-1", secondStore.LastEventId);
    }

    [Fact]
    public void Set_CalledMultipleTimes_OnlyLatestValueIsPersistedInCache()
    {
        // ARRANGE
        string key = nameof(Set_CalledMultipleTimes_OnlyLatestValueIsPersistedInCache);
        DistributedCacheLastEventIdStore store = CreateStore(key);

        // ACT
        store.Set("event-1");
        store.Set("event-2");
        store.Set("event-3");

        // ASSERT
        string? storedValue = _fixture.Cache.GetString(key);
        Assert.Equal("event-3", storedValue);
    }

    [Fact]
    public void Set_WithDifferentKeys_StoredValuesAreIsolated()
    {
        // ARRANGE
        string baseKey = nameof(Set_WithDifferentKeys_StoredValuesAreIsolated);
        DistributedCacheLastEventIdStore storeA = CreateStore($"{baseKey}:A");
        DistributedCacheLastEventIdStore storeB = CreateStore($"{baseKey}:B");

        // ACT
        storeA.Set("event-for-A");
        storeB.Set("event-for-B");

        // ASSERT 
        DistributedCacheLastEventIdStore reloadedA = CreateStore($"{baseKey}:A");
        DistributedCacheLastEventIdStore reloadedB = CreateStore($"{baseKey}:B");
        Assert.Equal("event-for-A", reloadedA.LastEventId);
        Assert.Equal("event-for-B", reloadedB.LastEventId);
    }

    [Fact]
    public void Set_WithEmptyString_DoesNotWriteToCache()
    {
        // ARRANGE
        string key = nameof(Set_WithEmptyString_DoesNotWriteToCache);
        DistributedCacheLastEventIdStore store = CreateStore(key);

        // ACT
        store.Set(string.Empty);

        // ASSERT
        string? storedValue = _fixture.Cache.GetString(key);
        Assert.Null(storedValue);
    }

    [Fact]
    public void Set_WithWhiteSpace_DoesNotWriteToCache()
    {
        // ARRANGE
        string key = nameof(Set_WithWhiteSpace_DoesNotWriteToCache);
        DistributedCacheLastEventIdStore store = CreateStore(key);

        // ACT
        store.Set("   ");

        // ASSERT
        string? storedValue = _fixture.Cache.GetString(key);
        Assert.Null(storedValue);
    }

    [Fact]
    public async Task Set_WithAbsoluteExpiration_ValueExpiresAfterTtl()
    {
        // ARRANGE
        string key = nameof(Set_WithAbsoluteExpiration_ValueExpiresAfterTtl);
        DistributedCacheLastEventIdStore store = CreateStore(key, absoluteExpirationRelativeToNow: TimeSpan.FromMilliseconds(200));

        // ACT
        store.Set("event-expiring");
        await Task.Delay(600);

        // ASSERT
        string? storedValue = _fixture.Cache.GetString(key);
        Assert.Null(storedValue);
    }
}