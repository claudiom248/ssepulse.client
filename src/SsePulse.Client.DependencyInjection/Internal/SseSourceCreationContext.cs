using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.DependencyInjection.Internal;

internal sealed class SseSourceCreationContext
{
    public SseSourceCreationContext(ILastEventIdStore? lastEventIdStore)
    {
        LastEventIdStore = lastEventIdStore;
    }
    
    public ILastEventIdStore? LastEventIdStore { get; }
}

