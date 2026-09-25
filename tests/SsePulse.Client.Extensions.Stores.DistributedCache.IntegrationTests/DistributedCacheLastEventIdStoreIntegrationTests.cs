using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using SsePulse.Client.Tests.Common;

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
    public async Task Get_WhenCacheIsEmpty_ReturnsNull()
    {
        // ARRANGE & ACT
        DistributedCacheLastEventIdStore store = CreateStore(nameof(Get_WhenCacheIsEmpty_ReturnsNull));

        // ASSERT
        Assert.Null(await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Set_WithValidId_EventIdIsPersistedInCache()
    {
        // ARRANGE
        string key = nameof(Set_WithValidId_EventIdIsPersistedInCache);
        DistributedCacheLastEventIdStore store = CreateStore(key);
        
        // ACT
        await store.SetLastEventIdAsync("event-100");
        
        // ASSERT
        string? storedValue = _fixture.Cache.GetString(key);
        Assert.Equal("event-100", storedValue);
    }

    [Fact]
    public async Task Get_WhenKeyExistsFromPreviousInstance_ReturnsThePersistedValue()
    {
        // ARRANGE
        string key = nameof(Get_WhenKeyExistsFromPreviousInstance_ReturnsThePersistedValue);
        DistributedCacheLastEventIdStore firstStore = CreateStore(key);
        await firstStore.SetLastEventIdAsync("event-session-1");

        // ACT
        DistributedCacheLastEventIdStore secondStore = CreateStore(key);

        // ASSERT
        Assert.Equal("event-session-1", await secondStore.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Set_CalledMultipleTimes_OnlyLatestValueIsPersistedInCache()
    {
        // ARRANGE
        string key = nameof(Set_CalledMultipleTimes_OnlyLatestValueIsPersistedInCache);
        DistributedCacheLastEventIdStore store = CreateStore(key);

        // ACT
        await store.SetLastEventIdAsync("event-1");
        await store.SetLastEventIdAsync("event-2");
        await store.SetLastEventIdAsync("event-3");

        // ASSERT
        string? storedValue = _fixture.Cache.GetString(key);
        Assert.Equal("event-3", storedValue);
    }

    [Fact]
    public async Task Set_WithDifferentKeys_StoredValuesAreIsolated()
    {
        // ARRANGE
        string baseKey = nameof(Set_WithDifferentKeys_StoredValuesAreIsolated);
        DistributedCacheLastEventIdStore storeA = CreateStore($"{baseKey}:A");
        DistributedCacheLastEventIdStore storeB = CreateStore($"{baseKey}:B");

        // ACT
        await storeA.SetLastEventIdAsync("event-for-A");
        await storeB.SetLastEventIdAsync("event-for-B");

        // ASSERT 
        DistributedCacheLastEventIdStore reloadedA = CreateStore($"{baseKey}:A");
        DistributedCacheLastEventIdStore reloadedB = CreateStore($"{baseKey}:B");
        Assert.Equal("event-for-A", await reloadedA.GetLastEventIdAsync());
        Assert.Equal("event-for-B", await reloadedB.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Set_WithEmptyString_DoesNotWriteToCache()
    {
        // ARRANGE
        string key = nameof(Set_WithEmptyString_DoesNotWriteToCache);
        DistributedCacheLastEventIdStore store = CreateStore(key);

        // ACT
        await store.SetLastEventIdAsync(string.Empty);

        // ASSERT
        string? storedValue = _fixture.Cache.GetString(key);
        Assert.Null(storedValue);
    }

    [Fact]
    public async Task Set_WithWhiteSpace_DoesNotWriteToCache()
    {
        // ARRANGE
        string key = nameof(Set_WithWhiteSpace_DoesNotWriteToCache);
        DistributedCacheLastEventIdStore store = CreateStore(key);

        // ACT
        await store.SetLastEventIdAsync("   ");

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
        await store.SetLastEventIdAsync("event-expiring");
        await TestWait.UntilAsync(async () => await _fixture.Cache.GetStringAsync(key) is null);

        // ASSERT
        string? storedValue = _fixture.Cache.GetString(key);
        Assert.Null(storedValue);
    }
}