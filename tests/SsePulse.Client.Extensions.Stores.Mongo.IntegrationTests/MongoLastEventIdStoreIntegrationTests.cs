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
    public void Constructor_WhenCollectionIsEmpty_LastEventIdIsNull()
    {
        // ARRANGE & ACT
        MongoLastEventIdStore store = CreateStore(nameof(Constructor_WhenCollectionIsEmpty_LastEventIdIsNull));

        // ASSERT
        Assert.Null(store.LastEventId);
    }

    [Fact]
    public void Set_WithValidId_DocumentIsPersistedInMongoDB()
    {
        // ARRANGE
        string collection = nameof(Set_WithValidId_DocumentIsPersistedInMongoDB);
        MongoLastEventIdStore store = CreateStore(collection);

        // ACT
        store.Set("event-100");

        // ASSERT 
        IMongoCollection<LastEventIdDocument> col = _fixture.MongoClient
            .GetDatabase("sse_integration_tests")
            .GetCollection<LastEventIdDocument>(collection);

        LastEventIdDocument? doc = col.Find(d => d.Id == "default").FirstOrDefault();
        Assert.NotNull(doc);
        Assert.Equal("event-100", doc.LastEventId);
    }

    [Fact]
    public void Constructor_WhenDocumentExistsFromPreviousRun_LastEventIdIsRehydrated()
    {
        // ARRANGE
        string collection = nameof(Constructor_WhenDocumentExistsFromPreviousRun_LastEventIdIsRehydrated);
        MongoLastEventIdStore firstStore = CreateStore(collection);
        firstStore.Set("event-session-1");

        // ACT
        MongoLastEventIdStore secondStore = CreateStore(collection);

        // ASSERT
        Assert.Equal("event-session-1", secondStore.LastEventId);
    }

    [Fact]
    public void Set_CalledMultipleTimes_OnlyLatestValueIsPersistedInMongoDB()
    {
        // ARRANGE
        string collection = nameof(Set_CalledMultipleTimes_OnlyLatestValueIsPersistedInMongoDB);
        MongoLastEventIdStore store = CreateStore(collection);

        // ACT
        store.Set("event-1");
        store.Set("event-2");
        store.Set("event-3");

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
    public void Set_WithDifferentDocumentKeys_StoredDocumentsAreIsolated()
    {
        // ARRANGE
        string collection = nameof(Set_WithDifferentDocumentKeys_StoredDocumentsAreIsolated);
        MongoLastEventIdStore storeA = CreateStore(collection, documentKey: "source-A");
        MongoLastEventIdStore storeB = CreateStore(collection, documentKey: "source-B");

        // ACT
        storeA.Set("event-for-A");
        storeB.Set("event-for-B");

        // ASSERT — each key holds its own value without affecting the other
        MongoLastEventIdStore reloadedA = CreateStore(collection, documentKey: "source-A");
        MongoLastEventIdStore reloadedB = CreateStore(collection, documentKey: "source-B");

        Assert.Equal("event-for-A", reloadedA.LastEventId);
        Assert.Equal("event-for-B", reloadedB.LastEventId);
    }

    [Fact]
    public void Set_WithEmptyString_DoesNotWriteAnyDocumentToMongoDB()
    {
        // ARRANGE
        string collection = nameof(Set_WithEmptyString_DoesNotWriteAnyDocumentToMongoDB);
        MongoLastEventIdStore store = CreateStore(collection);

        // ACT
        store.Set(string.Empty);

        // ASSERT
        IMongoCollection<LastEventIdDocument> col = _fixture.MongoClient
            .GetDatabase("sse_integration_tests")
            .GetCollection<LastEventIdDocument>(collection);

        long count = col.CountDocuments(FilterDefinition<LastEventIdDocument>.Empty);
        Assert.Equal(0, count);
    }

    [Fact]
    public void Set_WithWhiteSpace_DoesNotWriteAnyDocumentToMongoDB()
    {
        // ARRANGE
        string collection = nameof(Set_WithWhiteSpace_DoesNotWriteAnyDocumentToMongoDB);
        MongoLastEventIdStore store = CreateStore(collection);

        // ACT
        store.Set("   ");

        // ASSERT
        IMongoCollection<LastEventIdDocument> col = _fixture.MongoClient
            .GetDatabase("sse_integration_tests")
            .GetCollection<LastEventIdDocument>(collection);

        long count = col.CountDocuments(FilterDefinition<LastEventIdDocument>.Empty);
        Assert.Equal(0, count);
    }

    [Fact]
    public void Set_UpdatedAtTimestamp_IsSetToApproximatelyNow()
    {
        // ARRANGE
        string collection = nameof(Set_UpdatedAtTimestamp_IsSetToApproximatelyNow);
        MongoLastEventIdStore store = CreateStore(collection);
        DateTime before = DateTime.UtcNow.AddSeconds(-1);

        // ACT
        store.Set("event-ts");

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


