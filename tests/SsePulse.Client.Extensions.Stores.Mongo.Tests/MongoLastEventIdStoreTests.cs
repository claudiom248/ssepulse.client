using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SsePulse.Client.Extensions.Stores.Mongo;
using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Extensions.Stores.Mongo.Tests;

public sealed class MongoLastEventIdStoreTests
{
    [Fact]
    public void Constructor_WhenOptionsIsNull_ThrowsArgumentNullException()
    {
        // ARRANGE
        IMongoClient client = Substitute.For<IMongoClient>();

        // ACT & ASSERT
        Assert.Throws<ArgumentNullException>(() => new MongoLastEventIdStore(null!, client));
    }

    [Fact]
    public void Constructor_WhenMongoClientIsNull_ThrowsArgumentNullException()
    {
        // ARRANGE
        MongoLastEventIdStoreOptions options = new() { DatabaseName = "test-db" };

        // ACT & ASSERT
        Assert.Throws<ArgumentNullException>(() => new MongoLastEventIdStore(options, null!));
    }

    [Fact]
    public void Constructor_WhenNoDocumentExistsInMongo_LastEventIdIsNull()
    {
        // ARRANGE
        (MongoLastEventIdStore store, _) = CreateStore(existingEventId: null);

        // ACT
        string? result = store.LastEventId;

        // ASSERT
        Assert.Null(result);
    }

    [Fact]
    public void Constructor_WhenDocumentExistsInMongo_LastEventIdIsRestored()
    {
        // ARRANGE
        (MongoLastEventIdStore store, _) = CreateStore(existingEventId: "event-from-previous-session");

        // ACT
        string? result = store.LastEventId;

        // ASSERT
        Assert.Equal("event-from-previous-session", result);
    }

    [Fact]
    public void Constructor_WhenMongoIsUnavailable_LastEventIdIsNull()
    {
        // ARRANGE
        IMongoCollection<LastEventIdDocument> collection = Substitute.For<IMongoCollection<LastEventIdDocument>>();
        collection
            .FindSync(
                Arg.Any<FilterDefinition<LastEventIdDocument>>(),
                Arg.Any<FindOptions<LastEventIdDocument, string>>(),
                Arg.Any<CancellationToken>())
            .Throws(new MongoException("Connection refused"));

        MongoLastEventIdStoreOptions options = new() { DatabaseName = "test-db" };
        IMongoClient client = CreateClientWithCollection(collection);

        // ACT — constructor must not throw even when MongoDB is unavailable
        MongoLastEventIdStore store = new(options, client);

        // ASSERT
        Assert.Null(store.LastEventId);
    }

    [Fact]
    public void Set_WithValidId_UpdatesLastEventIdProperty()
    {
        // ARRANGE
        (MongoLastEventIdStore store, _) = CreateStore(existingEventId: null);

        // ACT
        store.Set("event-42");

        // ASSERT
        Assert.Equal("event-42", store.LastEventId);
    }

    [Fact]
    public void Set_WithValidId_CallsUpdateOneWithUpsert()
    {
        // ARRANGE
        (MongoLastEventIdStore store, IMongoCollection<LastEventIdDocument> collection) =
            CreateStore(existingEventId: null);

        // ACT
        store.Set("event-42");

        // ASSERT
        collection.Received(1).UpdateOne(
            Arg.Any<FilterDefinition<LastEventIdDocument>>(),
            Arg.Any<UpdateDefinition<LastEventIdDocument>>(),
            Arg.Is<UpdateOptions>(o => o.IsUpsert == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Set_Multiple_LastEventIdIsLatestValue()
    {
        // ARRANGE
        (MongoLastEventIdStore store, _) = CreateStore(existingEventId: null);

        // ACT
        store.Set("event-1");
        store.Set("event-2");
        store.Set("event-3");

        // ASSERT
        Assert.Equal("event-3", store.LastEventId);
    }

    [Fact]
    public void Set_WithEmptyString_DoesNotUpdateLastEventId()
    {
        // ARRANGE
        (MongoLastEventIdStore store, IMongoCollection<LastEventIdDocument> collection) =
            CreateStore(existingEventId: null);

        // ACT
        store.Set(string.Empty);

        // ASSERT
        Assert.Null(store.LastEventId);
        collection.DidNotReceive().UpdateOne(
            Arg.Any<FilterDefinition<LastEventIdDocument>>(),
            Arg.Any<UpdateDefinition<LastEventIdDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Set_WithWhiteSpace_DoesNotUpdateLastEventId()
    {
        // ARRANGE
        (MongoLastEventIdStore store, IMongoCollection<LastEventIdDocument> collection) =
            CreateStore(existingEventId: null);

        // ACT
        store.Set("   ");

        // ASSERT
        Assert.Null(store.LastEventId);
        collection.DidNotReceive().UpdateOne(
            Arg.Any<FilterDefinition<LastEventIdDocument>>(),
            Arg.Any<UpdateDefinition<LastEventIdDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Set_WhenMongoThrows_LogsErrorAndRetainsValue()
    {
        // ARRANGE
        MockLogger<MongoLastEventIdStore> logger = new();
        (MongoLastEventIdStore store, IMongoCollection<LastEventIdDocument> collection) =
            CreateStore(existingEventId: null, logger: logger);

        collection
            .UpdateOne(
                Arg.Any<FilterDefinition<LastEventIdDocument>>(),
                Arg.Any<UpdateDefinition<LastEventIdDocument>>(),
                Arg.Any<UpdateOptions>(),
                Arg.Any<CancellationToken>())
            .Throws(new MongoException("Disk full"));

        // ACT
        store.Set("event-persisted");

        // ASSERT — in-memory value must be updated even when MongoDB fails
        Assert.Equal("event-persisted", store.LastEventId);
        Assert.True(logger.HasLog(LogLevel.Error, "Error while updating document", typeof(MongoException)));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (MongoLastEventIdStore store, IMongoCollection<LastEventIdDocument> collection)
        CreateStore(string? existingEventId, ILogger<MongoLastEventIdStore>? logger = null)
    {
        IMongoCollection<LastEventIdDocument> collection = Substitute.For<IMongoCollection<LastEventIdDocument>>();
        SetupFindSync(collection, existingEventId);

        MongoLastEventIdStoreOptions options = new() { DatabaseName = "test-db" };
        IMongoClient client = CreateClientWithCollection(collection);
        MongoLastEventIdStore store = new(options, client, logger);
        return (store, collection);
    }

    private static IMongoClient CreateClientWithCollection(IMongoCollection<LastEventIdDocument> collection)
    {
        IMongoDatabase database = Substitute.For<IMongoDatabase>();
        database
            .GetCollection<LastEventIdDocument>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(collection);

        IMongoClient client = Substitute.For<IMongoClient>();
        client
            .GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>())
            .Returns(database);

        return client;
    }

    private static void SetupFindSync(IMongoCollection<LastEventIdDocument> collection, string? eventId)
    {
        IAsyncCursor<string> cursor = Substitute.For<IAsyncCursor<string>>();
        if (eventId is not null)
        {
            cursor.MoveNext(Arg.Any<CancellationToken>()).Returns(true, false);
            cursor.Current.Returns([eventId]);
        }
        else
        {
            cursor.MoveNext(Arg.Any<CancellationToken>()).Returns(false);
        }

        collection
            .FindSync(
                Arg.Any<FilterDefinition<LastEventIdDocument>>(),
                Arg.Any<FindOptions<LastEventIdDocument, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(cursor);
    }
}

