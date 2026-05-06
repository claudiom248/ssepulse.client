using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SsePulse.Client.Core;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.Core.Internal;
using SsePulse.Client.DependencyInjection.Abstractions;
using SsePulse.Client.DependencyInjection.Internal;

namespace SsePulse.Client.DependencyInjection.Extensions;

/// <summary>
/// Extension methods on <see cref="ISseSourceBuilder"/> for common configurations related to last-event-ID tracking and JSON serializer options.
/// </summary>
public static class SseSourceBuilderExtensions
{
    /// <summary>
    /// Enables last-event-ID tracking for this SSE source using the built-in in-memory store.
    /// When a reconnection occurs, the <c>Last-Event-ID</c> header is automatically included
    /// so the server can resume from where it left off.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/last-event-id.html"/>
    /// </summary>
    /// <param name="builder">The builder for configuring the <see cref="SseSource"/></param>
    /// <returns>The same builder for chaining.</returns>
    public static ISseSourceBuilder AddLastEventId(this ISseSourceBuilder builder)
    {
        builder.Services.TryAddTransient<InMemoryLastEventIdStore>();
        builder.Services.TryAddTransient<ILastEventIdStore>(sp => sp.GetRequiredService<InMemoryLastEventIdStore>());
        builder.Services.Configure<SseSourceFactoryOptions>(builder.Name, options =>
        {
            options.LastEventIdStoreFactory = sp => sp.GetRequiredService<InMemoryLastEventIdStore>();
        });
        builder.Services.Configure<SseSourceFactoryOptions>(builder.Name, options =>
        {
            options.RequestMutatorsFactories.Add((sp) =>
                ActivatorUtilities.CreateInstance<LastEventIdRequestMutator>(sp, sp.GetRequiredService<ILastEventIdStore>()));
        });
        return builder;
    }

    /// <summary>
    /// Enables last-event-ID tracking for this SSE source using a custom <see cref="ILastEventIdStore"/>
    /// implementation resolved from the DI container. Register <typeparamref name="TEventIdStore"/>
    /// in the container before calling this method.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/last-event-id.html"/>
    /// </summary>
    /// <typeparam name="TEventIdStore">The custom store type that persists the last-event-ID.</typeparam>
    /// <param name="builder">The builder for configuring the <see cref="SseSource"/></param>
    /// <returns>The same builder for chaining.</returns>
    public static ISseSourceBuilder AddLastEventId<TEventIdStore>(this ISseSourceBuilder builder)
        where TEventIdStore : class, ILastEventIdStore
    {
        return AddLastEventIdCore<TEventIdStore>(builder, fromKeyed: false);
    }

    /// <summary>
    /// Enables last-event-ID tracking for this SSE source using <see cref="FileLastEventIdStore"/>,
    /// which persists the last received event ID to a local file so that the SSE stream can be
    /// resumed after a process restart.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/last-event-id.html"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The store is registered as a keyed singleton scoped to this source's name. Use
    /// <paramref name="configureOptions"/> to set the file path and choose a flush strategy:
    /// </para>
    /// <para>
    /// <b>Resolving multiple instances of the same <see cref="SseSource"/> with this store will share the same file.
    /// You should instantiate only one instance per source.</b>
    /// </para>
    /// </remarks>
    /// <param name="builder">The builder for configuring the <see cref="SseSource"/>.</param>
    /// <param name="configureOptions">
    /// A delegate that configures <see cref="FileLastEventIdStoreOptions"/>.
    /// </param>
    /// <returns>The same builder for chaining.</returns>
    public static ISseSourceBuilder AddFileLastEventIdStore(this ISseSourceBuilder builder,
        Action<FileLastEventIdStoreOptions> configureOptions)
    {
        builder.Services.Configure(builder.Name, configureOptions);
        builder.Services.TryAddKeyedSingleton<FileLastEventIdStore>(
            builder.Name,
            (sp, _) => ActivatorUtilities.CreateInstance<FileLastEventIdStore>(
                sp,
                sp.GetRequiredService<IOptionsMonitor<FileLastEventIdStoreOptions>>().Get(builder.Name)));
        return AddLastEventIdCore<FileLastEventIdStore>(builder, true);
    }

    /// <summary>
    /// Configures custom JSON serializer options used by this SSE source when
    /// serializing and deserializing event payloads.
    /// </summary>
    /// <param name="builder">The builder for configuring the <see cref="SseSource"/></param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> instance to apply.</param>
    /// <returns>The same builder for chaining.</returns>
    public static ISseSourceBuilder WithSerializerOptions(this ISseSourceBuilder builder, JsonSerializerOptions options)
    {
        builder.Services.Configure<SseSourceOptions>(builder.Name, opts =>
        {
            opts.JsonSerializerOptions = options;
        });
        return builder;
    }
    
    private static ISseSourceBuilder AddLastEventIdCore<TEventIdStore>(ISseSourceBuilder builder, bool fromKeyed = false)
        where TEventIdStore : class, ILastEventIdStore
    {
        Func<IServiceProvider, ILastEventIdStore>? getStore = fromKeyed
            ? sp => sp.GetRequiredKeyedService<TEventIdStore>(builder.Name)
            : sp => sp.GetRequiredService<TEventIdStore>();
        builder.Services.Configure<SseSourceFactoryOptions>(builder.Name, options =>
        {
            options.LastEventIdStoreFactory = getStore;
        });
        builder.Services.Configure<SseSourceFactoryOptions>(builder.Name, options =>
        {
            options.RequestMutatorsFactories.Add((sp) =>
                ActivatorUtilities.CreateInstance<LastEventIdRequestMutator>(sp, getStore(sp)));
        });
        return builder;
    }
}