using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.DependencyInjection.Internal;

internal class LastEventIdStoreProvider
{
    private readonly ILastEventIdStore _store;

    public LastEventIdStoreProvider(ILastEventIdStore store)
    {
        _store = store;
    }

    public ILastEventIdStore Provide()
    {
        return _store;
    }
}