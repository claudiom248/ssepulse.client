using DotNet.Testcontainers.Builders;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace SsePulse.Client.Extensions.Stores.Mongo.IntegrationTests;

/// <summary>
/// xUnit collection fixture that starts a single MongoDB container for the entire
/// integration-test run and exposes a ready-to-use <see cref="IMongoClient"/>.
/// </summary>
public sealed class MongoContainerFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:8")
        .Build();

    /// <summary>Gets a <see cref="IMongoClient"/> connected to the running container.</summary>
    public IMongoClient MongoClient { get; private set; } = null!;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        MongoClient = new MongoClient(_container.GetConnectionString());
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        MongoClient.Dispose();
        await _container.DisposeAsync();
    }
}

/// <summary>Groups all <see cref="MongoLastEventIdStore"/> integration tests under a single container.</summary>
[CollectionDefinition(Name)]
public sealed class MongoContainerCollection : ICollectionFixture<MongoContainerFixture>
{
    /// <summary>The name used to annotate test classes that belong to this collection.</summary>
    public const string Name = "MongoContainer";
}


