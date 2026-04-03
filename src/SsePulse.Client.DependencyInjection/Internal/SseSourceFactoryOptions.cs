using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Configurations;

namespace SsePulse.Client.DependencyInjection.Internal;

internal class SseSourceFactoryOptions : SseSourceOptions
{
    internal List<Func<IServiceProvider, IRequestMutator>> RequestMutatorsFactories
    {
        get;
    } = [];
    internal Func<IServiceProvider, ILastEventIdStore>? LastEventIdStoreFactory { get; set; }
    internal List<Func<IServiceProvider, ISseEventsManager>> EventManagerFactories { get; } = [];
}