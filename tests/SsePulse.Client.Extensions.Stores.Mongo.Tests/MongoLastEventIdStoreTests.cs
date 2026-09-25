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
        IMongoClient client = Substitute.For<IMongoClient>();

        Assert.Throws<ArgumentNullException>(() => new MongoLastEventIdStore(null!, client));
    }

    [Fact]
    public void Constructor_WhenMongoClientIsNull_ThrowsArgumentNullException()
    {
        MongoLastEventIdStoreOptions options = new() { DatabaseName = "test-db" };

        Assert.Throws<ArgumentNullException>(() => new MongoLastEventIdStore(options, null!));
    }

    [Fact]
    public void Constructor_DoesNotQueryMongo()
    {
        (_, IMongoCollection<LastEventIdDocument> collection) = CreateStore(existingEventId: "event-from-previous-session");

        Assert.Empty(collection.ReceivedCalls());
    }

    [Fact]
    public async Task Get_WhenNoDocumentExistsInMongo_ReturnsNull()
    {
        (MongoLastEventIdStore store, _) = CreateStore(existingEventId: null);

        Assert.Null(await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Get_WhenDocumentExistsInMongo_ReturnsThePersistedValue()
    {
        (MongoLastEventIdStore store, _) = CreateStore(existingEventId: "event-from-previous-session");

        Assert.Equal("event-from-previous-session", await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Get_QueriesMongoOnlyOnce()
    {
        (MongoLastEventIdStore store, IMongoCollection<LastEventIdDocument> collection) =
            CreateStore(existingEventId: "event-from-previous-session");

        await store.GetLastEventIdAsync();
        await store.GetLastEventIdAsync();

        await collection.Received(1).FindAsync(
            Arg.Any<FilterDefinition<LastEventIdDocument>>(),
            Arg.Any<FindOptions<LastEventIdDocument, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_AfterASetBeforeTheFirstRead_KeepsTheValueSetInMemory()
    {
        (MongoLastEventIdStore store, _) = CreateStore(existingEventId: "persisted");

        await store.SetLastEventIdAsync("in-memory");

        Assert.Equal("in-memory", await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Get_WhenMongoIsUnavailable_ReturnsNullLogsErrorAndQueriesAgainOnTheNextCall()
    {
        MockLogger<MongoLastEventIdStore> logger = new();
        IMongoCollection<LastEventIdDocument> collection = Substitute.For<IMongoCollection<LastEventIdDocument>>();
        collection
            .FindAsync(
                Arg.Any<FilterDefinition<LastEventIdDocument>>(),
                Arg.Any<FindOptions<LastEventIdDocument, string>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new MongoException("Connection refused"));
        MongoLastEventIdStore store = new(new MongoLastEventIdStoreOptions { DatabaseName = "test-db" }, CreateClientWithCollection(collection), logger);

        string? first = await store.GetLastEventIdAsync();
        string? second = await store.GetLastEventIdAsync();

        Assert.Null(first);
        Assert.Null(second);
        Assert.True(logger.HasLog(LogLevel.Error, "Error while retrieving document", typeof(MongoException)));
        await collection.Received(2).FindAsync(
            Arg.Any<FilterDefinition<LastEventIdDocument>>(),
            Arg.Any<FindOptions<LastEventIdDocument, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_WithValidId_UpdatesTheValueReturnedByGet()
    {
        (MongoLastEventIdStore store, _) = CreateStore(existingEventId: null);

        await store.SetLastEventIdAsync("event-42");

        Assert.Equal("event-42", await store.GetLastEventIdAsync());
    }

    [Fact]
    public async Task Set_WithValidId_CallsUpdateOneWithUpsert()
    {
        (MongoLastEventIdStore store, IMongoCollection<LastEventIdDocument> collection) =
            CreateStore(existingEventId: null);

        await store.SetLastEventIdAsync("event-42");

        await collection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<LastEventIdDocument>>(),
            Arg.Any<UpdateDefinition<LastEventIdDocument>>(),
            Arg.Is<UpdateOptions>(o => o.IsUpsert == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_Multiple_GetReturnsTheLatestValue()
    {
        (MongoLastEventIdStore store, _) = CreateStore(existingEventId: null);

        await store.SetLastEventIdAsync("event-1");
        await store.SetLastEventIdAsync("event-2");
        await store.SetLastEventIdAsync("event-3");

        Assert.Equal("event-3", await store.GetLastEventIdAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Set_WithEmptyValue_DoesNotUpdateTheValueOrCallMongo(string value)
    {
        (MongoLastEventIdStore store, IMongoCollection<LastEventIdDocument> collection) =
            CreateStore(existingEventId: null);

        await store.SetLastEventIdAsync(value);

        Assert.Null(await store.GetLastEventIdAsync());
        await collection.DidNotReceive().UpdateOneAsync(
            Arg.Any<FilterDefinition<LastEventIdDocument>>(),
            Arg.Any<UpdateDefinition<LastEventIdDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_WhenMongoThrows_LogsErrorAndRetainsValue()
    {
        MockLogger<MongoLastEventIdStore> logger = new();
        (MongoLastEventIdStore store, IMongoCollection<LastEventIdDocument> collection) =
            CreateStore(existingEventId: null, logger: logger);
        collection
            .UpdateOneAsync(
                Arg.Any<FilterDefinition<LastEventIdDocument>>(),
                Arg.Any<UpdateDefinition<LastEventIdDocument>>(),
                Arg.Any<UpdateOptions>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new MongoException("Disk full"));

        await store.SetLastEventIdAsync("event-persisted");

        Assert.Equal("event-persisted", await store.GetLastEventIdAsync());
        Assert.True(logger.HasLog(LogLevel.Error, "Error while updating document", typeof(MongoException)));
    }

    [Fact]
    public async Task Set_WhenTheTokenIsCancelled_PropagatesTheCancellation()
    {
        (MongoLastEventIdStore store, IMongoCollection<LastEventIdDocument> collection) =
            CreateStore(existingEventId: null);
        collection
            .UpdateOneAsync(
                Arg.Any<FilterDefinition<LastEventIdDocument>>(),
                Arg.Any<UpdateDefinition<LastEventIdDocument>>(),
                Arg.Any<UpdateOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(call => Task.FromCanceled<UpdateResult>(call.Arg<CancellationToken>()));
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await store.SetLastEventIdAsync("event-42", cts.Token));
    }

    private static (MongoLastEventIdStore store, IMongoCollection<LastEventIdDocument> collection)
        CreateStore(string? existingEventId, ILogger<MongoLastEventIdStore>? logger = null)
    {
        IMongoCollection<LastEventIdDocument> collection = Substitute.For<IMongoCollection<LastEventIdDocument>>();
        SetupFindAsync(collection, existingEventId);

        MongoLastEventIdStoreOptions options = new() { DatabaseName = "test-db" };
        IMongoClient client = CreateClientWithCollection(collection);
        collection.ClearReceivedCalls();
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

    private static void SetupFindAsync(IMongoCollection<LastEventIdDocument> collection, string? eventId)
    {
        IAsyncCursor<string> cursor = Substitute.For<IAsyncCursor<string>>();
        if (eventId is not null)
        {
            cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
            cursor.Current.Returns([eventId]);
        }
        else
        {
            cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));
        }

        collection
            .FindAsync(
                Arg.Any<FilterDefinition<LastEventIdDocument>>(),
                Arg.Any<FindOptions<LastEventIdDocument, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(cursor));
    }
}
