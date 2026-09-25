using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SsePulse.Client.DependencyInjection.Abstractions;
using SsePulse.Client.DependencyInjection.Extensions;

namespace SsePulse.Client.Extensions.Stores.DistributedCache.DependencyInjection;

/// <summary>
/// Extension methods on <see cref="ISseSourceBuilder"/> for registering
/// <see cref="DistributedCacheLastEventIdStore"/> as the last-event-ID persistence store.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/store-distributed-cache.html"/>
/// </summary>
public static class SseSourceBuilderExtensions
{
    /// <summary>
    /// Enables last-event-ID tracking for this SSE source using
    /// <see cref="DistributedCacheLastEventIdStore"/>, resolving the <see cref="IDistributedCache"/>
    /// from the dependency-injection container. An <see cref="IDistributedCache"/> must be
    /// registered before the source is created (for example via <c>AddDistributedMemoryCache()</c>
    /// or any other distributed-cache integration).
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/store-distributed-cache.html"/>
    /// </summary>
    /// <param name="builder">The <see cref="ISseSourceBuilder"/> used to configure the SSE source.</param>
    /// <param name="configureOptions">A delegate that configures the store options, such as the cache key.</param>
    /// <returns>The same builder for chaining.</returns>
    public static ISseSourceBuilder AddDistributedCacheLastEventIdStore(
        this ISseSourceBuilder builder,
        Action<DistributedCacheLastEventIdStoreOptions> configureOptions)
    {
        builder.Services.Configure(builder.Name, configureOptions);
        builder.Services.AddKeyedTransient(builder.Name, (sp, _) =>
        {
            DistributedCacheLastEventIdStoreOptions options =
                sp.GetRequiredService<IOptionsMonitor<DistributedCacheLastEventIdStoreOptions>>().Get(builder.Name);
            IDistributedCache cache = sp.GetRequiredService<IDistributedCache>();
            return ActivatorUtilities.CreateInstance<DistributedCacheLastEventIdStore>(sp, cache, options);
        });
        builder.AddLastEventId(sp => sp.GetRequiredKeyedService<DistributedCacheLastEventIdStore>(builder.Name));
        return builder;
    }

    /// <summary>
    /// Enables last-event-ID tracking for this SSE source using
    /// <see cref="DistributedCacheLastEventIdStore"/>, obtaining the <see cref="IDistributedCache"/>
    /// from the provided factory delegate. Use this overload when the cache instance requires custom
    /// configuration or must be resolved from a named service, keyed service, or external factory.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/store-distributed-cache.html"/>
    /// </summary>
    /// <param name="builder">The <see cref="ISseSourceBuilder"/> used to configure the SSE source.</param>
    /// <param name="factory">
    /// A factory delegate that receives the <see cref="IServiceProvider"/> and returns the
    /// <see cref="IDistributedCache"/> to use. The lifetime of the returned instance is controlled
    /// by the caller.
    /// </param>
    /// <param name="configureOptions">A delegate that configures the store options, such as the cache key.</param>
    /// <returns>The same builder for chaining.</returns>
    public static ISseSourceBuilder AddDistributedCacheLastEventIdStore(
        this ISseSourceBuilder builder,
        Func<IServiceProvider, IDistributedCache> factory,
        Action<DistributedCacheLastEventIdStoreOptions> configureOptions)
    {
        builder.Services.Configure(builder.Name, configureOptions);
        builder.Services.AddKeyedTransient(builder.Name, (sp, _) =>
        {
            DistributedCacheLastEventIdStoreOptions options =
                sp.GetRequiredService<IOptionsMonitor<DistributedCacheLastEventIdStoreOptions>>().Get(builder.Name);
            return ActivatorUtilities.CreateInstance<DistributedCacheLastEventIdStore>(sp, factory(sp), options);
        });
        builder.AddLastEventId(sp => sp.GetRequiredKeyedService<DistributedCacheLastEventIdStore>(builder.Name));
        return builder;
    }
}