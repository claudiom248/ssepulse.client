using MongoDB.Driver;

namespace SsePulse.Client.Extensions.Stores.Mongo.IntegrationTests;

[Trait("Category","IntegrationTests")]
[Collection(MongoContainerCollection.Name)]
public sealed class MongoLastEventIdStoreIntegrationTests
{
    private readonly MongoContainerFixture _fixture;

    public MongoLastEventIdStoreIntegrationTests(MongoContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private MongoLastEventIdStore CreateStore(
        string collectionName,
        string documentKey = "default",
        string? logger = null)
    {
        MongoLastEventIdStoreOptions options = new()
        {
            DatabaseName = "sse_integration_tests",
            CollectionName = collectionName,
            DocumentKey = documentKey,
        };
        return new MongoLastEventIdStore(options, _fixture.MongoClient);
    }

    [Fact]
    public async Task Get_WhenCollectionIsEmpty_ReturnsNull()
    {
        // ARRANGE & ACT
        MongoLastEventIdStore store = CreateStore(nameof(Get_WhenCollectionIsEmpty_ReturnsNull));

        // ASSERT
        Assert.Null(await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Set_WithValidId_DocumentIsPersistedInMongoDB()
    {
        // ARRANGE
        string collection = nameof(Set_WithValidId_DocumentIsPersistedInMongoDB);
        MongoLastEventIdStore store = CreateStore(collection);

        // ACT
        await store.SetLastEventIdAsync("event-100");

        // ASSERT 
        IMongoCollection<LastEventIdDocument> col = _fixture.MongoClient
            .GetDatabase("sse_integration_tests")
            .GetCollection<LastEventIdDocument>(collection);

        LastEventIdDocument? doc = col.Find(d => d.Id == "default").FirstOrDefault();
        Assert.NotNull(doc);
        Assert.Equal("event-100", doc.LastEventId);
    }

    [Fact]
    public async Task Get_WhenDocumentExistsFromPreviousRun_ReturnsThePersistedValue()
    {
        // ARRANGE
        string collection = nameof(Get_WhenDocumentExistsFromPreviousRun_ReturnsThePersistedValue);
        MongoLastEventIdStore firstStore = CreateStore(collection);
        await firstStore.SetLastEventIdAsync("event-session-1");

        // ACT
        MongoLastEventIdStore secondStore = CreateStore(collection);

        // ASSERT
        Assert.Equal("event-session-1", await secondStore.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Set_CalledMultipleTimes_OnlyLatestValueIsPersistedInMongoDB()
    {
        // ARRANGE
        string collection = nameof(Set_CalledMultipleTimes_OnlyLatestValueIsPersistedInMongoDB);
        MongoLastEventIdStore store = CreateStore(collection);

        // ACT
        await store.SetLastEventIdAsync("event-1");
        await store.SetLastEventIdAsync("event-2");
        await store.SetLastEventIdAsync("event-3");

        // ASSERT
        IMongoCollection<LastEventIdDocument> col = _fixture.MongoClient
            .GetDatabase("sse_integration_tests")
            .GetCollection<LastEventIdDocument>(collection);

        long count = col.CountDocuments(FilterDefinition<LastEventIdDocument>.Empty);
        LastEventIdDocument? doc = col.Find(d => d.Id == "default").FirstOrDefault();

        Assert.Equal(1, count);
        Assert.NotNull(doc);
        Assert.Equal("event-3", doc.LastEventId);
    }

    [Fact]
    public async Task Set_WithDifferentDocumentKeys_StoredDocumentsAreIsolated()
    {
        // ARRANGE
        string collection = nameof(Set_WithDifferentDocumentKeys_StoredDocumentsAreIsolated);
        MongoLastEventIdStore storeA = CreateStore(collection, documentKey: "source-A");
        MongoLastEventIdStore storeB = CreateStore(collection, documentKey: "source-B");

        // ACT
        await storeA.SetLastEventIdAsync("event-for-A");
        await storeB.SetLastEventIdAsync("event-for-B");

        // ASSERT — each key holds its own value without affecting the other
        MongoLastEventIdStore reloadedA = CreateStore(collection, documentKey: "source-A");
        MongoLastEventIdStore reloadedB = CreateStore(collection, documentKey: "source-B");

        Assert.Equal("event-for-A", await reloadedA.GetLastEventIdAsync());
        Assert.Equal("event-for-B", await reloadedB.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Set_WithEmptyString_DoesNotWriteAnyDocumentToMongoDB()
    {
        // ARRANGE
        string collection = nameof(Set_WithEmptyString_DoesNotWriteAnyDocumentToMongoDB);
        MongoLastEventIdStore store = CreateStore(collection);

        // ACT
        await store.SetLastEventIdAsync(string.Empty);

        // ASSERT
        IMongoCollection<LastEventIdDocument> col = _fixture.MongoClient
            .GetDatabase("sse_integration_tests")
            .GetCollection<LastEventIdDocument>(collection);

        long count = col.CountDocuments(FilterDefinition<LastEventIdDocument>.Empty);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Set_WithWhiteSpace_DoesNotWriteAnyDocumentToMongoDB()
    {
        // ARRANGE
        string collection = nameof(Set_WithWhiteSpace_DoesNotWriteAnyDocumentToMongoDB);
        MongoLastEventIdStore store = CreateStore(collection);

        // ACT
        await store.SetLastEventIdAsync("   ");

        // ASSERT
        IMongoCollection<LastEventIdDocument> col = _fixture.MongoClient
            .GetDatabase("sse_integration_tests")
            .GetCollection<LastEventIdDocument>(collection);

        long count = col.CountDocuments(FilterDefinition<LastEventIdDocument>.Empty);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Set_UpdatedAtTimestamp_IsSetToApproximatelyNow()
    {
        // ARRANGE
        string collection = nameof(Set_UpdatedAtTimestamp_IsSetToApproximatelyNow);
        MongoLastEventIdStore store = CreateStore(collection);
        DateTime before = DateTime.UtcNow.AddSeconds(-1);

        // ACT
        await store.SetLastEventIdAsync("event-ts");

        // ASSERT
        DateTime after = DateTime.UtcNow.AddSeconds(1);
        IMongoCollection<LastEventIdDocument> col = _fixture.MongoClient
            .GetDatabase("sse_integration_tests")
            .GetCollection<LastEventIdDocument>(collection);

        LastEventIdDocument? doc = col.Find(d => d.Id == "default").FirstOrDefault();
        Assert.NotNull(doc);
        Assert.InRange(doc.UpdatedAt, before, after);
    }
}


