using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Testcontainers.Redis;
namespace SsePulse.Client.Extensions.Stores.DistributedCache.IntegrationTests;
/// <summary>
/// xUnit collection fixture that starts a single Redis container for the entire
/// integration-test run and exposes a ready-to-use <see cref="IDistributedCache"/>.
/// </summary>
public sealed class RedisContainerFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder("redis:6").Build();
    /// <summary>Gets an <see cref="IDistributedCache"/> backed by the running Redis container.</summary>
    public IDistributedCache Cache { get; private set; } = null!;
    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Cache = new RedisCache(new RedisCacheOptions
        {
            Configuration = _container.GetConnectionString()
        });
    }
    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (Cache is IDisposable disposable)
        {
            disposable.Dispose();
        }
        await _container.DisposeAsync();
    }
}
/// <summary>Groups all <see cref="DistributedCacheLastEventIdStore"/> integration tests under a single container.</summary>
[CollectionDefinition(Name)]
public sealed class RedisContainerCollection : ICollectionFixture<RedisContainerFixture>
{
    /// <summary>The name used to annotate test classes that belong to this collection.</summary>
    public const string Name = "RedisContainer";
}
