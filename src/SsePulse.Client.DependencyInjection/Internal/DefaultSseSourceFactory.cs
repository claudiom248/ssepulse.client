using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SsePulse.Client.Abstractions;
using SsePulse.Client.Core;
using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.DependencyInjection.Internal;

internal class DefaultSseSourceFactory : ISseSourceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<SseSourceFactoryOptions> _optionsMonitor;

    public DefaultSseSourceFactory(IServiceProvider serviceProvider, IOptionsMonitor<SseSourceFactoryOptions> optionsMonitor)
    {
        _serviceProvider = serviceProvider;
        _optionsMonitor = optionsMonitor;
    }

    public SseSource CreateSseSource(string? name)
    {
        name ??= Constants.DefaultSourceName;
        SseSourceFactoryOptions options = _optionsMonitor.Get(name);
        IReadOnlyCollection<IRequestMutator> mutators = BuildMutators();
        ILastEventIdStore? store = options.LastEventIdStoreFactory?.Invoke(_serviceProvider);
        ILoggerFactory? loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
        SseSource source = new(
            _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(name), 
            options, 
            mutators,
            store,
            loggerFactory?.CreateLogger<SseSource>());
        BindEventsManagers();
        return source;

        IReadOnlyCollection<IRequestMutator> BuildMutators()
        {
            IReadOnlyCollection<IRequestMutator> readOnlyCollection = options.RequestMutatorsFactories
                .Select(registration => registration.Invoke(_serviceProvider))
                .ToList()
                .AsReadOnly();
            return readOnlyCollection;
        }
        
        void BindEventsManagers()
        {
            foreach (Func<IServiceProvider, ISseEventsManager>? managerFactory in options.EventManagerFactories)
            {
                source.Bind(() => managerFactory(_serviceProvider));
            }
        }
    }
}