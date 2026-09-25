using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NSubstitute;
using SsePulse.Client.Core.Internal;
using SsePulse.Client.DependencyInjection;
using SsePulse.Client.DependencyInjection.Abstractions;
using SsePulse.Client.DependencyInjection.Internal;

namespace SsePulse.Client.Extensions.Stores.Mongo.DependencyInjection.Tests;

public sealed class SseSourceBuilderExtensionsTests
{
    [Fact]
    public void AddMongoLastEventIdStore_MongoLastEventIdStore_IsResolvableAsKeyedService()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddSingleton(CreateMockMongoClient());
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddMongoLastEventIdStore(options => options.DatabaseName = "test-db");

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        MongoLastEventIdStore store = provider.GetRequiredKeyedService<MongoLastEventIdStore>("MySource");

        Assert.IsType<MongoLastEventIdStore>(store);
    }

    [Fact]
    public void AddMongoLastEventIdStore_OptionsLastEventIdStoreFactory_ProvidesMongoLastEventIdStore()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddSingleton(CreateMockMongoClient());
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddMongoLastEventIdStore(options => options.DatabaseName = "test-db");

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions factoryOptions = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");

        Assert.NotNull(factoryOptions.LastEventIdStoreFactory);
        Assert.IsType<MongoLastEventIdStore>(factoryOptions.LastEventIdStoreFactory!(provider));
    }

    [Fact]
    public void AddMongoLastEventIdStore_RequestMutatorFactory_ProvidesLastEventIdRequestMutator()
    {
        // ARRANGE
        ServiceCollection services = new();
        services.AddSingleton(CreateMockMongoClient());
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddMongoLastEventIdStore(options => options.DatabaseName = "test-db");

        // ASSERT
        ServiceProvider provider = services.BuildServiceProvider();
        SseSourceFactoryOptions factoryOptions = provider
            .GetRequiredService<IOptionsMonitor<SseSourceFactoryOptions>>()
            .Get("MySource");

        Assert.Single(factoryOptions.RequestMutatorsFactories);
        Assert.IsType<LastEventIdRequestMutator>(factoryOptions.RequestMutatorsFactories[0](provider));
    }

    [Fact]
    public void AddMongoLastEventIdStore_ConfigureOptions_OptionsAreBoundToBuilderName()
    {
        // ARRANGE
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddMongoLastEventIdStore(options =>
        {
            options.DatabaseName = "my-custom-db";
            options.CollectionName = "my-collection";
            options.DocumentKey = "my-key";
        });

        // ASSERT — options must be retrievable by name, not by the default (empty) name
        ServiceProvider provider = services.BuildServiceProvider();
        MongoLastEventIdStoreOptions resolvedOptions = provider
            .GetRequiredService<IOptionsMonitor<MongoLastEventIdStoreOptions>>()
            .Get("MySource");

        Assert.Equal("my-custom-db", resolvedOptions.DatabaseName);
        Assert.Equal("my-collection", resolvedOptions.CollectionName);
        Assert.Equal("my-key", resolvedOptions.DocumentKey);
    }

    [Fact]
    public void AddMongoLastEventIdStore_WhenMongoClientFactorySet_DoesNotRequireClientInDI()
    {
        // ARRANGE — IMongoClient is intentionally NOT registered in DI
        ServiceCollection services = new();
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);
        bool factoryInvoked = false;

        // ACT
        builder.AddMongoLastEventIdStore(
            _ =>
            {
                factoryInvoked = true;
                return CreateMockMongoClient();
            }, 
            options =>
            {
                options.DatabaseName = "test-db";
            });

        // ASSERT — store resolves successfully using the factory; no DI client needed
        ServiceProvider provider = services.BuildServiceProvider();
        MongoLastEventIdStore store = provider.GetRequiredKeyedService<MongoLastEventIdStore>("MySource");

        Assert.IsType<MongoLastEventIdStore>(store);
        Assert.True(factoryInvoked);
    }

    [Fact]
    public void AddMongoLastEventIdStore_WhenClientInDIAndNoFactory_ResolveClientFromDI()
    {
        // ARRANGE
        ServiceCollection services = new();
        IMongoClient registeredClient = CreateMockMongoClient();
        services.AddSingleton(registeredClient);
        ISseSourceBuilder builder = new SseSourceBuilder("MySource", services);

        // ACT
        builder.AddMongoLastEventIdStore(options => options.DatabaseName = "test-db");

        // ASSERT — store resolves using the IMongoClient from DI
        ServiceProvider provider = services.BuildServiceProvider();
        MongoLastEventIdStore store = provider.GetRequiredKeyedService<MongoLastEventIdStore>("MySource");

        Assert.IsType<MongoLastEventIdStore>(store);
    }
    
    private static IMongoClient CreateMockMongoClient()
    {
        IAsyncCursor<string> emptyCursor = Substitute.For<IAsyncCursor<string>>();
        emptyCursor.MoveNext(Arg.Any<CancellationToken>()).Returns(false);

        IMongoCollection<LastEventIdDocument> collection = Substitute.For<IMongoCollection<LastEventIdDocument>>();
        collection
            .FindSync(
                Arg.Any<FilterDefinition<LastEventIdDocument>>(),
                Arg.Any<FindOptions<LastEventIdDocument, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(emptyCursor);

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
}

