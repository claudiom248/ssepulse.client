using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SsePulse.Client.Abstractions;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.DependencyInjection.Abstractions;
using SsePulse.Client.DependencyInjection.Internal;

namespace SsePulse.Client.DependencyInjection.Extensions;

/// <summary>
/// Extension methods on <see cref="IServiceCollection"/> for registering SSE sources.
/// </summary>
public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a default-named SSE source with no initial configuration.
    /// Call methods on the returned <see cref="ISseSourceBuilder"/> to configure the HTTP client,
    /// event handlers, and other components.
    /// </summary>
    /// <param name="services">The service collection to add the SSE source to.</param>
    /// <returns>An <see cref="ISseSourceBuilder"/> for further configuration.</returns>
    public static ISseSourceBuilder AddSseSource(this IServiceCollection services)
    {
        return services.AddSseSource(Constants.DefaultSourceName);
    }

    /// <summary>
    /// Registers a default-named SSE source and binds its options from <paramref name="configuration"/>.
    /// </summary>
    /// <param name="services">The service collection to add the SSE source to.</param>
    /// <param name="configuration">Configuration section containing <see cref="SseSourceOptions"/> values.</param>
    /// <returns>An <see cref="ISseSourceBuilder"/> for further configuration.</returns>
    public static ISseSourceBuilder AddSseSource(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddSseSource(Constants.DefaultSourceName, configuration);
    }

    /// <summary>
    /// Registers a named SSE source and optionally binds its options from <paramref name="configuration"/>.
    /// </summary>
    /// <param name="services">The service collection to add the SSE source to.</param>
    /// <param name="name">Unique name for this SSE source. Use this name when resolving the source via <see cref="SsePulse.Client.Abstractions.ISseSourceFactory"/>.</param>
    /// <param name="configuration">Optional configuration section. When <see langword="null"/>, default option values are used.</param>
    /// <returns>An <see cref="ISseSourceBuilder"/> for further configuration.</returns>
    public static ISseSourceBuilder AddSseSource(this IServiceCollection services, string name, IConfiguration? configuration = null)
    {
        return services.AddSseSourceCore(name, configuration);
    }

    /// <summary>
    /// Registers a default-named SSE source and configures its options using <paramref name="configureOptions"/>.
    /// </summary>
    /// <param name="services">The service collection to add the SSE source to.</param>
    /// <param name="configureOptions">Delegate to configure <see cref="SseSourceOptions"/>.</param>
    /// <returns>An <see cref="ISseSourceBuilder"/> for further configuration.</returns>
    public static ISseSourceBuilder AddSseSource(this IServiceCollection services, Action<SseSourceOptions> configureOptions)
    {
        return services.AddSseSource(Constants.DefaultSourceName, configureOptions);
    }

    /// <summary>
    /// Registers a named SSE source and configures its options using <paramref name="configureOptions"/>.
    /// </summary>
    /// <param name="services">The service collection to add the SSE source to.</param>
    /// <param name="name">Unique name for this SSE source.</param>
    /// <param name="configureOptions">Delegate to configure <see cref="SseSourceOptions"/>.</param>
    /// <returns>An <see cref="ISseSourceBuilder"/> for further configuration.</returns>
    public static ISseSourceBuilder AddSseSource(this IServiceCollection services, string name, Action<SseSourceOptions> configureOptions)
    {
        return services.AddSseSourceCore(name, configureOptions: configureOptions);
    }

    private static SseSourceBuilder AddSseSourceCore(this IServiceCollection services, string name,
        IConfiguration? configuration = null,
        Action<SseSourceOptions>? configureOptions = null)
    {
        services.TryAddSingleton<DefaultSseSourceFactory>();
        services.TryAddSingleton<ISseSourceFactory>(sp => sp.GetRequiredService<DefaultSseSourceFactory>());

        SseSourceRegistrationService registrationService = services.GetSseSourceRegistrationService();
        if (!registrationService.TryRegister(name))
        {
            return registrationService.SourceBuildersCache[name];
        }
        SseSourceBuilder sourceBuilder = configureOptions is not null
            ? new SseSourceBuilder(name, services, configureOptions)
            : new SseSourceBuilder(name, services, configuration);
        registrationService.SourceBuildersCache.Add(name, sourceBuilder);
        return sourceBuilder;
    }

    private static SseSourceRegistrationService GetSseSourceRegistrationService(this IServiceCollection services)
    {
        SseSourceRegistrationService? registrationService =
            (SseSourceRegistrationService?)services
                .FirstOrDefault(s => s.ServiceType == typeof(SseSourceRegistrationService))?.ImplementationInstance;
        if (registrationService is not null)
        {
            return registrationService;
        }
        registrationService = new SseSourceRegistrationService(services);
        services.AddSingleton(registrationService);
        return registrationService;
    }
}