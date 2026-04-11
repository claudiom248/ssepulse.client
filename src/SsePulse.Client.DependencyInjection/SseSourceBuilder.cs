using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SsePulse.Client.Core;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.DependencyInjection.Abstractions;
using SsePulse.Client.DependencyInjection.Internal;

namespace SsePulse.Client.DependencyInjection;

public class SseSourceBuilder : ISseSourceBuilder
{
    public IServiceCollection Services { get; }

    public IConfiguration? Configuration { get; }

    public string Name { get; }

    public SseSourceBuilder(string name, IServiceCollection services, Action<SseSourceOptions> configureOptions)
        : this(name, services)
    {
        Services.Configure(Name, configureOptions);
    }

    public SseSourceBuilder(IServiceCollection services, Action<SseSourceOptions> configureOptions)
        : this(Constants.DefaultSourceName, services, configureOptions)
    {
    }

    public SseSourceBuilder(IServiceCollection services, IConfiguration? configuration = null)
        : this(Constants.DefaultSourceName, services, configuration)
    {
    }

    public SseSourceBuilder(string name,
        IServiceCollection services,
        IConfiguration? configuration = null)
    {
        Configuration = configuration;
        Services = services;
        Name = name;

        Services.AddOptions<SseSourceFactoryOptions>(Name);
        
        if (Configuration is not null)
        {
            Services.AddOptions<SseSourceOptions>(Name)
                .Bind(Configuration);
        }
    }

    public SseSourceBuilder AddHttpClient()
    {
        return AddHttpClient(_ => { });
    }
    
    public SseSourceBuilder AddHttpClient(Action<HttpClient> configureClient)
    {
        return AddHttpClient(configureClient, null);
    }

    public SseSourceBuilder AddHttpClient(Action<HttpClient>? configureClient,
        Action<IHttpClientBuilder>? clientBuilder)
    {
        IHttpClientBuilder builder = Services.AddHttpClient(Name, configureClient ?? (_ => { }));
        clientBuilder?.Invoke(builder);
        return this;
    }

    ISseSourceBuilder ISseSourceBuilder.AddRequestMutator(IRequestMutator mutator)
    {
        Services.Configure<SseSourceFactoryOptions>(Name,
            options => { options.RequestMutatorsFactories.Add(_ => mutator); });
        return this;
    }

    ISseSourceBuilder ISseSourceBuilder.AddRequestMutator<TRequestMutator>()
    {
        Services.Configure<SseSourceFactoryOptions>(Name,
            options => { options.RequestMutatorsFactories.Add(sp => sp.GetRequiredService<TRequestMutator>()); });
        return this;
    }

    ISseSourceBuilder ISseSourceBuilder.AddRequestMutator(Func<IServiceProvider, IRequestMutator> mutatorFactory)
    {
        Services.Configure<SseSourceFactoryOptions>(Name,
            options => { options.RequestMutatorsFactories.Add(mutatorFactory); });
        return this;
    }

    public ISseSourceBuilder BindEventsManager(ISseEventsManager manager)
    {
        Services.Configure<SseSourceFactoryOptions>(Name,
            options => { options.EventManagerFactories.Add(_ => manager); });
        return this;
    }

    public ISseSourceBuilder BindEventsManager<TManager>() where TManager : ISseEventsManager
    {
        Services.Configure<SseSourceFactoryOptions>(Name,
            options => { options.EventManagerFactories.Add(sp => sp.GetRequiredService<TManager>()); });
        return this;
    }
    
    public ISseSourceBuilder BindEventsManager(Func<IServiceProvider, ISseEventsManager> managerFactory)
    {
        Services.Configure<SseSourceFactoryOptions>(Name,
            options => { options.EventManagerFactories.Add(managerFactory); });
        return this;
    }
    
    public ISseSourceBuilder RegisterHandlers(Action<IServiceProvider, SseSource> registerHandlers)
    {
        Services.Configure<SseSourceFactoryOptions>(Name,
            options =>
            {
                options.RegisterHandlersAction = registerHandlers;
            });
        return this;
    }
}