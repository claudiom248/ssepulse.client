using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        builder.Services.TryAddTransient<LastEventIdRequestMutator>();
        builder.AddRequestMutator<LastEventIdRequestMutator>();
        return builder;
    }

    /// <summary>
    /// Enables last-event-ID tracking for this SSE source using a custom <see cref="ILastEventIdStore"/>
    /// implementation resolved from the DI container. Register <typeparamref name="TEventIdStore"/>
    /// in the container before calling this method.
    /// </summary>
    /// <typeparam name="TEventIdStore">The custom store type that persists the last-event-ID.</typeparam>
    /// <param name="builder">The builder for configuring the <see cref="SseSource"/></param>
    /// <returns>The same builder for chaining.</returns>
    public static ISseSourceBuilder AddLastEventId<TEventIdStore>(this ISseSourceBuilder builder)
        where TEventIdStore : class, ILastEventIdStore
    {
        builder.Services.Configure<SseSourceFactoryOptions>(builder.Name, options =>
        {
            options.LastEventIdStoreFactory = sp => sp.GetRequiredService<TEventIdStore>();
        });
        builder.AddRequestMutator(sp => new LastEventIdRequestMutator(sp.GetRequiredService<TEventIdStore>()));
        return builder;
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
}