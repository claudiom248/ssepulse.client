using Microsoft.Extensions.DependencyInjection;
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

    public DefaultSseSourceFactory(
        IServiceProvider serviceProvider, 
        IOptionsMonitor<SseSourceOptions> sourceOptions,
        IOptionsMonitor<SseSourceFactoryOptions> sourceFactoryOptions)
    {
        _serviceProvider = serviceProvider;
        _sourceOptions = sourceOptions;
        _sourceFactoryOptions = sourceFactoryOptions;
    }

    public SseSource CreateSseSource(string? name)
    {
        name ??= Constants.DefaultSourceName;
        SseSourceOptions options = _sourceOptions.Get(name);
        SseSourceFactoryOptions sourceFactoryOptions = _sourceFactoryOptions.Get(name);
        ILastEventIdStore? store = sourceFactoryOptions.LastEventIdStoreFactory?.Invoke(_serviceProvider);
        IReadOnlyCollection<IRequestMutator> mutators = BuildMutators();
        ILoggerFactory? loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
        SseSource source = new(
            _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(sourceFactoryOptions.ClientName ?? name), 
            options, 
            mutators,
            store,
            loggerFactory?.CreateLogger<SseSource>());
        BindEventsManagers();
        sourceFactoryOptions.RegisterHandlersAction?.Invoke(_serviceProvider, source);
        return source;

        IReadOnlyCollection<IRequestMutator> BuildMutators() =>
            sourceFactoryOptions.RequestMutatorsFactories
                .Select(registration => registration.Invoke(_serviceProvider))
                .ToList()
                .AsReadOnly();

        void BindEventsManagers()
        {
            foreach (Func<IServiceProvider, ISseEventsManager>? managerFactory in sourceFactoryOptions.EventManagerFactories)
            {
                source.Bind(() => managerFactory(_serviceProvider));
            }
        }
    }
}