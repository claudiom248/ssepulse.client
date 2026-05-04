using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SsePulse.Client.Abstractions;
using SsePulse.Client.Core;
using SsePulse.Client.DependencyInjection.Abstractions;

namespace SsePulse.Client.Hosting.DependencyInjection.Extensions;

/// <summary>
/// Extension methods to add hosted-service registrations for the current <see cref="SseSource"/> registration.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/hosted-services.html"/>
/// </summary>
public static class SseSourceBuilderExtensions
{
    /// <summary>
    /// Registers the default <see cref="SseSourceHostedService"/> for the current <see cref="SseSource"/> registration.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/hosted-services.html"/>
    /// </summary>
    /// <param name="builder">The builder for configuring the <see cref="SseSource"/></param>
    /// <returns>The same builder for chaining.</returns>
    public static ISseSourceBuilder AddHostedService(this ISseSourceBuilder builder)
    {
        builder.Services.AddSingleton<IHostedService>(sp =>
        {
            ISseSourceFactory factory = sp.GetRequiredService<ISseSourceFactory>();
            return ActivatorUtilities.CreateInstance<SseSourceHostedService>(
                sp,
                factory.CreateSseSource(builder.Name));
        });
        return builder;
    }

    /// <summary>
    /// Registers a custom hosted service type for the current <see cref="SseSource"/> registration.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/hosted-services.html"/>
    /// </summary>
    /// <typeparam name="THostedService">The hosted service type to create through dependency injection.</typeparam>
    /// <param name="builder">The builder for configuring the <see cref="SseSource"/></param>
    /// <returns>The same builder for chaining.</returns>
    public static ISseSourceBuilder AddHostedService<
#if NET8_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
#endif
        THostedService>(this ISseSourceBuilder builder) where THostedService : BackgroundService
    {
        builder.Services.AddHostedService<THostedService>(sp =>
        {
            ISseSourceFactory factory = sp.GetRequiredService<ISseSourceFactory>();
            return ActivatorUtilities.CreateInstance<THostedService>(
                sp, 
                factory.CreateSseSource(builder.Name));
        });
        return builder;
    }
    
    /// <summary>
    /// Registers a custom hosted service factory for the current <see cref="SseSource"/> registration.
    /// <br/><br/>
    /// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/hosted-services.html"/>
    /// </summary>
    /// <typeparam name="THostedService">The hosted service type returned by <paramref name="factory"/>.</typeparam>
    /// <param name="builder">The builder for configuring the <see cref="SseSource"/></param>
    /// <param name="factory">Factory used to create the hosted service instance.</param>
    /// <returns>The same builder for chaining.</returns>
    public static ISseSourceBuilder AddHostedService<
#if NET8_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
#endif
        THostedService>(this ISseSourceBuilder builder, Func<IServiceProvider, THostedService> factory) where THostedService : BackgroundService
    {
        builder.Services.AddHostedService(factory);
        return builder;
    }
}