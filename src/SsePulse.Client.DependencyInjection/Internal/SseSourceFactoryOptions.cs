using SsePulse.Client.Core;
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
    internal Action<IServiceProvider, SseSource>? RegisterHandlersAction { get; set; }
    internal string? ClientName { get; set; }
}