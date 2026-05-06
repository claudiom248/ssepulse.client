using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SsePulse.Client.Abstractions;
using SsePulse.Client.Core;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Configurations;

namespace SsePulse.Client.DependencyInjection.Internal;

internal class DefaultSseSourceFactory : ISseSourceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<SseSourceOptions> _sourceOptions;
    private readonly IOptionsMonitor<SseSourceFactoryOptions> _sourceFactoryOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory? _loggerFactory;

    public DefaultSseSourceFactory(
        IServiceProvider serviceProvider, 
        IOptionsMonitor<SseSourceOptions> sourceOptions,
        IOptionsMonitor<SseSourceFactoryOptions> sourceFactoryOptions,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory? loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _sourceOptions = sourceOptions;
        _sourceFactoryOptions = sourceFactoryOptions;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
    }

    public SseSource CreateSseSource(string? name)
    {
        name ??= Constants.DefaultSourceName;
        SseSourceFactoryOptions sourceFactoryOptions = _sourceFactoryOptions.Get(name);
        SseSourceFactoryServiceProvider serviceProvider = new(_serviceProvider);
        ILastEventIdStore? store = sourceFactoryOptions.LastEventIdStoreFactory?.Invoke(serviceProvider);
        IReadOnlyCollection<IRequestMutator> mutators = BuildMutators();
        SseSource source = new(
            _httpClientFactory.CreateClient(sourceFactoryOptions.ClientName ?? name), 
            _sourceOptions.Get(name), 
            mutators,
            store,
            _loggerFactory?.CreateLogger<SseSource>());
        BindEventsManagers();
        sourceFactoryOptions.RegisterHandlersAction?.Invoke(serviceProvider, source);
        
        return source;

        IReadOnlyCollection<IRequestMutator> BuildMutators() =>
            sourceFactoryOptions.RequestMutatorsFactories
                .Select(registration => registration.Invoke(serviceProvider))
                .ToList()
                .AsReadOnly();

        void BindEventsManagers()
        {
            foreach (Func<IServiceProvider, ISseEventsManager>? managerFactory in sourceFactoryOptions.EventManagerFactories)
            {
                source.Bind(() => managerFactory(serviceProvider));
            }
        }
    }
}