using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using SsePulse.Client.DependencyInjection.Abstractions;
using SsePulse.Client.DependencyInjection.Extensions;

namespace SsePulse.Client.Extensions.Stores.Mongo.DependencyInjection;

/// <summary>
/// Extension methods on <see cref="ISseSourceBuilder"/> for registering
/// <see cref="MongoLastEventIdStore"/> as the last-event-ID persistence store.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/last-event-id-stores.html"/>
/// </summary>
public static class SseSourceBuilderExtensions
{
    /// <summary>
    /// Enables last-event-ID tracking for this SSE source using <see cref="MongoLastEventIdStore"/>, 
    /// connecting to MongoDB via the supplied <paramref name="connectionString"/>.
    /// A <see cref="IMongoClient"/> is created and owned by the internal factory; it is shared
    /// with all sources that specify the same connection string and is disposed when the
    /// <see cref="IServiceProvider"/> is disposed.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/last-event-id-stores.html"/>
    /// </summary>
    /// <param name="builder">The <see cref="ISseSourceBuilder"/> used to configure the SSE source.</param>
    /// <param name="connectionString">The MongoDB connection string used to create the client.</param>
    /// <param name="configureOptions">A delegate that configures the store options, such as the database name and collection name.</param>
    /// <returns>The same builder for chaining.</returns>
    public static ISseSourceBuilder AddMongoLastEventIdStore(
        this ISseSourceBuilder builder, 
        string connectionString,
        Action<MongoLastEventIdStoreOptions> configureOptions)
    {
        builder.Services.Configure(builder.Name, configureOptions);
        builder.Services.AddSingleton<MongoLastEventIdStoreFactory>();
        builder.Services.AddKeyedTransient(builder.Name, (sp, _) =>
        {
            MongoLastEventIdStoreFactory factory = sp.GetRequiredService<MongoLastEventIdStoreFactory>();
            return factory.Create(builder.Name, connectionString);
        });
        return builder.AddLastEventId(sp => sp.GetRequiredKeyedService<MongoLastEventIdStore>(builder.Name));
    }
    
    /// <summary>
    /// Enables last-event-ID tracking for this SSE source using <see cref="MongoLastEventIdStore"/>, 
    /// resolving the <see cref="IMongoClient"/> from the dependency-injection container.
    /// An <see cref="IMongoClient"/> must be registered in the container before the source is created.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/last-event-id-stores.html"/>
    /// </summary>
    /// <param name="builder">The <see cref="ISseSourceBuilder"/> used to configure the SSE source.</param>
    /// <param name="configureOptions">A delegate that configures the store options, such as the database name and collection name.</param>
    /// <returns>The same builder for chaining.</returns>
    public static ISseSourceBuilder AddMongoLastEventIdStore(
        this ISseSourceBuilder builder,
        Action<MongoLastEventIdStoreOptions> configureOptions)
    {
        builder.Services.Configure(builder.Name, configureOptions);
        builder.Services.AddSingleton<MongoLastEventIdStoreFactory>();
        builder.Services.AddKeyedTransient(builder.Name, (sp, _) =>
        {
            MongoLastEventIdStoreFactory factory = sp.GetRequiredService<MongoLastEventIdStoreFactory>();
            return factory.Create(builder.Name, _ => sp.GetRequiredService<IMongoClient>());
        });
        return builder.AddLastEventId(sp => sp.GetRequiredKeyedService<MongoLastEventIdStore>(builder.Name));
    }
    
    /// <summary>
    /// Enables last-event-ID tracking for this SSE source using <see cref="MongoLastEventIdStore"/>, 
    /// obtaining the <see cref="IMongoClient"/> from the provided factory delegate.
    /// Use this overload when the client requires custom configuration or must be resolved from
    /// a named service, keyed service, or external factory.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/last-event-id-stores.html"/>
    /// </summary>
    /// <param name="builder">The <see cref="ISseSourceBuilder"/> used to configure the SSE source.</param>
    /// <param name="mongoClientFactory">
    /// A factory delegate that receives the <see cref="IServiceProvider"/> and returns the
    /// <see cref="IMongoClient"/> to use. The lifetime of the returned client is controlled
    /// by the caller.
    /// </param>
    /// <param name="configureOptions">A delegate that configures the store options, such as the database name and collection name.</param>
    /// <returns>The same builder for chaining.</returns>
    public static ISseSourceBuilder AddMongoLastEventIdStore(
        this ISseSourceBuilder builder,
        Func<IServiceProvider, IMongoClient> mongoClientFactory,
        Action<MongoLastEventIdStoreOptions> configureOptions)
    {
        builder.Services.Configure(builder.Name, configureOptions);
        builder.Services.AddSingleton<MongoLastEventIdStoreFactory>();
        builder.Services.AddKeyedTransient(builder.Name, (sp, _) =>
        {
            MongoLastEventIdStoreFactory factory = sp.GetRequiredService<MongoLastEventIdStoreFactory>();
            return factory.Create(builder.Name, mongoClientFactory);
        });
        return builder.AddLastEventId(sp => sp.GetRequiredKeyedService<MongoLastEventIdStore>(builder.Name));
    }
}