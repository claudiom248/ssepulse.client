using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SsePulse.Client.Core;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.DependencyInjection.Abstractions;
using SsePulse.Client.DependencyInjection.Internal;

namespace SsePulse.Client.DependencyInjection;

/// <summary>
/// Default implementation of <see cref="ISseSourceBuilder"/> that registers SSE source
/// components into an <see cref="IServiceCollection"/>.
/// Instances are created by the <c>AddSseSource</c> extension methods and returned to the
/// caller for further configuration.
/// </summary>
public class SseSourceBuilder : ISseSourceBuilder
{
    /// <inheritdoc/>
    public IServiceCollection Services { get; }

    /// <inheritdoc/>
    public IConfiguration? Configuration { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <summary>
    /// Initializes a new <see cref="SseSourceBuilder"/> with a named registration and an options-configure delegate.
    /// </summary>
    /// <param name="name">Unique name for this SSE source.</param>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configureOptions">Delegate used to configure <see cref="SseSourceOptions"/>.</param>
    public SseSourceBuilder(string name, IServiceCollection services, Action<SseSourceOptions> configureOptions)
        : this(name, services)
    {
        Services.Configure<SseSourceOptions>(Name, options =>
        {
            options.Name = Name;
            configureOptions(options);
        });
    }

    /// <summary>
    /// Initializes a new <see cref="SseSourceBuilder"/> using the default source name and an options-configure delegate.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configureOptions">Delegate used to configure <see cref="SseSourceOptions"/>.</param>
    public SseSourceBuilder(IServiceCollection services, Action<SseSourceOptions> configureOptions)
        : this(Constants.DefaultSourceName, services, configureOptions)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="SseSourceBuilder"/> using the default source name and an optional configuration section.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Optional configuration section to bind <see cref="SseSourceOptions"/> from.</param>
    public SseSourceBuilder(IServiceCollection services, IConfiguration? configuration = null)
        : this(Constants.DefaultSourceName, services, configuration)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="SseSourceBuilder"/> with a named registration and an optional configuration section.
    /// </summary>
    /// <param name="name">Unique name for this SSE source.</param>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Optional configuration section to bind <see cref="SseSourceOptions"/> from.</param>
    public SseSourceBuilder(string name,
        IServiceCollection services,
        IConfiguration? configuration = null)
    {
        Configuration = configuration;
        Services = services;
        Name = name;

        Services.AddOptions<SseSourceFactoryOptions>(Name);
        services.Configure<SseSourceOptions>(Name, options => { options.Name = Name; });
        if (Configuration is not null)
        {
            Services.AddOptions<SseSourceOptions>(Name)
                .Bind(Configuration);
        }
    }

    /// <inheritdoc/>
    public SseSourceBuilder AddHttpClient()
    {
        return AddHttpClient(_ => { });
    }

    /// <inheritdoc/>
    public SseSourceBuilder AddHttpClient(Action<HttpClient> configureClient)
    {
        return AddHttpClient(configureClient, null);
    }

    /// <inheritdoc/>
    public SseSourceBuilder AddHttpClient(Action<HttpClient>? configureClient,
        Action<IHttpClientBuilder>? clientBuilder)
    {
        IHttpClientBuilder builder = Services.AddHttpClient(Name, configureClient ?? (_ => { }));
        clientBuilder?.Invoke(builder);
        Services.Configure<SseSourceFactoryOptions>(Name, options => { options.ClientName = Name; });
        return this;
    }

    /// <inheritdoc/>
    public SseSourceBuilder UseHttpClient(string clientName)
    {
        Services.Configure<SseSourceFactoryOptions>(Name,
            options => { options.ClientName = clientName; });
        return this;
    }

    /// <inheritdoc/>
    public ISseSourceBuilder RegisterHandlers(Action<IServiceProvider, SseSource> registerHandlers)
    {
        Services.Configure<SseSourceFactoryOptions>(Name,
            options => { options.RegisterHandlersAction = registerHandlers; });
        return this;
    }

    /// <inheritdoc/>
    public ISseSourceBuilder BindEventsManager(ISseEventsManager manager)
    {
        Services.Configure<SseSourceFactoryOptions>(Name,
            options => { options.EventManagerFactories.Add(_ => manager); });
        return this;
    }

    /// <inheritdoc/>
    public ISseSourceBuilder BindEventsManager<TManager>() where TManager : ISseEventsManager
    {
        Services.Configure<SseSourceFactoryOptions>(Name,
            options => { options.EventManagerFactories.Add(sp => sp.GetRequiredService<TManager>()); });
        return this;
    }

    /// <inheritdoc/>
    public ISseSourceBuilder BindEventsManager(Func<IServiceProvider, ISseEventsManager> managerFactory)
    {
        Services.Configure<SseSourceFactoryOptions>(Name,
            options => { options.EventManagerFactories.Add(managerFactory); });
        return this;
    }

    /// <inheritdoc/>
    public ISseSourceBuilder AddRequestMutator(IRequestMutator mutator)
    {
        Services.Configure<SseSourceFactoryOptions>(Name,
            options => { options.RequestMutatorsFactories.Add(_ => mutator); });
        return this;
    }

    /// <inheritdoc/>
    public ISseSourceBuilder AddRequestMutator<TRequestMutator>() where TRequestMutator : IRequestMutator
    {
        Services.Configure<SseSourceFactoryOptions>(Name,
            options => { options.RequestMutatorsFactories.Add(sp => sp.GetRequiredService<TRequestMutator>()); });
        return this;
    }

    /// <inheritdoc/>
    public ISseSourceBuilder AddRequestMutator(Func<IServiceProvider, IRequestMutator> mutatorFactory)
    {
        Services.Configure<SseSourceFactoryOptions>(Name,
            options => { options.RequestMutatorsFactories.Add(mutatorFactory); });
        return this;
    }
}