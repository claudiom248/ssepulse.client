using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.DependencyInjection.Internal;

internal class SseSourceFactoryOptions
{
    internal List<Func<IServiceProvider, IRequestMutator>> RequestMutatorsFactories
    {
        get;
    } = [];
    internal Func<IServiceProvider, ILastEventIdStore>? LastEventIdStoreFactory { get; set; }
    internal List<Func<IServiceProvider, ISseEventsManager>> EventManagerFactories { get; } = [];
}